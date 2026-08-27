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
        if (ValidateIds(request.EventId, request.ProjectId, request.WorkflowId, request.NodeId, request.AgentId) is { } error) return error;

        if (request.EventType != "NODE_STATUS_CHANGED") return "eventType은 NODE_STATUS_CHANGED여야 합니다.";
        if (!Statuses.Contains(request.Status)) return "지원하지 않는 status입니다.";
        if (request.Summary?.Length > 2000 || request.Error?.Length > 2000) return "summary와 error는 2,000자 이하여야 합니다.";
        if (request.Commands?.Count > 100 || request.Artifacts?.Count > 100 || request.Commands?.Any(value => value is null || value.Length > 1000) == true || request.Artifacts?.Any(value => value is null || value.Length > 1000) == true)
            return "commands와 artifacts는 각각 100개, 항목당 1,000자 이하여야 합니다.";
        return null;
    }

    public static string? ValidateWorkflowIds(string projectId, string workflowId) => ValidateIds(projectId, workflowId);

    private static string? ValidateIds(params string[] values)
        => values.Any(value => string.IsNullOrWhiteSpace(value) || value.Length > 128) ? "식별자는 1~128자여야 합니다." : null;
}
