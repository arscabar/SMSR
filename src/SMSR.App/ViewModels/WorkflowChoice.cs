namespace SMSR.App.ViewModels;

public sealed record WorkflowChoice(
    string ProjectId,
    string WorkflowId,
    string Title,
    string Status,
    int NodeCount,
    DateTimeOffset? UpdatedAtUtc)
{
    public DateTime? ActivityDate => UpdatedAtUtc?.ToLocalTime().Date;
    public string TimeLabel => UpdatedAtUtc?.ToLocalTime().ToString("HH:mm") ?? "시각 없음";
    public string DateTimeLabel => UpdatedAtUtc?.ToLocalTime().ToString("yyyy-MM-dd HH:mm") ?? "기록 시각 없음";
    public string StatusLabel => Status switch
    {
        "ACTIVE" => "진행 중",
        "TERMINAL" => "완료",
        _ => "계획 없음"
    };
    public string DisplayName => $"{Title} · {DateTimeLabel} · {StatusLabel}";
    public string CalendarMeta => $"{TimeLabel} · {ProjectId} · {StatusLabel} · 노드 {NodeCount}개";
}
