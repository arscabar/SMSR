using SMSR.App.Services;
using System.Text.Json;

namespace SMSR.App.Mvp;

internal static class AgentStopMappingSelfCheck
{
    public static async Task RunAsync(string dataPath)
    {
        var sessions = new TrackingSessionStore(dataPath);
        sessions.Save("detached-child", new("detached", "detached-wf", null,
            DateTimeOffset.UtcNow, AgentId: "detached-child", ParentAgentId: "detached-parent"));
        new TokenUsageRecorder(dataPath, new ActivityJsonlStore(dataPath)).Capture(
            "detached", "detached-wf", "detached-child", graphRequest: "test");
        using var document = JsonDocument.Parse("""
            {"session_id":"detached-parent","hook_event_name":"SubagentStop",
             "agent_id":"detached-child","agent_type":"worker"}
            """);
        await CodexActivityHook.ProcessAsync(document.RootElement, dataPath);
        var records = new ActivityJsonlStore(dataPath).ReadLatest("detached", "detached-wf");
        if (records.Count != 2 || records[0].Event != "AGENT_STOPPED"
            || records[0].AgentId != "detached-child"
            || records[0].ParentAgentId != "detached-parent"
            || sessions.Load("detached-child") is not null
            || sessions.Load("detached-parent") is not null)
            throw new InvalidOperationException("부모 매핑 종료 후 하위 종료 수집 실패.");
        sessions.Save("legacy-child", new("legacy", "legacy-wf", null,
            DateTimeOffset.UtcNow, AgentId: "legacy-child"));
        using var legacy = JsonDocument.Parse("""
            {"session_id":"legacy-parent","hook_event_name":"SubagentStop",
             "agent_id":"legacy-child","agent_type":"worker"}
            """);
        await CodexActivityHook.ProcessAsync(legacy.RootElement, dataPath);
        var stopped = new ActivityJsonlStore(dataPath).ReadLatest("legacy", "legacy-wf");
        if (stopped.Count != 1 || stopped[0].ParentAgentId != "legacy-parent")
            throw new InvalidOperationException("이전 매핑 부모 ID 복구 실패.");
    }
}
