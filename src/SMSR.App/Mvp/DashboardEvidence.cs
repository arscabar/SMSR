using System.Text;

namespace SMSR.App.Mvp;

internal static class DashboardEvidence
{
    public static string Render(IReadOnlyList<WorkflowEvidenceLink> evidence,
        IReadOnlyList<WorkflowEvent> events, WorkflowPlan plan)
    {
        var html = new StringBuilder($"<details class=\"evidence-disclosure\" data-disclosure=\"evidence\"><summary><span class=\"evidence-icon\" aria-hidden=\"true\">▤</span><span class=\"evidence-heading\"><strong>산출물·검증 근거</strong><small>파일 · 확인 기록</small></span><span class=\"evidence-count\">{evidence.Count}건</span></summary>");
        if (evidence.Count == 0)
            return html.Append("<p class=\"empty\">아직 등록된 산출물·검증 근거가 없습니다.</p></details>").ToString();
        var visible = events.Select(item => item.EventId).ToHashSet(StringComparer.Ordinal);
        if (evidence.Count > 100)
            html.Append($"<p class=\"evidence-limit muted\">최근 100/{evidence.Count}건 · 전체는 내보내기에서 확인</p>");
        html.Append("<ul class=\"evidence-links\">");
        foreach (var item in evidence.TakeLast(100))
            html.Append(DashboardEvidenceCard.Render(item, plan, visible.Contains(item.EventId)));
        return html.Append("</ul></details>").ToString();
    }
}
