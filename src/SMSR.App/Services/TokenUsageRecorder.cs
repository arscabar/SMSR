using SMSR.App.Mvp;
using System.Text;

namespace SMSR.App.Services;

public sealed class TokenUsageRecorder(string dataPath, ActivityJsonlStore activity,
    string? sessionsRoot = null)
{
    public void Capture(string projectId, string workflowId, string sessionId, string? nodeId = null,
        string? graphRequest = null, string? graphResponse = null)
    {
        try
        {
            var sessions = new TrackingSessionStore(dataPath);
            var previous = sessions.Load(sessionId);
            var sameGraph = previous is not null && previous.ProjectId == projectId
                && previous.WorkflowId == workflowId;
            var usage = CodexTokenUsageReader.Read(sessionId, sameGraph ? previous!.RolloutPath : null,
                sessionsRoot: sessionsRoot);
            if (usage is null && graphRequest is null && graphResponse is null) return;
            var goalId = sameGraph ? previous!.GoalId ?? workflowId : workflowId;
            var goalInputBase = sameGraph ? previous!.GoalInputBase ?? usage?.Input : usage?.Input;
            var goalOutputBase = sameGraph ? previous!.GoalOutputBase ?? usage?.Output : usage?.Output;
            var goalCachedInputBase = sameGraph
                ? previous!.GoalCachedInputBase ?? usage?.CachedInput : usage?.CachedInput;
            var graphInput = (sameGraph ? previous!.GraphInputTotal : 0) ?? 0;
            var graphOutput = (sameGraph ? previous!.GraphOutputTotal : 0) ?? 0;
            graphInput += Estimate(graphResponse);
            graphOutput += Estimate(graphRequest);
            sessions.Save(sessionId, new(projectId, workflowId, nodeId, DateTimeOffset.UtcNow,
                goalId, usage?.RolloutPath ?? previous?.RolloutPath, goalInputBase, goalOutputBase,
                graphInput, graphOutput, goalCachedInputBase));
            activity.Append(new(DateTimeOffset.UtcNow, projectId, workflowId, sessionId,
                "TOKEN_SNAPSHOT", "LIFECYCLE", AgentId: sessionId, NodeId: nodeId,
                ActivityId: $"tokens:{sessionId}:{usage?.Input}:{usage?.Output}:{graphInput}:{graphOutput}", GoalId: goalId,
                SessionInputTokens: usage is null || goalInputBase is null ? null : Math.Max(0, usage.Input - goalInputBase.Value),
                SessionOutputTokens: usage is null || goalOutputBase is null ? null : Math.Max(0, usage.Output - goalOutputBase.Value),
                GraphInputTokens: graphInput, GraphOutputTokens: graphOutput,
                SessionCachedInputTokens: usage is null || goalCachedInputBase is null ? null
                    : Math.Max(0, usage.CachedInput - goalCachedInputBase.Value)));
        }
        catch { }
    }

    internal static long Estimate(string? value) => string.IsNullOrEmpty(value)
        ? 0 : Math.Max(1, (Encoding.UTF8.GetByteCount(value) + 3L) / 4L);
}
