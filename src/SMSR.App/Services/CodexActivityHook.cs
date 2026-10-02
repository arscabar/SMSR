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
        var hookAgentId = HookJson.String(input, "agent_id");
        var childAtStop = eventName == "SubagentStop" && hookAgentId.Length > 0
            ? sessions.Load(hookAgentId) : null;
        var resolved = CodexTrackingResolver.Resolve(input, toolInput, tool);
        var tracking = resolved ?? previous ?? childAtStop;
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
            GoalInputBase = previous?.GoalInputBase ?? usage?.Input,
            GoalOutputBase = previous?.GoalOutputBase ?? usage?.Output,
            GraphInputTotal = sameGraph ? previous!.GraphInputTotal : 0,
            GraphOutputTotal = sameGraph ? previous!.GraphOutputTotal : 0,
            AgentId = previous?.AgentId
        };

        var lifecycle = eventName is "SubagentStart" or "SubagentStop";
        var explicitAgentId = resolved is not null && toolInput is { } trackedInput
            ? HookJson.String(trackedInput, "agentId") : "";
        var childTool = explicitAgentId.Length > 0 && sessions.Load(explicitAgentId) is not null;
        var nodeId = lifecycle ? null : toolInput is { } arguments && HookJson.String(arguments, "nodeId") is { Length: > 0 } node
            ? node : tracking.NodeId;
        tracking = tracking with { NodeId = lifecycle || childTool ? previous?.NodeId : nodeId,
            AgentId = explicitAgentId.Length > 0 && !childTool ? explicitAgentId : tracking.AgentId,
            UpdatedAtUtc = DateTimeOffset.UtcNow };
        if (previous is not null || childAtStop is null) sessions.Save(sessionId, tracking);
        var agentId = explicitAgentId.Length > 0 ? explicitAgentId : hookAgentId;
        if (eventName == "SubagentStart" && agentId.Length > 0)
            sessions.Save(agentId, tracking with { NodeId = null, AgentId = agentId,
                ParentAgentId = tracking.AgentId ?? sessionId,
                GoalId = tracking.GoalId ?? sessionId, RolloutPath = null,
                GoalInputBase = 0, GoalOutputBase = 0, GraphInputTotal = 0, GraphOutputTotal = 0 });

        var activityAgentId = agentId.Length == 0 ? tracking.AgentId ?? sessionId : agentId;
        var activityTracking = childAtStop ?? tracking;
        var turnId = HookJson.String(input, "turn_id");
        var toolUseId = HookJson.String(input, "tool_use_id");
        var record = new ActivityRecord(DateTimeOffset.UtcNow, activityTracking.ProjectId, activityTracking.WorkflowId,
            sessionId, CodexActivityClassifier.Event(eventName), CodexActivityClassifier.Category(eventName, tool), turnId,
            activityAgentId, nodeId, tool.Length == 0 ? null : tool,
            NullIfEmpty(toolUseId), CodexActivityClassifier.Identity(eventName, sessionId, turnId, agentId, tool, toolUseId),
            activityTracking.GoalId, Delta(usage?.Input, activityTracking.GoalInputBase),
            Delta(usage?.Output, activityTracking.GoalOutputBase),
            activityTracking.GraphInputTotal, activityTracking.GraphOutputTotal,
            AgentRole: lifecycle ? NullIfEmpty(HookJson.String(input, "agent_type")) : null,
            Model: lifecycle || explicitAgentId.Length == 0 ? NullIfEmpty(HookJson.String(input, "model")) : null,
            ReasoningEffort: lifecycle || explicitAgentId.Length == 0 ? KnownEffort(HookJson.String(input, "model_reasoning_effort")) : null,
            ParentAgentId: lifecycle ? (childAtStop is not null
                ? childAtStop.ParentAgentId ?? sessionId : tracking.AgentId ?? sessionId) : null);
        await new ActivityHookClient(dataPath).RecordAsync(record);

        if (eventName == "SubagentStop" && agentId.Length > 0) sessions.Remove(agentId);

        if (eventName is "Stop" or "SessionEnd" && await ActivityHookClient.IsTerminalAsync(tracking))
            sessions.RemoveWorkflow(tracking.ProjectId, tracking.WorkflowId);
    }

    private static string? NullIfEmpty(string value) => value.Length == 0 ? null : value;
    private static string? KnownEffort(string value) => value is "none" or "minimal" or "low" or "medium"
        or "high" or "xhigh" or "max" or "ultra" ? value : null;
    private static long? Delta(long? value, long? baseline)
        => value is null || baseline is null ? null : Math.Max(0, value.Value - baseline.Value);
}
