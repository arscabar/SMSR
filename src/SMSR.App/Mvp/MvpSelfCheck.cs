using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using SMSR.App.Infrastructure;
using SMSR.App.Services;
using SMSR.App.ViewModels;

namespace SMSR.App.Mvp;

public static class MvpSelfCheck
{
    public static async Task RunAsync()
    {
        var path = Path.Combine(Path.GetTempPath(), $"smsr-{Guid.NewGuid():N}.db");
        var serverPath = Path.Combine(Path.GetTempPath(), $"smsr-server-{Guid.NewGuid():N}");
        var logPath = Path.Combine(Path.GetTempPath(), $"smsr-log-{Guid.NewGuid():N}");
        try
        {
            var tray = new TrayMenuState(true, true, true);
            if (!tray.StatusText.Contains("Codex 연결됨") || !tray.ToolTip.Contains("Codex 연결됨")
                || new TrayMenuState(false, false, false).StatusText != "● 서버 중지됨")
                throw new InvalidOperationException("트레이 상태 모델 검증이 실패했습니다.");
            CodexMcpConfigSelfCheck.Run();
            var connectionTracker = new McpConnectionTracker();
            var connectionChanges = 0;
            connectionTracker.Changed += (_, _) => connectionChanges++;
            connectionTracker.MarkActivity();
            connectionTracker.MarkActivity();
            if (!connectionTracker.IsConnected || connectionTracker.LastActivityAt is null || connectionChanges != 1)
                throw new InvalidOperationException("실제 MCP 연결 추적 검증이 실패했습니다.");
            var activityLog = new LocalActivityLog(logPath);
            Directory.CreateDirectory(logPath);
            await File.WriteAllTextAsync(activityLog.Path, new string('x', 1_000_000));
            await activityLog.WriteAsync("rotated");
            if (!File.Exists(activityLog.PreviousPath) || !File.ReadAllText(activityLog.Path).Contains("rotated"))
                throw new InvalidOperationException("활동 로그 회전이 실패했습니다.");
            var notifier = new WorkflowEventNotifier();
            using var notifierTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            var otherWorkflowChanged = notifier.WaitForChangeAsync("demo", "wf-other", notifier.Version("demo", "wf-other"), notifierTimeout.Token);
            notifier.Publish("demo", "wf-1");
            if (otherWorkflowChanged.IsCompleted) throw new InvalidOperationException("워크플로우별 SSE 분리가 실패했습니다.");
            notifier.Publish("demo", "wf-other");
            await otherWorkflowChanged;
            var store = new EventStore(path);
            await store.InitializeAsync();
            var first = new RecordEventRequest("evt-1", "demo", "wf-1", "node-1", "agent-1", "NODE_STATUS_CHANGED", "IN_PROGRESS", "시작", null, null, ["result.txt"], "implementer", 25, 1, "검증");
            if (!await store.RecordAsync(first) || await store.RecordAsync(first)) throw new InvalidOperationException("event_id 중복 처리가 실패했습니다.");
            var done = first with { EventId = "evt-2", Status = "SUCCESS", Summary = "완료" };
            if (!await store.RecordAsync(done)) throw new InvalidOperationException("상태 기록이 실패했습니다.");
            var state = await store.GetStateAsync("demo", "wf-1");
            if (state.Nodes.Count != 1 || state.Nodes[0].Status != "SUCCESS" || state.Nodes[0].AgentRole != "implementer" || state.Nodes[0].RetryCount != 1 || state.Agents?.Count != 1)
                throw new InvalidOperationException("확장 상태와 에이전트 계산이 실패했습니다.");
            var recent = await store.GetRecentEventsAsync("demo", "wf-1");
            if (recent.Count != 2 || recent[0].Status != "SUCCESS") throw new InvalidOperationException("최근 이벤트 조회가 실패했습니다.");
            if ((await store.GetLatestEventAsync("demo", "wf-1"))?.EventId != "evt-2") throw new InvalidOperationException("최신 이벤트 조회가 실패했습니다.");
            var concurrent = await Task.WhenAll(Enumerable.Range(0, 16).Select(index => store.RecordAsync(first with { EventId = $"evt-load-{index}", NodeId = $"node-load-{index}" })));
            if (concurrent.Any(inserted => !inserted) || (await store.GetStateAsync("demo", "wf-1")).Nodes.Count != 17)
                throw new InvalidOperationException("동시 이벤트 기록이 실패했습니다.");
            await Task.WhenAll(Enumerable.Range(0, 8).Select(async index =>
            {
                await store.SaveSummaryAsync(new WorkflowSummary("demo", "wf-1", $"요약 {index}", DateTimeOffset.UtcNow), null);
                if (!await store.RecordAsync(first with { EventId = $"evt-summary-{index}", NodeId = $"node-summary-{index}" }))
                    throw new InvalidOperationException("동시 요약 저장이 실패했습니다.");
            }));
            if (!(await store.GetProjectIdsAsync()).Contains("demo") || !(await store.GetWorkflowIdsAsync("demo")).Contains("wf-1")) throw new InvalidOperationException("프로젝트·워크플로우 목록 조회가 실패했습니다.");
            if (EventValidation.Validate(first with { Status = "INVALID" }) is null) throw new InvalidOperationException("입력 검증이 실패했습니다.");
            if (EventValidation.Validate(first with { Commands = Enumerable.Repeat("command", 101).ToArray() }) is null || EventValidation.ValidateWorkflowIds("", "wf-1") is null)
                throw new InvalidOperationException("입력 크기 검증이 실패했습니다.");
            if (PlanValidation.Validate("demo", "wf-1", [new("node-a", "A", 1, ["node-a"])]) is null)
                throw new InvalidOperationException("계획 의존성 검증이 실패했습니다.");
            await store.SavePlanAsync("demo", "wf-1", [new("group", "구현", 1, null, null, "agent-1", "coordinator", "하위 작업 완료"), new("node-1", "코드 변경", 1, null, "group", "agent-1", "implementer", "검증 통과")]);
            var hierarchicalPlan = await store.GetPlanAsync("demo", "wf-1");
            if (hierarchicalPlan.Nodes.Single(node => node.NodeId == "node-1").ParentNodeId != "group") throw new InvalidOperationException("계층 계획 저장이 실패했습니다.");
            var page = DashboardPage.Render(state with { Nodes = [state.Nodes[0] with { Summary = "<script>" }] }, hierarchicalPlan, [new RecentEvent("node-1", "agent-1", "SUCCESS", "<script>", null, DateTimeOffset.UtcNow)], null, "group", "node-1");
            if (!page.Contains("&lt;script&gt;") || !page.Contains("breadcrumb") || !page.Contains("검증 통과") || !page.Contains("new EventSource"))
                throw new InvalidOperationException("계층 대시보드와 이스케이프 검증이 실패했습니다.");
            await using (var server = await LocalServer.StartAsync(serverPath, 0))
            using (var client = new HttpClient())
            {
                var denied = await client.GetAsync($"{server.Address}/mcp");
                var oauthToken = await OAuthSelfCheck.RunAsync(server.Address);
                var stateResponse = await client.GetAsync($"{server.Address}/api/state?projectId=demo&workflowId=wf-1");
                var dashboardResponse = await client.GetAsync($"{server.Address}/dashboard?projectId=demo&workflowId=wf-1");
                using var streamResponse = await client.GetAsync($"{server.Address}/api/events/stream?projectId=demo&workflowId=wf-1", HttpCompletionOption.ResponseHeadersRead);
                using var streamReader = new StreamReader(await streamResponse.Content.ReadAsStreamAsync());
                var initialEvent = await streamReader.ReadLineAsync();
                var initialData = await streamReader.ReadLineAsync();
                await streamReader.ReadLineAsync();
                using var recordEvent = new HttpRequestMessage(HttpMethod.Post, $"{server.Address}/mcp")
                {
                    Content = new StringContent("{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"tools/call\",\"params\":{\"name\":\"record_event\",\"arguments\":{\"eventId\":\"evt-mcp-1\",\"projectId\":\"demo\",\"workflowId\":\"wf-1\",\"nodeId\":\"mcp-node\",\"agentId\":\"agent-1\",\"agentRole\":\"implementer\",\"eventType\":\"NODE_STATUS_CHANGED\",\"status\":\"IN_PROGRESS\",\"progressPercentage\":40,\"retryCount\":2,\"artifacts\":[\"build.log\"]},\"_meta\":{\"io.modelcontextprotocol/protocolVersion\":\"2026-07-28\",\"io.modelcontextprotocol/clientInfo\":{\"name\":\"self-test\",\"version\":\"1.0\"},\"io.modelcontextprotocol/clientCapabilities\":{}}}}", Encoding.UTF8, "application/json")
                };
                recordEvent.Headers.Authorization = new("Bearer", oauthToken);
                recordEvent.Headers.Accept.ParseAdd("application/json, text/event-stream");
                recordEvent.Headers.Add("MCP-Protocol-Version", "2026-07-28");
                recordEvent.Headers.Add("MCP-Method", "tools/call");
                recordEvent.Headers.Add("MCP-Name", "record_event");
                var recordResponse = await client.SendAsync(recordEvent);
                var recordJson = await recordResponse.Content.ReadAsStringAsync();
                using var savePlan = new HttpRequestMessage(HttpMethod.Post, $"{server.Address}/mcp")
                {
                    Content = new StringContent("{\"jsonrpc\":\"2.0\",\"id\":2,\"method\":\"tools/call\",\"params\":{\"name\":\"save_plan\",\"arguments\":{\"projectId\":\"demo\",\"workflowId\":\"wf-1\",\"nodes\":[{\"nodeId\":\"mcp-node\",\"title\":\"MCP 계획 노드\",\"weight\":2,\"assignedAgentId\":\"agent-1\",\"agentRole\":\"coordinator\"},{\"nodeId\":\"mcp-final\",\"title\":\"완료 노드\",\"parentNodeId\":\"mcp-node\",\"completionCriteria\":\"테스트 통과\"}]},\"_meta\":{\"io.modelcontextprotocol/protocolVersion\":\"2026-07-28\",\"io.modelcontextprotocol/clientInfo\":{\"name\":\"self-test\",\"version\":\"1.0\"},\"io.modelcontextprotocol/clientCapabilities\":{}}}}", Encoding.UTF8, "application/json")
                };
                savePlan.Headers.Authorization = new("Bearer", oauthToken);
                savePlan.Headers.Accept.ParseAdd("application/json, text/event-stream");
                savePlan.Headers.Add("MCP-Protocol-Version", "2026-07-28");
                savePlan.Headers.Add("MCP-Method", "tools/call");
                savePlan.Headers.Add("MCP-Name", "save_plan");
                var planResponse = await client.SendAsync(savePlan);
                var planJson = await planResponse.Content.ReadAsStringAsync();
                using var listWorkflows = new HttpRequestMessage(HttpMethod.Post, $"{server.Address}/mcp")
                {
                    Content = new StringContent("{\"jsonrpc\":\"2.0\",\"id\":3,\"method\":\"tools/call\",\"params\":{\"name\":\"list_workflows\",\"arguments\":{\"projectId\":\"demo\"},\"_meta\":{\"io.modelcontextprotocol/protocolVersion\":\"2026-07-28\",\"io.modelcontextprotocol/clientInfo\":{\"name\":\"self-test\",\"version\":\"1.0\"},\"io.modelcontextprotocol/clientCapabilities\":{}}}}", Encoding.UTF8, "application/json")
                };
                listWorkflows.Headers.Authorization = new("Bearer", oauthToken);
                listWorkflows.Headers.Accept.ParseAdd("application/json, text/event-stream");
                listWorkflows.Headers.Add("MCP-Protocol-Version", "2026-07-28");
                listWorkflows.Headers.Add("MCP-Method", "tools/call");
                listWorkflows.Headers.Add("MCP-Name", "list_workflows");
                var listResponse = await client.SendAsync(listWorkflows);
                var listJson = await listResponse.Content.ReadAsStringAsync();
                var recordedState = await client.GetStringAsync($"{server.Address}/api/state?projectId=demo&workflowId=wf-1");
                var recordedPlan = await client.GetStringAsync($"{server.Address}/api/plan?projectId=demo&workflowId=wf-1");
                var recordedDashboard = await client.GetStringAsync($"{server.Address}/dashboard?projectId=demo&workflowId=wf-1");
                using var sseTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                var changedEvent = await streamReader.ReadLineAsync(sseTimeout.Token);
                if (!recordResponse.IsSuccessStatusCode) throw new InvalidOperationException($"MCP record_event 호출 실패: {recordJson}");
                if (denied.StatusCode != HttpStatusCode.Unauthorized || !stateResponse.IsSuccessStatusCode || !dashboardResponse.IsSuccessStatusCode || !streamResponse.IsSuccessStatusCode || initialEvent != "event: state" || initialData != "data: changed" || changedEvent != "event: state" || !recordResponse.IsSuccessStatusCode || !planResponse.IsSuccessStatusCode || !listResponse.IsSuccessStatusCode || !recordJson.Contains("evt-mcp-1") || !planJson.Contains("nodeCount") || !listJson.Contains("wf-1") || !listJson.Contains("ACTIVE") || !recordedState.Contains("mcp-node") || !recordedState.Contains("implementer") || !recordedPlan.Contains("MCP 계획 노드") || !recordedPlan.Contains("parentNodeId") || !recordedDashboard.Contains("계층형 작업 흐름") || !recordedDashboard.Contains("id=\"agents\"") || !recordedDashboard.Contains("flow-svg") || !recordedDashboard.Contains("MCP 계획 노드") || recordedDashboard.Contains("http-equiv=\"refresh\"") || !recordedDashboard.Contains("new EventSource") || !recordedDashboard.Contains("smsr-graph-nav") || !recordedDashboard.Contains("getAttribute('href')"))
                    throw new InvalidOperationException("로컬 서버 검증이 실패했습니다.");
            }
            var settings = new AppSettingsService(serverPath);
            await using (var host = new LocalServerHost(serverPath, 0, () => settings.Current.DashboardTheme))
            {
                await host.StartAsync();
                await OAuthSelfCheck.RunAsync(host.Address);
                var platform = new TestPlatformActions();
                var viewModel = new MainWindowViewModel(host, platform, settings);
                await viewModel.LoadAsync();
                if (!host.IsCodexAuthorized || !host.IsCodexConnected || !viewModel.Server.IsCodexConnected || viewModel.Server.NeedsCodexSetup)
                    throw new InvalidOperationException("Codex 연결 완료 UI 상태 복원이 실패했습니다.");
                viewModel.Settings.StartServerAutomatically = false;
                viewModel.Settings.AutomateCodexIntegration = false;
                viewModel.Settings.MinimizeToTray = false;
                viewModel.Settings.DashboardTheme = DashboardThemes.Light;
                var savedSettings = new AppSettingsService(serverPath).Current;
                if (savedSettings.StartServerAutomatically || savedSettings.AutomateCodexIntegration
                    || savedSettings.MinimizeToTray || savedSettings.DashboardTheme != DashboardThemes.Light)
                    throw new InvalidOperationException("사용자 설정 저장 검증이 실패했습니다.");
                viewModel.Workspace.Selection.ProjectId = "demo";
                await viewModel.Workspace.Selection.LoadAsync();
                viewModel.Workspace.Selection.WorkflowId = "wf-1";
                await host.StopAsync().WaitAsync(TimeSpan.FromSeconds(5));
                await host.StartAsync();
                viewModel = new MainWindowViewModel(host, platform, settings);
                await viewModel.LoadAsync();
                if (viewModel.Workspace.Selection.ProjectId != "demo" || viewModel.Workspace.Selection.WorkflowId != "wf-1" || viewModel.Workspace.Monitor.Nodes.Count == 0)
                    throw new InvalidOperationException("재시작 후 저장된 작업 진행도 복원이 실패했습니다.");
                viewModel.Workspace.Selection.ProjectId = "demo";
                await viewModel.Workspace.Selection.LoadAsync();
                viewModel.Workspace.Selection.WorkflowId = "wf-1";
                if (!viewModel.Workspace.OpenDashboardCommand.CanExecute(null)) throw new InvalidOperationException("대시보드 명령 활성화가 실패했습니다.");
                viewModel.Workspace.OpenDashboardCommand.Execute(null);
                await viewModel.Workspace.Monitor.RefreshAsync("demo", "wf-1");
                var summary = await host.GenerateSummaryAsync("demo", "wf-1");
                var export = await host.ExportAsync("demo", "wf-1");
                using var dashboardClient = new HttpClient();
                var themedDashboard = await dashboardClient.GetStringAsync($"{host.Address}/dashboard?projectId=demo&workflowId=wf-1");
                var exportedDashboard = File.ReadAllText(Path.Combine(export.DirectoryPath, "dashboard.html"));
                if (!platform.OpenedUrl.Contains("projectId=demo") || viewModel.Workspace.Monitor.Nodes.Count == 0 || summary.Content.Length == 0 || !File.Exists(export.ZipPath) || !File.ReadAllText(Path.Combine(export.DirectoryPath, "events.jsonl")).Contains("evt-mcp-1") || !themedDashboard.Contains("color-scheme:light") || !exportedDashboard.Contains("color-scheme:light") || !exportedDashboard.Contains("flow-svg"))
                    throw new InvalidOperationException("WPF 서버 제어·요약·내보내기 검증이 실패했습니다.");
                await host.StopAsync().WaitAsync(TimeSpan.FromSeconds(5));
                if (host.IsRunning || viewModel.Workspace.ExportCommand.CanExecute(null) || !File.ReadAllText(host.LogPath).Contains("server started") || !File.ReadAllText(host.LogPath).Contains("server stopped"))
                    throw new InvalidOperationException("서버 중지·로그 복구 검증이 실패했습니다.");
            }
        }
        finally
        {
            foreach (var file in new[] { path, $"{path}-shm", $"{path}-wal" })
                if (File.Exists(file)) File.Delete(file);
            if (Directory.Exists(serverPath)) Directory.Delete(serverPath, true);
            if (Directory.Exists(logPath)) Directory.Delete(logPath, true);
        }
    }

    private sealed class TestPlatformActions : IPlatformActions
    {
        public string OpenedUrl { get; private set; } = "";
        public bool TryCopyToClipboard(string value) => true;
        public bool TryOpenBrowser(string url) { OpenedUrl = url; return true; }
        public bool TryOpenPath(string path) => Directory.Exists(path);
    }
}
