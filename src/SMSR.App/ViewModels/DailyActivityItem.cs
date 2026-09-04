using SMSR.App.Mvp;

namespace SMSR.App.ViewModels;

public sealed record DailyActivityItem(
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
    DateTimeOffset RecordedAtUtc)
{
    public string TimeLabel => RecordedAtUtc.ToLocalTime().ToString("HH:mm");
    public string StatusLabel => Status switch { "SUCCESS" => "완료", "FAILED" => "실패", _ => "중단" };
    public string Meta => $"{TimeLabel} · {ProjectId} · {StatusLabel}";
    public string FileLabel => Files.Count == 0 ? "변경 파일 없음" : $"변경 파일 {Files.Count}개 · {Preview(Files, 4)}";
    public string VerificationLabel => Verifications.Count == 0 ? "검증 기록 없음" : Preview(Verifications, 3);
    public string GraphLabel => string.IsNullOrWhiteSpace(WorkflowId) ? "간단 작업 · 일일 기록만" : "그래프 연결됨 · 선택하여 열기";

    public static DailyActivityItem From(DailyActivity activity)
        => new(activity.ActivityId, activity.ProjectId, activity.TaskId, activity.Title, activity.Summary,
            activity.Status, activity.Files, activity.Verifications, activity.Artifacts,
            activity.WorkflowId, activity.RecordedAtUtc);

    private static string Preview(IReadOnlyList<string> values, int limit)
        => string.Join(" · ", values.Take(limit)) + (values.Count > limit ? $" · 외 {values.Count - limit}개" : "");
}
