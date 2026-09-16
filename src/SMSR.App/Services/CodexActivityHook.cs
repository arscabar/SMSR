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
        var eventName = HookJson.String(input, "hook_event_name");
        var previous = sessions.Load(sessionId);
        var resolved = CodexTrackingResolver.Resolve(input, toolInput, tool);
        var tracking = resolved ?? previous;
        if (tracking is null) return;

        var sameGraph = previous is not null && previous.ProjectId == tracking.ProjectId
            && previous.WorkflowId == tracking.WorkflowId;
        var usage = resolved is not null || eventName is "Stop" or "SessionEnd" or "SubagentStop"
            ? CodexTokenUsageReader.Read(sessionId, sameGraph ? previous!.RolloutPath : null,
                hookPath: HookJson.String(input, "transcript_path")) : null;
        tracking = tracking with
        {
            GoalId = previous?.GoalId ?? sessionId,
            RolloutPath = usage?.RolloutPath ?? previous?.RolloutPath,
            GraphInputBase = sameGraph ? previous!.GraphInputBase ?? usage?.Input : usage?.Input,
            GraphOutputBase = sameGraph ? previous!.GraphOutputBase ?? usage?.Output : usage?.Output
        };

        var nodeId = toolInput is { } arguments && HookJson.String(arguments, "nodeId") is { Length: > 0 } node
            ? node : tracking.NodeId;
        tracking = tracking with { NodeId = nodeId, UpdatedAtUtc = DateTimeOffset.UtcNow };
        sessions.Save(sessionId, tracking);
        var agentId = HookJson.String(input, "agent_id");
        if (HookJson.String(input, "hook_event_name") == "SubagentStart" && agentId.Length > 0)
            sessions.Save(agentId, tracking with { GoalId = tracking.GoalId ?? sessionId, RolloutPath = null,
                GraphInputBase = 0, GraphOutputBase = 0 });

        var turnId = HookJson.String(input, "turn_id");
        var toolUseId = HookJson.String(input, "tool_use_id");
        var record = new ActivityRecord(DateTimeOffset.UtcNow, tracking.ProjectId, tracking.WorkflowId,
            sessionId, CodexActivityClassifier.Event(eventName), CodexActivityClassifier.Category(eventName, tool), turnId,
            agentId.Length == 0 ? sessionId : agentId, nodeId, tool.Length == 0 ? null : tool,
            NullIfEmpty(toolUseId), CodexActivityClassifier.Identity(eventName, sessionId, turnId, agentId, tool, toolUseId),
            tracking.GoalId, usage?.Input, usage?.Output,
            Delta(usage?.Input, tracking.GraphInputBase), Delta(usage?.Output, tracking.GraphOutputBase));
        await new ActivityHookClient(dataPath).RecordAsync(record);

        if (eventName == "SubagentStop" && agentId.Length > 0) sessions.Remove(agentId);

        if (eventName is "Stop" or "SessionEnd" && await ActivityHookClient.IsTerminalAsync(tracking))
            sessions.RemoveWorkflow(tracking.ProjectId, tracking.WorkflowId);
    }

    private static string? NullIfEmpty(string value) => value.Length == 0 ? null : value;
    private static long? Delta(long? value, long? baseline)
        => value is null || baseline is null ? null : Math.Max(0, value.Value - baseline.Value);
}
