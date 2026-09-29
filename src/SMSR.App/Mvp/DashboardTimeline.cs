using System.Text;

namespace SMSR.App.Mvp;

internal static partial class DashboardTimeline
{
    public static string Render(IReadOnlyList<PlanRevision> revisions, IReadOnlyList<WorkflowEvent> events,
        IReadOnlyList<WorkflowEvidenceLink> evidence, long eventCount, WorkflowPlan plan)
    {
        if (revisions.Count == 0 && events.Count == 0)
            return "<p class=\"empty\">계획 버전과 실행 기록이 없습니다.</p>";
        var entries = revisions.Select(item => (item.CreatedAt, 0, Html: Revision(item)))
            .Concat(events.Select(item => (item.CreatedAt, 1, Html: Event(item, plan))));
        var ordered = entries.OrderBy(item => item.CreatedAt).ThenBy(item => item.Item2).ToArray();
        var html = new StringBuilder("<section class=\"workflow-timeline\"><h3>계획·실행 순서</h3>");
        if (revisions.Count == 0) html.Append("<p class=\"empty\">이전 계획은 버전 이력 없음 · 현재 계획을 과거로 역산하지 않습니다.</p>");
        if (eventCount > events.Count) html.Append($"<p class=\"empty\">최근 {events.Count}개 이벤트만 화면에 표시합니다. 전체 기록은 내보내기의 events.jsonl에 있습니다.</p>");
        html.Append($"<label class=\"timeline-control\">기록 선택 <input id=\"timeline-step\" type=\"range\" min=\"1\" max=\"{ordered.Length}\" value=\"{ordered.Length}\"><output id=\"timeline-position\">{ordered.Length}/{ordered.Length}</output></label><ul id=\"timeline-list\">");
        for (var index = 0; index < ordered.Length; index++)
            html.Append(index == ordered.Length - 1 ? ordered[index].Html : ordered[index].Html.Insert(3, " hidden"));
        html.Append("</ul>");
        if (evidence.Count > 0)
        {
            var visible = events.Select(item => item.EventId).ToHashSet(StringComparer.Ordinal);
            html.Append("<h3>산출물·검증 근거</h3><ul class=\"evidence-links\">");
            if (evidence.Count > 100) html.Append($"<li>최근 100/{evidence.Count}건 표시 · 전체는 내보내기에서 확인</li>");
            foreach (var item in evidence.TakeLast(100))
            {
                var label = DashboardPanels.Encode(item.Reference);
                var node = DashboardPanels.Encode(plan.Nodes.FirstOrDefault(value => value.NodeId == item.NodeId)?.Title ?? item.NodeId);
                var href = $"#event-{Uri.EscapeDataString(item.EventId)}";
                html.Append($"<li>{node} · {(visible.Contains(item.EventId) ? $"<a href=\"{DashboardPanels.Encode(href)}\">{label}</a>" : label)}</li>");
            }
            html.Append("</ul>");
        }
        return html.Append("</section>").ToString();
    }

}
