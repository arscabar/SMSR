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
    string? ActivityId = null,
    string? GoalId = null,
    long? SessionInputTokens = null,
    long? SessionOutputTokens = null,
    long? GraphInputTokens = null,
    long? GraphOutputTokens = null);

internal sealed record TrackingSession(
    string ProjectId,
    string WorkflowId,
    string? NodeId,
    DateTimeOffset UpdatedAtUtc,
    string? GoalId = null,
    string? RolloutPath = null,
    long? GraphInputBase = null,
    long? GraphOutputBase = null);

public sealed record TokenUsageSummary(long GoalInput, long GoalOutput, long GraphInput, long GraphOutput,
    bool HasGoalUsage, bool HasGraphUsage);
