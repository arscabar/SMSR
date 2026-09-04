namespace SMSR.App.Mvp;

public sealed record DailyActivityRequest(
    string ActivityId,
    string ProjectId,
    string TaskId,
    string Title,
    string Summary,
    string Status = "SUCCESS",
    IReadOnlyList<string>? Files = null,
    IReadOnlyList<string>? Verifications = null,
    IReadOnlyList<string>? Artifacts = null,
    string? WorkflowId = null,
    string? AgentId = null);

public sealed record DailyActivity(
    string ActivityId,
    string ProjectId,
    string TaskId,
    string Title,
    string Summary,
    string Status,
    IReadOnlyList<string> Files,
    IReadOnlyList<string> Verifications,
    IReadOnlyList<string> Artifacts,
    string? WorkflowId,
    string? AgentId,
    DateTimeOffset RecordedAtUtc);

public static class DailyActivityValidation
{
    private static readonly HashSet<string> Statuses = ["SUCCESS", "FAILED", "BLOCKED"];

    public static string? Validate(DailyActivityRequest request)
    {
        if (new[] { request.ActivityId, request.ProjectId, request.TaskId }.Any(InvalidId))
            return "activityId, projectId, taskId는 1~128자여야 합니다.";
        if (string.IsNullOrWhiteSpace(request.Title) || request.Title.Length > 200
            || string.IsNullOrWhiteSpace(request.Summary) || request.Summary.Length > 2000)
            return "title은 1~200자, summary는 1~2,000자여야 합니다.";
        if (!Statuses.Contains(request.Status)) return "status는 SUCCESS, FAILED, BLOCKED 중 하나여야 합니다.";
        if (request.WorkflowId?.Length > 128 || request.AgentId?.Length > 128)
            return "workflowId와 agentId는 128자 이하여야 합니다.";
        return InvalidList(request.Files) || InvalidList(request.Verifications) || InvalidList(request.Artifacts)
            ? "파일·검증·산출물은 각각 100개, 항목당 1,000자 이하여야 합니다." : null;
    }

    private static bool InvalidId(string value) => string.IsNullOrWhiteSpace(value) || value.Length > 128;
    private static bool InvalidList(IReadOnlyList<string>? values)
        => values?.Count > 100 || values?.Any(value => string.IsNullOrWhiteSpace(value) || value.Length > 1000) == true;
}
