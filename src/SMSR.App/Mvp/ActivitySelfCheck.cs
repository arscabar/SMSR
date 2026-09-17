using System.IO;
using SMSR.App.Services;

namespace SMSR.App.Mvp;

internal static class ActivitySelfCheck
{
    public static async Task RunAsync(string dataPath)
    {
        var session = "activity-session";
        await CodexActivityHook.ProcessAsync(HookJsonDocument.Parse("""
            {"session_id":"activity-session","turn_id":"turn-1","hook_event_name":"PostToolUse",
             "tool_name":"mcp__smsr__save_plan","tool_use_id":"tool-1","tool_input":{"projectId":"demo"},
             "tool_response":{"content":[{"type":"text","text":"{\"workflowId\":\"activity-wf\"}"}]}}
            """), dataPath);
        await CodexActivityHook.ProcessAsync(HookJsonDocument.Parse("""
            {"session_id":"activity-session","turn_id":"turn-1","hook_event_name":"PreToolUse",
             "tool_name":"apply_patch","tool_use_id":"tool-2","tool_input":{"command":"SECRET-CONTENT"}}
            """), dataPath);
        await CodexActivityHook.ProcessAsync(HookJsonDocument.Parse("""
            {"session_id":"activity-session","turn_id":"turn-1","hook_event_name":"PostToolUse",
             "tool_name":"apply_patch","tool_use_id":"tool-2","tool_input":{"command":"SECRET-CONTENT"}}
            """), dataPath);
        await CodexActivityHook.ProcessAsync(HookJsonDocument.Parse("""
            {"session_id":"activity-session","turn_id":"turn-1","hook_event_name":"SubagentStart",
             "agent_id":"agent-child","agent_type":"worker"}
            """), dataPath);
        await CodexActivityHook.ProcessAsync(HookJsonDocument.Parse("""
            {"session_id":"untracked","turn_id":"turn-2","hook_event_name":"PostToolUse",
             "tool_name":"Bash","tool_input":{"command":"SHOULD-NOT-EXIST"}}
            """), dataPath);

        var store = new ActivityJsonlStore(dataPath);
        var records = store.ReadLatest("demo", "activity-wf", 10);
        var text = File.ReadAllText(store.PathFor("demo", "activity-wf"));
        if (records.Count != 4 || records[0].Event != "AGENT_STARTED"
            || records[1].Event != "TOOL_COMPLETED" || records[1].Category != "FILE_EDIT"
            || records[2].Event != "TOOL_STARTED" || records[2].Category != "FILE_EDIT"
            || records[3].Category != "SMSR"
            || text.Contains("SECRET-CONTENT", StringComparison.Ordinal)
            || text.Contains("SHOULD-NOT-EXIST", StringComparison.Ordinal)
            || new TrackingSessionStore(dataPath).Load(session)?.WorkflowId != "activity-wf"
            || new TrackingSessionStore(dataPath).Load("agent-child")?.WorkflowId != "activity-wf")
            throw new InvalidOperationException("Codex 훅 활동 JSONL 검증이 실패했습니다.");
        var liveActivity = DashboardPanels.RenderActivities([records[2]], new("demo", "activity-wf", []));
        if (!liveActivity.Contains("작업 시작", StringComparison.Ordinal)
            || !liveActivity.Contains("running-time", StringComparison.Ordinal)
            || !liveActivity.Contains("파일 변경", StringComparison.Ordinal))
            throw new InvalidOperationException("실시간 도구 시작 표시 검증이 실패했습니다.");
        if (store.Append(records[0]) || store.ReadLatest("demo", "activity-wf", 10).Count != 4)
            throw new InvalidOperationException("Codex 훅 활동 중복 방지가 실패했습니다.");

        await CodexActivityHook.ProcessAsync(HookJsonDocument.Parse("""
            {"session_id":"activity-session","turn_id":"turn-2","hook_event_name":"PostToolUse",
             "tool_name":"mcp__smsr__save_plan","tool_use_id":"tool-3","tool_input":{"projectId":"project-b"},
             "tool_response":{"content":[{"type":"text","text":"{\"workflowId\":\"workflow-b\"}"}]}}
            """), dataPath);
        await CodexActivityHook.ProcessAsync(HookJsonDocument.Parse("""
            {"session_id":"activity-session","turn_id":"turn-2","hook_event_name":"PostToolUse",
             "tool_name":"apply_patch","tool_use_id":"tool-4","tool_input":{}}
            """), dataPath);
        if (store.ReadLatest("project-b", "workflow-b", 10).Count != 2
            || new TrackingSessionStore(dataPath).Load(session)?.ProjectId != "project-b")
            throw new InvalidOperationException("같은 Codex 세션의 다중 프로젝트 전환 검증이 실패했습니다.");

        await CodexActivityHook.ProcessAsync(HookJsonDocument.Parse("""
            {"session_id":"activity-session","turn_id":"turn-1","hook_event_name":"SubagentStop",
             "agent_id":"agent-child","agent_type":"worker"}
            """), dataPath);
        if (new TrackingSessionStore(dataPath).Load("agent-child") is not null
            || store.ReadLatest("demo", "activity-wf", 10).Count != 4
            || store.ReadLatest("project-b", "workflow-b", 10).Count != 3)
            throw new InvalidOperationException("하위 에이전트 활동 매핑 정리가 실패했습니다.");
        VerifyTokenUsage(dataPath, store);
        var isolated = await CodexHookRunner.ProcessAsync("""
            {"session_id":"isolated","cwd":"C:\\projects\\demo","hook_event_name":"UserPromptSubmit",
             "prompt":"PRIVATE-HOOK-INPUT"}
            """, _ => Task.FromException(new IOException("activity unavailable")));
        if (isolated is null || isolated.Contains("PRIVATE-HOOK-INPUT", StringComparison.Ordinal))
            throw new InvalidOperationException("활동 기록 실패 격리가 실패했습니다.");
        await ActivityStoreSelfCheck.RunAsync(dataPath);
    }

    private static void VerifyTokenUsage(string dataPath, ActivityJsonlStore store)
    {
        var sessionId = "token-session";
        var sessionsRoot = Path.Combine(dataPath, "codex-sessions");
        var sessionDirectory = Path.Combine(sessionsRoot, "2026", "09", "16");
        Directory.CreateDirectory(sessionDirectory);
        var rollout = Path.Combine(sessionDirectory, $"rollout-{sessionId}.jsonl");
        File.WriteAllText(rollout, """
            {"type":"token_usage_record","payload":{"thread_token_usage":{"input_tokens":1400,"cached_input_tokens":1200,"output_tokens":350}}}
            {"type":"event_msg","payload":{"type":"token_count","info":{"total_token_usage":{"input_tokens":1200,"output_tokens":300}}}}
            """);
        var read = CodexTokenUsageReader.Read(sessionId, sessionsRoot: sessionsRoot);
        if (read?.Input != 1400 || read.CachedInput != 1200 || read.Output != 350)
            throw new InvalidOperationException("Codex 토큰 사용량 읽기 검증이 실패했습니다.");

        var recorder = new TokenUsageRecorder(dataPath, store, sessionsRoot);
        recorder.Capture("tokens", "graph-direct", sessionId, "node", "plan request", "plan response");
        File.AppendAllText(rollout, Environment.NewLine + """
            {"type":"token_usage_record","payload":{"thread_token_usage":{"input_tokens":1600,"cached_input_tokens":1350,"output_tokens":400}}}
            """);
        recorder.Capture("tokens", "graph-direct", sessionId, "node", "event request", "event response");
        var direct = store.ReadTokenUsage("tokens", "graph-direct");
        var expectedGraphInput = TokenUsageRecorder.Estimate("plan response") + TokenUsageRecorder.Estimate("event response");
        var expectedGraphOutput = TokenUsageRecorder.Estimate("plan request") + TokenUsageRecorder.Estimate("event request");
        if (!direct.HasGoalUsage || !direct.HasGraphUsage || direct.GoalInput != 200
            || direct.GoalCachedInput != 150 || !direct.HasGoalBreakdown
            || direct.GoalOutput != 50 || direct.GraphInput != expectedGraphInput
            || direct.GraphOutput != expectedGraphOutput)
            throw new InvalidOperationException("MCP 직접 토큰 스냅샷 검증이 실패했습니다.");

        var now = DateTimeOffset.UtcNow;
        store.Append(new(now, "tokens", "graph-a", sessionId, "TURN_STOPPED", "LIFECYCLE",
            ActivityId: "token-a-root", GoalId: "goal-1", SessionInputTokens: 1000,
            SessionOutputTokens: 100, GraphInputTokens: 400, GraphOutputTokens: 40,
            SessionCachedInputTokens: 800));
        store.Append(new(now.AddSeconds(1), "tokens", "graph-a", "token-child", "TURN_STOPPED", "LIFECYCLE",
            ActivityId: "token-a-child", GoalId: "goal-1", SessionInputTokens: 200,
            SessionOutputTokens: 20, GraphInputTokens: 200, GraphOutputTokens: 20,
            SessionCachedInputTokens: 100));
        store.Append(new(now.AddSeconds(2), "tokens", "graph-b", sessionId, "TURN_STOPPED", "LIFECYCLE",
            ActivityId: "token-b-root", GoalId: "goal-1", SessionInputTokens: 1500,
            SessionOutputTokens: 150, GraphInputTokens: 100, GraphOutputTokens: 10,
            SessionCachedInputTokens: 1200));
        var summary = store.ReadTokenUsage("tokens", "graph-a");
        var page = DashboardPage.Render(new("tokens", "graph-a", []), new("tokens", "graph-a", []), [],
            tokenUsage: summary);
        if (!summary.HasGoalUsage || !summary.HasGraphUsage || summary.GoalInput != 1200
            || summary.GoalCachedInput != 900 || !summary.HasGoalBreakdown
            || summary.GoalOutput != 120 || summary.GraphInput != 600 || summary.GraphOutput != 60
            || !page.Contains("목표 작업 토큰", StringComparison.Ordinal)
            || !page.Contains("<b>신규 입력</b><strong>300</strong>", StringComparison.Ordinal)
            || !page.Contains("<b>캐시 입력</b><strong>900</strong>", StringComparison.Ordinal)
            || page.Contains("title=\"신규 입력", StringComparison.Ordinal)
            || !page.Contains("그래프(추정) 토큰", StringComparison.Ordinal))
            throw new InvalidOperationException("목표·그래프 토큰 집계 표시 검증이 실패했습니다.");
    }

    private static class HookJsonDocument
    {
        public static System.Text.Json.JsonElement Parse(string json)
            => System.Text.Json.JsonDocument.Parse(json).RootElement.Clone();
    }
}
