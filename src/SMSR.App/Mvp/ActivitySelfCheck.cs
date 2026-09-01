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
        if (records.Count != 3 || records[0].Event != "AGENT_STARTED"
            || records[1].Category != "FILE_EDIT" || records[2].Category != "SMSR"
            || text.Contains("SECRET-CONTENT", StringComparison.Ordinal)
            || text.Contains("SHOULD-NOT-EXIST", StringComparison.Ordinal)
            || new TrackingSessionStore(dataPath).Load(session)?.WorkflowId != "activity-wf"
            || new TrackingSessionStore(dataPath).Load("agent-child")?.WorkflowId != "activity-wf")
            throw new InvalidOperationException("Codex 훅 활동 JSONL 검증이 실패했습니다.");
        if (store.Append(records[0]) || store.ReadLatest("demo", "activity-wf", 10).Count != 3)
            throw new InvalidOperationException("Codex 훅 활동 중복 방지가 실패했습니다.");

        await CodexActivityHook.ProcessAsync(HookJsonDocument.Parse("""
            {"session_id":"activity-session","turn_id":"turn-1","hook_event_name":"SubagentStop",
             "agent_id":"agent-child","agent_type":"worker"}
            """), dataPath);
        if (new TrackingSessionStore(dataPath).Load("agent-child") is not null
            || store.ReadLatest("demo", "activity-wf", 10).Count != 4)
            throw new InvalidOperationException("하위 에이전트 활동 매핑 정리가 실패했습니다.");
        var isolated = await CodexHookRunner.ProcessAsync("""
            {"session_id":"isolated","cwd":"C:\\projects\\demo","hook_event_name":"UserPromptSubmit",
             "prompt":"PRIVATE-HOOK-INPUT"}
            """, _ => Task.FromException(new IOException("activity unavailable")));
        if (isolated is null || isolated.Contains("PRIVATE-HOOK-INPUT", StringComparison.Ordinal))
            throw new InvalidOperationException("활동 기록 실패 격리가 실패했습니다.");
        await ActivityStoreSelfCheck.RunAsync(dataPath);
    }

    private static class HookJsonDocument
    {
        public static System.Text.Json.JsonElement Parse(string json)
            => System.Text.Json.JsonDocument.Parse(json).RootElement.Clone();
    }
}
