using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using Microsoft.AspNetCore.Http;
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
            var activityLog = new LocalActivityLog(logPath);
            Directory.CreateDirectory(logPath);
            await File.WriteAllTextAsync(activityLog.Path, new string('x', 1_000_000));
            await activityLog.WriteAsync("rotated");
            if (!File.Exists(activityLog.PreviousPath) || !File.ReadAllText(activityLog.Path).Contains("rotated"))
                throw new InvalidOperationException("활동 로그 회전이 실패했습니다.");
            var store = new EventStore(path);
            await store.InitializeAsync();
            var first = new RecordEventRequest("evt-1", "demo", "wf-1", "node-1", "agent-1", "NODE_STATUS_CHANGED", "IN_PROGRESS", "시작", null, null, null);
            if (!await store.RecordAsync(first) || await store.RecordAsync(first)) throw new InvalidOperationException("event_id 중복 처리가 실패했습니다.");
            var done = first with { EventId = "evt-2", Status = "SUCCESS", Summary = "완료" };
            if (!await store.RecordAsync(done)) throw new InvalidOperationException("상태 기록이 실패했습니다.");
            var state = await store.GetStateAsync("demo", "wf-1");
            if (state.Nodes.Count != 1 || state.Nodes[0].Status != "SUCCESS") throw new InvalidOperationException("최신 상태 계산이 실패했습니다.");
            var recent = await store.GetRecentEventsAsync("demo", "wf-1");
            if (recent.Count != 2 || recent[0].Status != "SUCCESS") throw new InvalidOperationException("최근 이벤트 조회가 실패했습니다.");
            var concurrent = await Task.WhenAll(Enumerable.Range(0, 16).Select(index => store.RecordAsync(first with { EventId = $"evt-load-{index}", NodeId = $"node-load-{index}" })));
            if (concurrent.Any(inserted => !inserted) || (await store.GetStateAsync("demo", "wf-1")).Nodes.Count != 17)
                throw new InvalidOperationException("동시 이벤트 기록이 실패했습니다.");
            if (!(await store.GetProjectIdsAsync()).Contains("demo") || !(await store.GetWorkflowIdsAsync("demo")).Contains("wf-1")) throw new InvalidOperationException("프로젝트·워크플로우 목록 조회가 실패했습니다.");
            if (EventValidation.Validate(first with { Status = "INVALID" }) is null) throw new InvalidOperationException("입력 검증이 실패했습니다.");
            var page = DashboardPage.Render(new WorkflowState("demo", "wf-1", [new StateNode("node-1", "agent-1", "SUCCESS", "<script>", null, DateTimeOffset.UtcNow)]), [new RecentEvent("node-1", "agent-1", "SUCCESS", "<script>", null, DateTimeOffset.UtcNow)]);
            if (page.Contains("<script>") || !page.Contains("&lt;script&gt;")) throw new InvalidOperationException("대시보드 이스케이프가 실패했습니다.");
            var request = new DefaultHttpContext().Request;
            request.Headers.Authorization = "Bearer token";
            if (!LocalServer.IsAuthorized(request, "token") || LocalServer.IsAuthorized(request, "other")) throw new InvalidOperationException("토큰 검증이 실패했습니다.");
            await using (var server = await LocalServer.StartAsync(serverPath))
            using (var client = new HttpClient())
            {
                var denied = await client.GetAsync($"{server.Address}/mcp");
                var stateResponse = await client.GetAsync($"{server.Address}/api/state?projectId=demo&workflowId=wf-1");
                var dashboardResponse = await client.GetAsync($"{server.Address}/dashboard?projectId=demo&workflowId=wf-1");
                using var streamResponse = await client.GetAsync($"{server.Address}/api/events/stream?projectId=demo&workflowId=wf-1", HttpCompletionOption.ResponseHeadersRead);
                using var streamReader = new StreamReader(await streamResponse.Content.ReadAsStreamAsync());
                var initialEvent = await streamReader.ReadLineAsync();
                var initialData = await streamReader.ReadLineAsync();
                await streamReader.ReadLineAsync();
                using var recordEvent = new HttpRequestMessage(HttpMethod.Post, $"{server.Address}/mcp")
                {
                    Content = new StringContent("{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"tools/call\",\"params\":{\"name\":\"record_event\",\"arguments\":{\"eventId\":\"evt-mcp-1\",\"projectId\":\"demo\",\"workflowId\":\"wf-1\",\"nodeId\":\"mcp-node\",\"agentId\":\"agent-1\",\"eventType\":\"NODE_STATUS_CHANGED\",\"status\":\"IN_PROGRESS\"},\"_meta\":{\"io.modelcontextprotocol/protocolVersion\":\"2026-07-28\",\"io.modelcontextprotocol/clientInfo\":{\"name\":\"self-test\",\"version\":\"1.0\"},\"io.modelcontextprotocol/clientCapabilities\":{}}}}", Encoding.UTF8, "application/json")
                };
                recordEvent.Headers.Authorization = new("Bearer", server.Token);
                recordEvent.Headers.Accept.ParseAdd("application/json, text/event-stream");
                recordEvent.Headers.Add("MCP-Protocol-Version", "2026-07-28");
                recordEvent.Headers.Add("MCP-Method", "tools/call");
                recordEvent.Headers.Add("MCP-Name", "record_event");
                var recordResponse = await client.SendAsync(recordEvent);
                var recordJson = await recordResponse.Content.ReadAsStringAsync();
                var recordedState = await client.GetStringAsync($"{server.Address}/api/state?projectId=demo&workflowId=wf-1");
                var recordedDashboard = await client.GetStringAsync($"{server.Address}/dashboard?projectId=demo&workflowId=wf-1");
                using var sseTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                var changedEvent = await streamReader.ReadLineAsync(sseTimeout.Token);
                if (!recordResponse.IsSuccessStatusCode) throw new InvalidOperationException($"MCP record_event 호출 실패: {recordJson}");
                if (denied.StatusCode != HttpStatusCode.Unauthorized || !stateResponse.IsSuccessStatusCode || !dashboardResponse.IsSuccessStatusCode || !streamResponse.IsSuccessStatusCode || initialEvent != "event: state" || initialData is null || changedEvent != "event: state" || !recordResponse.IsSuccessStatusCode || !recordJson.Contains("evt-mcp-1") || !recordedState.Contains("mcp-node") || !recordedDashboard.Contains("최근 이벤트") || !recordedDashboard.Contains("mcp-node"))
                    throw new InvalidOperationException("로컬 서버 검증이 실패했습니다.");
            }
            await using (var host = new LocalServerHost(serverPath))
            {
                await host.StartAsync();
                var platform = new TestPlatformActions();
                var viewModel = new MainWindowViewModel(host, platform);
                await viewModel.LoadAsync();
                if (!viewModel.Workspace.OpenDashboardCommand.CanExecute(null)) throw new InvalidOperationException("대시보드 명령 활성화가 실패했습니다.");
                viewModel.Server.CopyTokenCommand.Execute(null);
                viewModel.Workspace.OpenDashboardCommand.Execute(null);
                await viewModel.Workspace.Monitor.RefreshAsync("demo", "wf-1");
                var summary = await host.GenerateSummaryAsync("demo", "wf-1");
                var export = await host.ExportAsync("demo", "wf-1");
                if (platform.CopiedToken != host.Token || !platform.OpenedUrl.Contains("projectId=demo") || viewModel.Workspace.Monitor.Nodes.Count == 0 || summary.Content.Length == 0 || !File.Exists(export.ZipPath))
                    throw new InvalidOperationException("WPF 서버 제어·요약·내보내기 검증이 실패했습니다.");
                await host.StopAsync();
                if (host.IsRunning || !File.ReadAllText(host.LogPath).Contains("server started") || !File.ReadAllText(host.LogPath).Contains("server stopped"))
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
        public string CopiedToken { get; private set; } = "";
        public string OpenedUrl { get; private set; } = "";
        public bool TryCopyToClipboard(string value) { CopiedToken = value; return true; }
        public bool TryOpenBrowser(string url) { OpenedUrl = url; return true; }
    }
}
