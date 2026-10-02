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
        return html.Append("</section>").ToString();
    }

}
