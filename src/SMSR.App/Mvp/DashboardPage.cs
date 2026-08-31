namespace SMSR.App.Mvp;

public static class DashboardPage
{
    public static string Render(WorkflowState state, IReadOnlyList<RecentEvent> events)
        => Render(state, new WorkflowPlan(state.ProjectId, state.WorkflowId, []), events);

    public static string Render(WorkflowState state, WorkflowPlan plan, IReadOnlyList<RecentEvent> events,
        string? theme = null, string? parentNodeId = null, string? selectedNodeId = null)
    {
        var parentIds = plan.Nodes.Where(node => node.ParentNodeId is not null).Select(node => node.ParentNodeId).ToHashSet();
        var progressNodes = plan.Nodes.Where(node => !parentIds.Contains(node.NodeId)).ToArray();
        var totalWeight = progressNodes.Sum(node => node.Weight);
        var completedWeight = progressNodes.Where(node => node.Status == "SUCCESS").Sum(node => node.Weight);
        var progress = totalWeight == 0 ? 0 : completedWeight * 100 / totalWeight;
        var completed = progressNodes.Count(node => node.Status == "SUCCESS");
        var blocked = state.Nodes.Where(node => node.Status == "BLOCKED").ToArray();
        var alert = blocked.Length == 0 ? "" :
            $"<div id=\"alert\">사용자 결정 필요: {DashboardPanels.Encode(string.Join(", ", blocked.Select(node => node.NodeId)))}</div>";

        return $$"""
            <!doctype html><html lang="ko"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
            <title>SMSR 작업 그래프</title><style>{{DashboardStyles.For(theme)}}</style></head><body>
            <header><div><h1>작업 그래프 대시보드</h1><span class="muted">{{DashboardPanels.Encode(state.ProjectId)}} / {{DashboardPanels.Encode(state.WorkflowId)}}</span></div>
            <div class="summary"><span class="chip">완료 {{completed}} / {{progressNodes.Length}}</span><span class="chip">전체 진행률 {{progress}}%</span></div></header>
            {{alert}}<main><aside id="agents"><h2>에이전트</h2>{{DashboardPanels.RenderAgents(state, plan)}}</aside>
            <section id="flow"><div class="flow-heading"><div><h2>계층형 작업 흐름</h2>{{DashboardNavigation.Breadcrumb(state.ProjectId, state.WorkflowId, plan, parentNodeId)}}</div></div><div id="graph">{{DashboardGraph.Render(plan, state, parentNodeId)}}</div></section>
            <aside id="details"><h2>작업 상세</h2>{{DashboardPanels.RenderDetails(state, plan, selectedNodeId, parentNodeId)}}<h2 class="history-title">최근 기록</h2>{{DashboardPanels.RenderHistory(events)}}</aside></main>
            {{DashboardLiveUpdates.Render(state.ProjectId, state.WorkflowId)}}
            </body></html>
            """;
    }
}
