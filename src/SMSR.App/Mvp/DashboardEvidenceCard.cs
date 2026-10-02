namespace SMSR.App.Mvp;

internal static class DashboardEvidenceCard
{
    public static string Render(WorkflowEvidenceLink item, WorkflowPlan plan, bool linked)
    {
        var label = DashboardPanels.Encode(item.Reference);
        var node = DashboardPanels.Encode(plan.Nodes.FirstOrDefault(value => value.NodeId == item.NodeId)?.Title ?? item.NodeId);
        var href = DashboardPanels.Encode($"#event-{Uri.EscapeDataString(item.EventId)}");
        var reference = linked
            ? $"<a class=\"evidence-reference\" href=\"{href}\" aria-label=\"실행 기록에서 {label} 확인\">{label}<span class=\"evidence-action\">실행 기록 보기 ↗</span></a>"
            : $"<strong class=\"evidence-reference\">{label}</strong><span class=\"evidence-action muted\">이전 기록 · 내보내기에서 확인</span>";
        return $"<li class=\"evidence-card\"><span class=\"evidence-kind\">작업 근거</span>{reference}<div class=\"evidence-meta\"><span>{node}</span><time datetime=\"{item.CreatedAt:O}\">{item.CreatedAt.ToLocalTime():MM-dd HH:mm}</time></div></li>";
    }
}
