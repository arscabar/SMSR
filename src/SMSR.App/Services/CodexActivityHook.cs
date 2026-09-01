using System.IO;
using System.Text.Json;
using SMSR.App.Mvp;

namespace SMSR.App.Services;

internal static class CodexActivityHook
{
    public static async Task ProcessAsync(JsonElement input, string? dataPath = null)
    {
        var sessionId = HookJson.String(input, "session_id");
        if (string.IsNullOrWhiteSpace(sessionId)) return;
        dataPath ??= Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SMSR");
        var sessions = new TrackingSessionStore(dataPath);
        var tool = HookJson.String(input, "tool_name");
        var toolInput = HookJson.Object(input, "tool_input");
        var tracking = sessions.Load(sessionId) ?? CodexTrackingResolver.Resolve(input, toolInput, tool);
        if (tracking is null) return;

        var nodeId = toolInput is { } arguments && HookJson.String(arguments, "nodeId") is { Length: > 0 } node
            ? node : tracking.NodeId;
        tracking = tracking with { NodeId = nodeId, UpdatedAtUtc = DateTimeOffset.UtcNow };
        sessions.Save(sessionId, tracking);
        var agentId = HookJson.String(input, "agent_id");
        if (HookJson.String(input, "hook_event_name") == "SubagentStart" && agentId.Length > 0)
            sessions.Save(agentId, tracking);

        var eventName = HookJson.String(input, "hook_event_name");
        var turnId = HookJson.String(input, "turn_id");
        var toolUseId = HookJson.String(input, "tool_use_id");
        var record = new ActivityRecord(DateTimeOffset.UtcNow, tracking.ProjectId, tracking.WorkflowId,
            sessionId, CodexActivityClassifier.Event(eventName), CodexActivityClassifier.Category(eventName, tool), turnId,
            agentId.Length == 0 ? sessionId : agentId, nodeId, tool.Length == 0 ? null : tool,
            NullIfEmpty(toolUseId), CodexActivityClassifier.Identity(eventName, sessionId, turnId, agentId, tool, toolUseId));
        await new ActivityHookClient(dataPath).RecordAsync(record);

        if (eventName == "SubagentStop" && agentId.Length > 0) sessions.Remove(agentId);

        if (CodexActivityClassifier.IsTerminalEvent(tool, toolInput) && await ActivityHookClient.IsTerminalAsync(tracking))
            sessions.RemoveWorkflow(tracking.ProjectId, tracking.WorkflowId);
    }

    private static string? NullIfEmpty(string value) => value.Length == 0 ? null : value;
}
