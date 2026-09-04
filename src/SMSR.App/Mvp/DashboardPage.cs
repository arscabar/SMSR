namespace SMSR.App.Mvp;

public static class DashboardPage
{
    public static string Render(WorkflowState state, IReadOnlyList<RecentEvent> events)
        => Render(state, new WorkflowPlan(state.ProjectId, state.WorkflowId, []), events);

    public static string Render(WorkflowState state, WorkflowPlan plan, IReadOnlyList<RecentEvent> events,
        string? theme = null, string? parentNodeId = null, string? selectedNodeId = null,
        IReadOnlyList<ActivityRecord>? activities = null)
    {
        var parentIds = plan.Nodes.Where(node => node.ParentNodeId is not null).Select(node => node.ParentNodeId).ToHashSet();
        var progressNodes = plan.Nodes.Where(node => !parentIds.Contains(node.NodeId)).ToArray();
        var totalWeight = progressNodes.Sum(node => node.Weight);
        var states = state.Nodes.ToDictionary(node => node.NodeId);
        var progress = totalWeight == 0 ? 0 : progressNodes.Sum(node =>
        {
            var stateNode = states.GetValueOrDefault(node.NodeId);
            if (DashboardHierarchy.DisplayStatus(node, plan.Nodes) == "PENDING"
                && (stateNode?.Status ?? node.Status) != "PENDING") return 0;
            return node.Weight * WorkflowProgress.Value(stateNode?.Status ?? node.Status, stateNode?.ProgressPercentage);
        }) / totalWeight;
        var completed = plan.Nodes.Count(node => DashboardHierarchy.DisplayStatus(node, plan.Nodes) == "SUCCESS");
        var blocked = state.Nodes.Where(node => node.Status == "BLOCKED").ToArray();
        var workflowTitle = plan.Nodes.FirstOrDefault(node => node.ParentNodeId is null)?.Title;
        var workflowLabel = string.IsNullOrWhiteSpace(workflowTitle)
            ? state.WorkflowId : $"{workflowTitle} · {state.WorkflowId}";
        var alert = blocked.Length == 0 ? "" :
            $"<div id=\"alert\">사용자 결정 필요: {DashboardPanels.Encode(string.Join(", ", blocked.Select(node => node.NodeId)))}</div>";

        return $$"""
            <!doctype html><html lang="ko"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
            <title>SMSR 작업 그래프</title><style>{{DashboardStyles.For(theme)}}</style></head><body>
            <header><div><h1>작업 그래프 대시보드</h1><span class="muted">{{DashboardPanels.Encode(state.ProjectId)}} / {{DashboardPanels.Encode(workflowLabel)}}</span></div>
            <div class="summary"><span class="chip">완료 {{completed}} / {{plan.Nodes.Count}}</span><span class="chip">전체 진행률 {{progress}}%</span></div></header>
            {{alert}}<main><aside id="agents"><h2>에이전트</h2>{{DashboardPanels.RenderAgents(state, plan)}}</aside>
            <section id="flow"><div class="flow-heading"><div><h2>계층형 작업 흐름</h2>{{DashboardNavigation.Breadcrumb(state.ProjectId, state.WorkflowId, plan, parentNodeId)}}</div></div><div id="graph">{{DashboardGraph.Render(plan, state, parentNodeId)}}</div></section>
            <aside id="details"><h2>작업 상세</h2>{{DashboardPanels.RenderDetails(state, plan, selectedNodeId, parentNodeId)}}<h2 class="history-title">실시간 활동</h2>{{DashboardPanels.RenderActivities(activities ?? [])}}<h2 class="history-title">상태 기록</h2>{{DashboardPanels.RenderHistory(events, plan)}}</aside></main>
            {{DashboardLiveUpdates.Render(state.ProjectId, state.WorkflowId)}}
            </body></html>
            """;
    }
}
