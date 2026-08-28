namespace SMSR.App.Mvp;

public static class EventValidation
{
    private static readonly HashSet<string> Statuses =
    ["PENDING", "IN_PROGRESS", "VALIDATING", "SUCCESS", "FAILED", "RETRYING", "BLOCKED"];

    public static string? Validate(RecordEventRequest request)
    {
        if (ValidateIds(request.EventId, request.ProjectId, request.WorkflowId, request.NodeId, request.AgentId) is { } error) return error;
        if (request.EventType != "NODE_STATUS_CHANGED") return "eventType은 NODE_STATUS_CHANGED여야 합니다.";
        if (!Statuses.Contains(request.Status)) return "지원하지 않는 status입니다.";
        if (request.AgentRole?.Length > 128 || request.NextAction?.Length > 2000 || request.Summary?.Length > 2000 || request.Error?.Length > 2000)
            return "agentRole은 128자, summary·error·nextAction은 2,000자 이하여야 합니다.";
        if (request.ProgressPercentage is < 0 or > 100 || request.RetryCount is < 0 or > 1000)
            return "progressPercentage는 0~100, retryCount는 0~1,000이어야 합니다.";
        if (request.Commands?.Count > 100 || request.Artifacts?.Count > 100 || request.Commands?.Any(TooLong) == true || request.Artifacts?.Any(TooLong) == true)
            return "commands와 artifacts는 각각 100개, 항목당 1,000자 이하여야 합니다.";
        return null;
    }

    public static string? Validate(AgentHeartbeatRequest request)
    {
        if (ValidateIds(request.ProjectId, request.WorkflowId, request.AgentId, request.AgentRole) is { } error) return error;
        if (request.NodeId is { Length: > 128 } || request.Summary?.Length > 2000) return "nodeId는 128자, summary는 2,000자 이하여야 합니다.";
        if (request.RetryCount is < 0 or > 1000) return "retryCount는 0~1,000이어야 합니다.";
        return request.Status is "ACTIVE" or "IDLE" or "STOPPED" or "FAILED" ? null : "heartbeat status가 올바르지 않습니다.";
    }

    public static string? ValidateWorkflowIds(string projectId, string workflowId) => ValidateIds(projectId, workflowId);
    private static bool TooLong(string value) => value is null || value.Length > 1000;
    private static string? ValidateIds(params string[] values)
        => values.Any(value => string.IsNullOrWhiteSpace(value) || value.Length > 128) ? "식별자는 1~128자여야 합니다." : null;
}
