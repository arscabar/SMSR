namespace SMSR.App.Mvp;

public sealed record ActivityRecord(
    DateTimeOffset TimestampUtc,
    string ProjectId,
    string WorkflowId,
    string SessionId,
    string Event,
    string Category,
    string? TurnId = null,
    string? AgentId = null,
    string? NodeId = null,
    string? ToolName = null,
    string? ToolUseId = null,
    string? ActivityId = null);

internal sealed record TrackingSession(
    string ProjectId,
    string WorkflowId,
    string? NodeId,
    DateTimeOffset UpdatedAtUtc);
