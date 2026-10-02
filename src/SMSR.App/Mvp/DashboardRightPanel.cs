namespace SMSR.App.Mvp;

internal static class DashboardRightPanel
{
    internal static string Render(WorkflowState state, WorkflowPlan plan, IReadOnlyList<RecentEvent> events,
        string? selectedNodeId, string? parentNodeId, IReadOnlyList<ActivityRecord> activities,
        WorkflowContext? context, IReadOnlyList<PlanRevision> revisions, IReadOnlyList<WorkflowEvent> timeline,
        IReadOnlyList<WorkflowEvidenceLink> evidence, long eventCount)
    {
        var selected = selectedNodeId is not null;
        var eventTotal = selected ? events.Count(e => e.NodeId == selectedNodeId) : events.Count;
        var activityTotal = selected ? activities.Count(a => a.NodeId == selectedNodeId) : activities.Count;
        return DashboardDisclosure.Render("context", "작업 이력", "배경 · 진행 방식 · 최종 결과",
            DashboardPanels.RenderWorkflowContext(context, state, plan), "▤", "요약", true)
            + DashboardDisclosure.Render("timeline", "계획·실행 순서", "기록을 선택해 당시 작업 확인",
                DashboardTimeline.Render(revisions, timeline, evidence, eventCount, plan), "↗", $"{revisions.Count + timeline.Count}건", true)
            + DashboardDisclosure.Render("detail", "작업 상세", "선택한 작업 · 담당 · 결과",
                DashboardPanels.RenderDetails(state, plan, selectedNodeId, parentNodeId), "◎", "상세", true)
            + DashboardDisclosure.Render("history", selected ? "선택 노드 상태 기록" : "상태 기록", "진행 · 완료 · 문제 내역",
                DashboardPanels.RenderHistory(events, plan, selectedNodeId), "≡", $"{eventTotal}건")
            + DashboardDisclosure.Render("activity", selected ? "선택 노드 활동" : "실시간 활동", "에이전트 · 도구 활동",
                DashboardPanels.RenderActivities(activities, plan, selectedNodeId), "◷", $"{activityTotal}건")
            + DashboardEvidence.Render(evidence, timeline, plan);
    }
}
