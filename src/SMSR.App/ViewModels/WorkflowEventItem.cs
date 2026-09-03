using SMSR.App.Mvp;

namespace SMSR.App.ViewModels;

public sealed record WorkflowEventItem(RecentEvent Source)
{
    public string Title => string.IsNullOrWhiteSpace(Source.Summary) ? Source.NodeId : Source.Summary;
    public string Meta => $"{Source.CreatedAt.ToLocalTime():yyyy-MM-dd HH:mm:ss} · {Source.Status} · {Source.AgentId}";
    public string Artifacts => Source.Artifacts is { Count: > 0 }
        ? $"산출물: {string.Join(" · ", Source.Artifacts)}" : "";
}
