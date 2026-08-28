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
    IReadOnlyList<string>? Artifacts,
    string? AgentRole = null,
    int? ProgressPercentage = null,
    int RetryCount = 0,
    string? NextAction = null,
    DateTimeOffset? HeartbeatAt = null);

public sealed record StateNode(
    string NodeId,
    string AgentId,
    string Status,
    string? Summary,
    string? Error,
    DateTimeOffset UpdatedAt,
    string? AgentRole = null,
    int? ProgressPercentage = null,
    int RetryCount = 0,
    string? NextAction = null,
    IReadOnlyList<string>? Artifacts = null,
    DateTimeOffset? HeartbeatAt = null);

public sealed record WorkflowState(
    string ProjectId,
    string WorkflowId,
    IReadOnlyList<StateNode> Nodes,
    IReadOnlyList<AgentState>? Agents = null);

public sealed record RecentEvent(
    string NodeId,
    string AgentId,
    string Status,
    string? Summary,
    string? Error,
    DateTimeOffset CreatedAt,
    string? AgentRole = null,
    int? ProgressPercentage = null,
    int RetryCount = 0,
    IReadOnlyList<string>? Artifacts = null);

public sealed record WorkflowEvent(
    string EventId,
    string NodeId,
    string AgentId,
    string EventType,
    string Status,
    string? Summary,
    string? Error,
    DateTimeOffset CreatedAt,
    string? AgentRole = null,
    int? ProgressPercentage = null,
    int RetryCount = 0,
    IReadOnlyList<string>? Artifacts = null);

public sealed record AgentHeartbeatRequest(
    string ProjectId,
    string WorkflowId,
    string AgentId,
    string AgentRole,
    string Status = "ACTIVE",
    string? NodeId = null,
    string? Summary = null,
    int RetryCount = 0);

public sealed record AgentState(
    string AgentId,
    string AgentRole,
    string Status,
    string? NodeId,
    string? Summary,
    int RetryCount,
    DateTimeOffset LastHeartbeatAt,
    bool IsStale);

public sealed record WorkflowSummary(
    string ProjectId,
    string WorkflowId,
    string Content,
    DateTimeOffset CreatedAt);

public sealed record ExportResult(string DirectoryPath, string ZipPath);
