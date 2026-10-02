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
    public string DateTimeLabel => UpdatedAtUtc?.ToLocalTime().ToString("yyyy-MM-dd HH:mm") ?? "기록 시각 없음";
    public string DisplayName => Status == "BLOCKED" ? $"{Title} · 확인 필요" : Title;
    public override string ToString() => DisplayName;
}
