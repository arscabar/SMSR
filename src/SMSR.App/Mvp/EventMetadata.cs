using System.Text.Json;

namespace SMSR.App.Mvp;

internal sealed record EventMetadata(
    string? AgentRole,
    int? ProgressPercentage,
    int RetryCount,
    string? NextAction,
    IReadOnlyList<string>? Artifacts,
    DateTimeOffset? HeartbeatAt)
{
    public static EventMetadata From(RecordEventRequest request, DateTimeOffset heartbeatAt)
        => new(request.AgentRole, WorkflowProgress.Value(request.Status, request.ProgressPercentage), request.RetryCount, request.NextAction, request.Artifacts, heartbeatAt);

    public static EventMetadata Parse(string json)
        => JsonSerializer.Deserialize<EventMetadata>(json) ?? new(null, null, 0, null, null, null);
}

internal sealed record PlanNodeMetadata(
    string? ParentNodeId,
    string? AssignedAgentId,
    string? AgentRole,
    string? CompletionCriteria)
{
    public static PlanNodeMetadata From(PlanNodeDefinition node)
        => new(node.ParentNodeId, node.AssignedAgentId, node.AgentRole, node.CompletionCriteria);

    public static PlanNodeMetadata Parse(string json)
        => JsonSerializer.Deserialize<PlanNodeMetadata>(json) ?? new(null, null, null, null);
}
