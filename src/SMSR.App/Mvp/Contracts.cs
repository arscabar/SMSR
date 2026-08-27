namespace SMSR.App.Mvp;

public sealed record RecordEventRequest(
    string EventId,
    string ProjectId,
    string WorkflowId,
    string NodeId,
    string AgentId,
    string EventType,
    string Status,
    string? Summary,
    string? Error,
    IReadOnlyList<string>? Commands,
    IReadOnlyList<string>? Artifacts);

public sealed record StateNode(
    string NodeId,
    string AgentId,
    string Status,
    string? Summary,
    string? Error,
    DateTimeOffset UpdatedAt);

public sealed record WorkflowState(
    string ProjectId,
    string WorkflowId,
    IReadOnlyList<StateNode> Nodes);

public sealed record RecentEvent(
    string NodeId,
    string AgentId,
    string Status,
    string? Summary,
    string? Error,
    DateTimeOffset CreatedAt);

public sealed record WorkflowEvent(
    string EventId,
    string NodeId,
    string AgentId,
    string EventType,
    string Status,
    string? Summary,
    string? Error,
    DateTimeOffset CreatedAt);

public sealed record WorkflowSummary(
    string ProjectId,
    string WorkflowId,
    string Content,
    DateTimeOffset CreatedAt);

public sealed record ExportResult(string DirectoryPath, string ZipPath);

public static class EventValidation
{
    private static readonly HashSet<string> Statuses =
    ["PENDING", "IN_PROGRESS", "VALIDATING", "SUCCESS", "FAILED", "RETRYING", "BLOCKED"];

    public static string? Validate(RecordEventRequest request)
    {
        foreach (var id in new[] { request.EventId, request.ProjectId, request.WorkflowId, request.NodeId, request.AgentId })
            if (string.IsNullOrWhiteSpace(id) || id.Length > 128) return "식별자는 1~128자여야 합니다.";

        if (request.EventType != "NODE_STATUS_CHANGED") return "eventType은 NODE_STATUS_CHANGED여야 합니다.";
        if (!Statuses.Contains(request.Status)) return "지원하지 않는 status입니다.";
        if (request.Summary?.Length > 2000 || request.Error?.Length > 2000) return "summary와 error는 2,000자 이하여야 합니다.";
        return null;
    }
}
