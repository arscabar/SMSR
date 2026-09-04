using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
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
                || tray.StatusColor != System.Drawing.Color.SeaGreen
                || new TrayMenuState(true, false, false).StatusColor != System.Drawing.Color.DarkOrange
                || new TrayMenuState(false, false, false).StatusColor != System.Drawing.Color.Firebrick)
                throw new InvalidOperationException("트레이 상태 모델 검증이 실패했습니다.");
            CodexMcpConfigSelfCheck.Run();
            OAuthPersistenceSelfCheck.Run(serverPath);
            await ActivitySelfCheck.RunAsync(serverPath);
            await AppUpdateSelfCheck.RunAsync(serverPath);
            await AiSummarySelfCheck.RunAsync(serverPath);
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
            var observedWorkflows = new List<WorkflowChangedEventArgs>();
            notifier.Changed += (_, eventArgs) => observedWorkflows.Add(eventArgs);
            using var notifierTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            var otherWorkflowChanged = notifier.WaitForChangeAsync("demo", "wf-other", notifier.Version("demo", "wf-other"), notifierTimeout.Token);
            notifier.Publish("demo", "wf-1");
            if (otherWorkflowChanged.IsCompleted) throw new InvalidOperationException("워크플로우별 SSE 분리가 실패했습니다.");
            notifier.Publish("demo", "wf-other");
            await otherWorkflowChanged;
            notifier.Publish("demo", "wf-other");
            if (observedWorkflows.Count != 3 || !observedWorkflows[0].IsFirstObservation
                || !observedWorkflows[1].IsFirstObservation || observedWorkflows[2].IsFirstObservation)
                throw new InvalidOperationException("새 워크플로우 감지 이벤트 검증이 실패했습니다.");
            var store = new EventStore(path);
            await store.InitializeAsync();
            var first = new RecordEventRequest("evt-1", "demo", "wf-1", "node-1", "agent-1", "NODE_STATUS_CHANGED", "IN_PROGRESS", "시작", null, null, ["result.txt"], "implementer", 25, 1, "검증");
            if (!await store.RecordAsync(first) || await store.RecordAsync(first)) throw new InvalidOperationException("event_id 중복 처리가 실패했습니다.");
            var done = first with { EventId = "evt-2", Status = "SUCCESS", Summary = "완료" };
            if (!await store.RecordAsync(done)) throw new InvalidOperationException("상태 기록이 실패했습니다.");
            var state = await store.GetStateAsync("demo", "wf-1");
            if (state.Nodes.Count != 1 || state.Nodes[0].Status != "SUCCESS" || state.Nodes[0].ProgressPercentage != 100 || state.Nodes[0].AgentRole != "implementer" || state.Nodes[0].RetryCount != 1 || state.Agents?.Count != 1)
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
            await store.SavePlanAsync("demo", "wf-1", [new("group", "구현", 1, null, null, "agent-1", "coordinator", "하위 작업 완료"), new("node-1", "코드 변경", 1, null, "group", "agent-1", "implementer", "검증 통과"), new("future", "후속 검사", 1, null, "group")]);
            const string opaqueWorkflow = "01a05b48-d586-7052-bb7c-eb258bf3f06d";
            await store.SavePlanAsync("demo", opaqueWorkflow, [new("readable", "사람이 읽는 기존 작업")]);
            var opaquePlan = await store.GetPlanAsync("demo", opaqueWorkflow);
            var opaquePage = DashboardPage.Render(new("demo", opaqueWorkflow, []), opaquePlan, []);
            if (!WebUtility.HtmlDecode(opaquePage).Contains($"사람이 읽는 기존 작업 · {opaqueWorkflow}", StringComparison.Ordinal))
                throw new InvalidOperationException("기존 UUID 대시보드 표시명 검증이 실패했습니다.");
            var hierarchicalPlan = await store.GetPlanAsync("demo", "wf-1");
            if (hierarchicalPlan.Nodes.Single(node => node.NodeId == "node-1").ParentNodeId != "group") throw new InvalidOperationException("계층 계획 저장이 실패했습니다.");
            var page = DashboardPage.Render(state with { Nodes = [state.Nodes[0] with { Summary = "<script>" }] }, hierarchicalPlan, [new RecentEvent("node-1", "agent-1", "SUCCESS", "<script>", null, DateTimeOffset.UtcNow)], null, "group", "node-1");
            if (!page.Contains("&lt;script&gt;") || !page.Contains("breadcrumb") || !page.Contains("검증 통과")
                || !page.Contains("new EventSource") || !page.Contains("let queued = false") || !page.Contains("void refresh()")
                || !page.Contains("const scrollIds = ['flow', 'graph', 'details']")
                || !page.Contains("element.scrollTop = position.top"))
                throw new InvalidOperationException("계층 대시보드와 이스케이프 검증이 실패했습니다.");
            var projectedPlan = new WorkflowPlan("demo", "wf-1", [
                new("phase", "아주 긴 프로젝트 기반 구성 제목", 1, [], "SUCCESS", null, null, null, null, "01a05675-2be3-7011-8574-f7130ef83a35"),
                new("leaf", "구현", 1, ["phase"], "SUCCESS", null, null, null, "phase"),
                new("release", "배포", 1, ["leaf"], "SUCCESS", null, null, null)]);
            var projectedGraph = DashboardGraph.Render(projectedPlan, state);
            var childGraph = DashboardGraph.Render(projectedPlan, state, "phase");
            if (!projectedGraph.Contains("class=\"edge SUCCESS\"") || !childGraph.Contains("class=\"edge SUCCESS\"")
                || !projectedGraph.Contains("01a05675…3a35") || !projectedGraph.Contains("…"))
                throw new InvalidOperationException("계층 의존선 투영과 SVG 텍스트 축약 검증이 실패했습니다.");
            var highlightPlan = new WorkflowPlan("demo", "highlight", [
                new("older", "이전 진행", 1, [], "IN_PROGRESS", null, null, DateTimeOffset.UtcNow.AddMinutes(-2)),
                new("current", "현재 진행", 1, [], "IN_PROGRESS", null, null, DateTimeOffset.UtcNow.AddMinutes(-1))]);
            var highlightState = new WorkflowState("demo", "highlight", [
                new("older", "agent", "IN_PROGRESS", null, null, DateTimeOffset.UtcNow.AddMinutes(-2), ProgressPercentage: 50),
                new("current", "agent", "IN_PROGRESS", null, null, DateTimeOffset.UtcNow.AddMinutes(-1), ProgressPercentage: 60)]);
            var highlightGraph = DashboardGraph.Render(highlightPlan, highlightState);
            if (highlightGraph.Split(" current\"", StringSplitOptions.None).Length != 2
                || !highlightGraph.Contains("flow-node IN_PROGRESS current\"><title>현재 진행"))
                throw new InvalidOperationException("현재 작업 단일 하이라이트 검증이 실패했습니다.");
            var heartbeatGraph = DashboardGraph.Render(highlightPlan, highlightState with
            {
                Agents = [
                new("agent", "worker", "ACTIVE", "older", null, 0, DateTimeOffset.UtcNow, false)]
            });
            if (heartbeatGraph.Split(" current\"", StringSplitOptions.None).Length != 2
                || !heartbeatGraph.Contains("flow-node IN_PROGRESS current\"><title>이전 진행"))
                throw new InvalidOperationException("최신 heartbeat 작업 하이라이트 검증이 실패했습니다.");
            var gatedPlan = new WorkflowPlan("demo", "gated", [
                new("first", "선행", 1, [], "IN_PROGRESS", null, null, null),
                new("second", "후행", 1, ["first"], "IN_PROGRESS", null, null, null)]);
            var gatedRequest = first with { WorkflowId = "gated", NodeId = "second", ProgressPercentage = 5 };
            if (WorkflowDependencyGate.Validate(first, new("demo", "empty", [])) is null
                || WorkflowDependencyGate.Validate(gatedRequest, gatedPlan) is null
                || DashboardHierarchy.DisplayStatus(gatedPlan.Nodes[1], gatedPlan.Nodes) != "PENDING")
                throw new InvalidOperationException("선행 작업 완료 게이트 검증이 실패했습니다.");
            if (!(await new AgentTools(store, notifier).RecordHeartbeat("demo", "empty", "agent", "worker"))
                .Contains("save_plan", StringComparison.Ordinal))
                throw new InvalidOperationException("계획 없는 heartbeat 차단 검증이 실패했습니다.");
            var completedGate = gatedPlan with { Nodes = [gatedPlan.Nodes[0] with { Status = "SUCCESS" }, gatedPlan.Nodes[1]] };
            if (WorkflowDependencyGate.Validate(gatedRequest, completedGate) is not null
                || DashboardHierarchy.DisplayStatus(completedGate.Nodes[1], completedGate.Nodes) != "IN_PROGRESS")
                throw new InvalidOperationException("선행 작업 완료 후 진행 허용 검증이 실패했습니다.");
            var parentGate = new WorkflowPlan("demo", "parent-gated", [
                new("parent", "상위", 1, [], "SUCCESS", null, null, null),
                new("child", "하위", 1, [], "PENDING", null, null, null, "parent")]);
            var parentRequest = first with { WorkflowId = "parent-gated", NodeId = "parent", Status = "SUCCESS" };
            if (DashboardHierarchy.DisplayStatus(parentGate.Nodes[0], parentGate.Nodes) != "PENDING"
                || WorkflowDependencyGate.Validate(parentRequest, parentGate) is null)
                throw new InvalidOperationException("하위 작업 미완료 상위 성공 차단 검증이 실패했습니다.");
            await using (var server = await LocalServer.StartAsync(serverPath, 0))
            using (var client = new HttpClient())
            {
                var healthResponse = await client.GetAsync($"{server.Address}/api/health");
                var denied = await client.GetAsync($"{server.Address}/mcp");
                var bridgeAnnounced = await McpBridgeConnection.NotifyAsync(server.Address, serverPath);
                var bridgeWorkflows = await new McpHttpGateway(server.Address, serverPath)
                    .CallAsync("list_workflows", new { projectId = "demo" });
                var restartedBridgeWorkflows = await new McpHttpGateway(server.Address, serverPath)
                    .CallAsync("list_workflows", new { projectId = "demo" });
                var dailyResult = await new McpHttpGateway(server.Address, serverPath).CallAsync("record_daily_activity", new
                {
                    activityId = "daily-mcp-1", projectId = "daily-mcp", taskId = "task-mcp",
                    title = "간단한 파일 수정", summary = "한 파일을 수정하고 검증했습니다.",
                    status = "SUCCESS", files = new[] { "README.md" }, verifications = new[] { "확인 완료" }
                });
                var dailyRecords = await server.GetDailyActivitiesAsync(DateTimeOffset.UtcNow.AddMinutes(-1),
                    DateTimeOffset.UtcNow.AddMinutes(1));
                var pendingSummary = server.CreateDailySummaryRequest(DateTime.Today, "MCP 요약 자료");
                DailySummaryCompletedEventArgs? completedSummary = null;
                server.DailySummaryCompleted += (_, item) => completedSummary = item;
                var summaryGateway = new McpHttpGateway(server.Address, serverPath);
                var summaryRequest = await summaryGateway.CallAsync("get_daily_summary_request",
                    new { requestId = pendingSummary.RequestId });
                var summaryResult = await summaryGateway.CallAsync("save_daily_summary_result",
                    new { requestId = pendingSummary.RequestId, content = "MCP 일일 요약 완료" });
                using var summaryRequestDocument = JsonDocument.Parse(summaryRequest);
                using var summaryResultDocument = JsonDocument.Parse(summaryResult);
                using var bridgeDocument = JsonDocument.Parse(bridgeWorkflows);
                using var restartedBridgeDocument = JsonDocument.Parse(restartedBridgeWorkflows);
                var bridgeCatalogRead = bridgeDocument.RootElement.GetProperty("projectId").GetString() == "demo"
                    && bridgeDocument.RootElement.GetProperty("workflows").GetArrayLength() == 0
                    && restartedBridgeDocument.RootElement.GetProperty("projectId").GetString() == "demo"
                    && restartedBridgeDocument.RootElement.GetProperty("workflows").GetArrayLength() == 0;
                var oauthToken = await OAuthSelfCheck.RunAsync(server.Address);
                var stateResponse = await client.GetAsync($"{server.Address}/api/state?projectId=demo&workflowId=wf-1");
                var dashboardResponse = await client.GetAsync($"{server.Address}/dashboard?projectId=demo&workflowId=wf-1");
                using var streamResponse = await client.GetAsync($"{server.Address}/api/events/stream?projectId=demo&workflowId=wf-1", HttpCompletionOption.ResponseHeadersRead);
                using var streamReader = new StreamReader(await streamResponse.Content.ReadAsStreamAsync());
                var initialEvent = await streamReader.ReadLineAsync();
                var initialData = await streamReader.ReadLineAsync();
                await streamReader.ReadLineAsync();
                using var activityRequest = new HttpRequestMessage(HttpMethod.Post, $"{server.Address}/api/activity")
                {
                    Content = JsonContent.Create(new ActivityRecord(DateTimeOffset.UtcNow, "demo", "wf-1",
                        "session-activity", "TOOL_COMPLETED", "FILE_EDIT", "turn-1", "agent-1", "node-1", "apply_patch", "tool-1", "activity-http-1"))
                };
                activityRequest.Headers.Add("X-SMSR-Hook-Token", new ActivityHookToken(serverPath).Value);
                using var activityResponse = await client.SendAsync(activityRequest);
                var activityJson = await client.GetStringAsync($"{server.Address}/api/activity?projectId=demo&workflowId=wf-1");
                using var recordEvent = new HttpRequestMessage(HttpMethod.Post, $"{server.Address}/mcp")
                {
                    Content = new StringContent("{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"tools/call\",\"params\":{\"name\":\"record_event\",\"arguments\":{\"eventId\":\"evt-mcp-1\",\"projectId\":\"demo\",\"workflowId\":\"wf-1\",\"nodeId\":\"mcp-node\",\"agentId\":\"agent-1\",\"agentRole\":\"implementer\",\"eventType\":\"NODE_STATUS_CHANGED\",\"status\":\"IN_PROGRESS\",\"progressPercentage\":40,\"retryCount\":2,\"artifacts\":[\"build.log\"]},\"_meta\":{\"io.modelcontextprotocol/protocolVersion\":\"2026-07-28\",\"io.modelcontextprotocol/clientInfo\":{\"name\":\"self-test\",\"version\":\"1.0\"},\"io.modelcontextprotocol/clientCapabilities\":{}}}}", Encoding.UTF8, "application/json")
                };
                recordEvent.Headers.Authorization = new("Bearer", oauthToken);
                recordEvent.Headers.Accept.ParseAdd("application/json, text/event-stream");
                recordEvent.Headers.Add("MCP-Protocol-Version", "2026-07-28");
                recordEvent.Headers.Add("MCP-Method", "tools/call");
                recordEvent.Headers.Add("MCP-Name", "record_event");
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
                var recordResponse = await client.SendAsync(recordEvent);
                var recordJson = await recordResponse.Content.ReadAsStringAsync();
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
                using var stateDocument = JsonDocument.Parse(recordedState);
                using var planDocument = JsonDocument.Parse(recordedPlan);
                var stateRecorded = stateDocument.RootElement.GetProperty("nodes").EnumerateArray().Any(node =>
                    node.GetProperty("nodeId").GetString() == "mcp-node" && node.GetProperty("agentRole").GetString() == "implementer");
                var planRecorded = planDocument.RootElement.GetProperty("nodes").EnumerateArray().Any(node =>
                    node.GetProperty("title").GetString() == "MCP 계획 노드")
                    && planDocument.RootElement.GetProperty("nodes").EnumerateArray().Any(node =>
                        node.GetProperty("nodeId").GetString() == "mcp-final" && node.GetProperty("parentNodeId").GetString() == "mcp-node");
                var decodedDashboard = WebUtility.HtmlDecode(recordedDashboard);
                using var sseTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                var changedEvent = await streamReader.ReadLineAsync(sseTimeout.Token);
                if (!recordResponse.IsSuccessStatusCode) throw new InvalidOperationException($"MCP record_event 호출 실패: {recordJson}");
                var localChecks = new (string Name, bool Passed)[]
                {
                    ("health-auth", healthResponse.IsSuccessStatusCode && denied.StatusCode == HttpStatusCode.Unauthorized),
                    ("bridge", bridgeAnnounced && server.HasActiveMcpClient && bridgeCatalogRead),
                    ("daily-activity", dailyResult.Contains("daily-mcp-1", StringComparison.Ordinal)
                        && dailyRecords.Any(item => item.ActivityId == "daily-mcp-1" && item.Files.Single() == "README.md")),
                    ("daily-summary", summaryRequestDocument.RootElement.GetProperty("Prompt").GetString() == "MCP 요약 자료"
                        && summaryResultDocument.RootElement.GetProperty("saved").GetBoolean()
                        && completedSummary?.Content == "MCP 일일 요약 완료"),
                    ("initial-http-sse", stateResponse.IsSuccessStatusCode && dashboardResponse.IsSuccessStatusCode && streamResponse.IsSuccessStatusCode && initialEvent == "event: state" && initialData == "data: changed" && changedEvent == "event: state"),
                    ("activity", activityResponse.IsSuccessStatusCode && activityJson.Contains("TOOL_COMPLETED") && recordedDashboard.Contains("TOOL_COMPLETED")),
                    ("mcp-http", recordResponse.IsSuccessStatusCode && planResponse.IsSuccessStatusCode && listResponse.IsSuccessStatusCode),
                    ("mcp-payload", recordJson.Contains("evt-mcp-1") && planJson.Contains("nodeCount") && listJson.Contains("wf-1") && listJson.Contains("ACTIVE")),
                    ("state", stateRecorded),
                    ("plan", planRecorded),
                    ("dashboard-content", decodedDashboard.Contains("계층형 작업 흐름") && recordedDashboard.Contains("id=\"agents\"") && decodedDashboard.Contains("실시간 활동") && recordedDashboard.Contains("flow-svg") && decodedDashboard.Contains("MCP 계획 노드")),
                    ("dashboard-live", !recordedDashboard.Contains("http-equiv=\"refresh\"") && recordedDashboard.Contains("new EventSource") && recordedDashboard.Contains("smsr-graph-nav") && recordedDashboard.Contains("getAttribute('href')"))
                };
                var failedChecks = localChecks.Where(check => !check.Passed).Select(check => check.Name).ToArray();
                if (failedChecks.Length > 0)
                    throw new InvalidOperationException($"로컬 서버 검증 실패: {string.Join(", ", failedChecks)}. plan={planJson}; event={recordJson}; list={listJson}");
            }
            var settings = new AppSettingsService(serverPath);
            await using (var host = new LocalServerHost(serverPath, 0, () => settings.Current.DashboardTheme))
            {
                await host.StartAsync();
                var legacyStore = new EventStore(Path.Combine(serverPath, "smsr.db"));
                await legacyStore.InitializeAsync();
                await legacyStore.SavePlanAsync("demo", opaqueWorkflow, [new("readable", "사람이 읽는 기존 작업")]);
                await legacyStore.SavePlanAsync("project-b", "workflow-b", [new("b-node", "B 프로젝트 구현")]);
                await legacyStore.RecordAsync(new("event-b", "project-b", "workflow-b", "b-node", "agent-b",
                    "NODE_STATUS_CHANGED", "IN_PROGRESS", "B 프로젝트 작업 중", null, null, ["b-result.txt"],
                    "implementer", 40));
                await legacyStore.RecordDailyActivityAsync(new("daily-simple", "simple-project", "simple-task",
                    "단일 문서 수정", "안내 문구 한 곳을 수정했습니다.", Files: ["README.md"],
                    Verifications: ["문서 렌더링 확인"]));
                await OAuthSelfCheck.RunAsync(host.Address);
                var platform = new TestPlatformActions();
                var viewModel = new MainWindowViewModel(host, platform, settings);
                await viewModel.LoadAsync();
                if (!host.IsCodexAuthorized || !host.IsCodexConnected || !viewModel.Server.IsCodexConnected || viewModel.Server.NeedsCodexSetup)
                    throw new InvalidOperationException("Codex 연결 완료 UI 상태 복원이 실패했습니다.");
                viewModel.Settings.StartServerAutomatically = false;
                viewModel.Settings.AutomateCodexIntegration = false;
                viewModel.Settings.TrackComplexTasksAutomatically = false;
                viewModel.Settings.AutoUpdateEnabled = false;
                viewModel.Settings.MinimizeToTray = false;
                viewModel.Settings.DashboardTheme = DashboardThemes.Light;
                viewModel.Settings.RequirePlanReview = false;
                viewModel.Settings.PlanningPrompt = "검토용 계획 {projectId}";
                var savedSettings = new AppSettingsService(serverPath).Current;
                if (savedSettings.StartServerAutomatically || savedSettings.AutomateCodexIntegration
                    || savedSettings.TrackComplexTasksAutomatically || savedSettings.AutoUpdateEnabled
                    || savedSettings.MinimizeToTray || savedSettings.DashboardTheme != DashboardThemes.Light
                    || savedSettings.RequirePlanReview || savedSettings.PlanningPrompt != "검토용 계획 {projectId}")
                    throw new InvalidOperationException("사용자 설정 저장 검증이 실패했습니다.");
                viewModel.Workspace.Selection.ProjectId = "demo";
                await viewModel.Workspace.Selection.LoadAsync();
                if (!viewModel.Workspace.Selection.DailyActivities.Any(item => item.ActivityId == "daily-simple")
                    || !viewModel.Workspace.Selection.CalendarSummary.Contains("작업 기록", StringComparison.Ordinal)
                    || !viewModel.Workspace.Selection.DailyOverview.Contains("변경 파일", StringComparison.Ordinal)
                    || viewModel.Workspace.Selection.CalendarDays.Count(item => item.IsInMonth)
                        != DateTime.DaysInMonth(viewModel.Workspace.Selection.SelectedDate!.Value.Year,
                            viewModel.Workspace.Selection.SelectedDate.Value.Month)
                    || viewModel.Workspace.Selection.CalendarDays.Count(item => item.IsInMonth) > 31)
                    throw new InvalidOperationException("일일 작업 캘린더 표시 검증이 실패했습니다.");
                if (!viewModel.Workspace.Selection.Workflows.Any(item => item.WorkflowId == opaqueWorkflow
                    && item.DisplayName.Contains("사람이 읽는 기존 작업", StringComparison.Ordinal)))
                    throw new InvalidOperationException("기존 UUID 워크플로우 표시명 검증이 실패했습니다.");
                var projectB = viewModel.Workspace.Selection.CalendarWorkflows.SingleOrDefault(item =>
                    item.ProjectId == "project-b" && item.WorkflowId == "workflow-b");
                if (!viewModel.Workspace.Selection.ProjectIds.Contains("project-b") || projectB is null
                    || projectB.DisplayName.Contains("workflow-b", StringComparison.Ordinal))
                    throw new InvalidOperationException("다중 프로젝트 캘린더 통합 검증이 실패했습니다.");
                await viewModel.Workspace.SelectCalendarWorkflowAsync(projectB);
                if (viewModel.Workspace.Selection.ProjectId != "project-b"
                    || viewModel.Workspace.Selection.WorkflowId != "workflow-b"
                    || viewModel.Workspace.Monitor.Nodes.Single().NodeId != "b-node")
                    throw new InvalidOperationException("캘린더 작업 전환 검증이 실패했습니다.");
                await viewModel.Workspace.SelectCalendarWorkflowAsync(viewModel.Workspace.Selection.CalendarWorkflows
                    .First(item => item.ProjectId == "demo" && item.WorkflowId == "wf-1"));
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
                if (!platform.OpenedUrl.Contains("projectId=demo") || viewModel.Workspace.Monitor.Nodes.Count == 0 || summary.Content.Length == 0 || !File.Exists(export.ZipPath) || !File.ReadAllText(Path.Combine(export.DirectoryPath, "events.jsonl")).Contains("evt-mcp-1") || !File.ReadAllText(Path.Combine(export.DirectoryPath, "activity.jsonl")).Contains("TOOL_COMPLETED") || !themedDashboard.Contains("color-scheme:light") || !exportedDashboard.Contains("color-scheme:light") || !exportedDashboard.Contains("flow-svg"))
                    throw new InvalidOperationException("WPF 서버 제어·요약·내보내기 검증이 실패했습니다.");
                var deleteActivity = new ActivityJsonlStore(serverPath);
                deleteActivity.Append(new(DateTimeOffset.UtcNow, "delete-project", "delete-workflow", "delete-session",
                    "TOOL_COMPLETED", "TOOL", ActivityId: "delete-activity"));
                await legacyStore.SavePlanAsync("delete-project", "delete-workflow", [new("delete-node", "삭제 검증")]);
                new TrackingSessionStore(serverPath).Save("delete-session",
                    new("delete-project", "delete-workflow", null, DateTimeOffset.UtcNow));
                if (await host.DeleteWorkflowAsync("delete-project", "delete-workflow") != 1
                    || (await host.GetWorkflowIdsAsync("delete-project")).Count != 0
                    || File.Exists(deleteActivity.PathFor("delete-project", "delete-workflow"))
                    || new TrackingSessionStore(serverPath).Load("delete-session") is not null)
                    throw new InvalidOperationException("워크플로우 이력 삭제 검증이 실패했습니다.");
                foreach (var workflowId in new[] { "project-delete-a", "project-delete-b" })
                {
                    await legacyStore.SavePlanAsync("delete-project", workflowId, [new("delete-node", "프로젝트 삭제 검증")]);
                    deleteActivity.Append(new(DateTimeOffset.UtcNow, "delete-project", workflowId, workflowId,
                        "TOOL_COMPLETED", "TOOL", ActivityId: workflowId));
                    new TrackingSessionStore(serverPath).Save(workflowId,
                        new("delete-project", workflowId, null, DateTimeOffset.UtcNow));
                }
                if (await host.DeleteProjectAsync("delete-project") != 2
                    || (await host.GetProjectIdsAsync()).Contains("delete-project")
                    || File.Exists(deleteActivity.PathFor("delete-project", "project-delete-a"))
                    || new TrackingSessionStore(serverPath).Load("project-delete-b") is not null)
                    throw new InvalidOperationException("프로젝트 이력 삭제 검증이 실패했습니다.");
                var exportedZip = export.ZipPath;
                if (await host.DeleteAllAsync() < 3 || (await host.GetProjectIdsAsync()).Count != 0
                    || !File.Exists(exportedZip)
                    || Directory.EnumerateFiles(Path.Combine(serverPath, "activity"), "*.jsonl*").Any())
                    throw new InvalidOperationException("전체 이력 삭제와 내보내기 보존 검증이 실패했습니다.");
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
        public bool Confirm(string title, string message) => true;
    }
}
