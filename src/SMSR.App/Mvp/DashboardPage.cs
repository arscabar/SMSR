namespace SMSR.App.Mvp;

public static class DashboardPage
{
    public static string Render(WorkflowState state, IReadOnlyList<RecentEvent> events)
        => Render(state, new WorkflowPlan(state.ProjectId, state.WorkflowId, []), events);

    public static string Render(WorkflowState state, WorkflowPlan plan, IReadOnlyList<RecentEvent> events,
        string? theme = null, string? parentNodeId = null, string? selectedNodeId = null,
        IReadOnlyList<ActivityRecord>? activities = null, TokenUsageSummary? tokenUsage = null)
    {
        var progress = WorkflowProgress.Overall(plan, state);
        var blocked = state.Nodes.Where(node => node.Status == "BLOCKED").ToArray();
        var displayStatuses = plan.Nodes.Select(node => DashboardHierarchy.DisplayStatus(node, plan.Nodes)).ToArray();
        var live = LiveLabel(displayStatuses);
        var workflowTitle = plan.Nodes.FirstOrDefault(node => node.ParentNodeId is null)?.Title;
        var workflowLabel = string.IsNullOrWhiteSpace(workflowTitle)
            ? state.WorkflowId : $"{workflowTitle} · {state.WorkflowId}";
        var alert = blocked.Length == 0 ? "" : $"<div id=\"alert\"><strong>확인 필요</strong> · {string.Join(" · ", blocked.Select(node =>
        {
            var title = plan.Nodes.FirstOrDefault(item => item.NodeId == node.NodeId)?.Title ?? node.NodeId;
            return $"<a href=\"{DashboardNavigation.Encode(DashboardNavigation.Url(state.ProjectId, state.WorkflowId, selectedNodeId: node.NodeId))}\">{DashboardPanels.Encode(title)}</a>";
        }))}</div>";

        return $$"""
            <!doctype html><html lang="ko"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
            <title>SMSR 작업 그래프</title><style>{{DashboardStyles.For(theme)}}</style></head><body>
            <header><div><h1>작업 그래프 대시보드</h1><span class="muted">{{DashboardPanels.Encode(state.ProjectId)}} / {{DashboardPanels.Encode(workflowLabel)}}</span></div>
            <div class="summary"><span id="live-connection" class="chip"{{(live.Static ? " data-static=\"true\"" : "")}}>{{live.Label}}</span>{{TokenChip("목표 작업", tokenUsage?.GoalInput ?? 0, tokenUsage?.GoalOutput ?? 0, tokenUsage?.HasGoalUsage == true, tokenUsage?.HasGoalBreakdown == true ? tokenUsage.GoalCachedInput : null)}}{{TokenChip("그래프(추정)", tokenUsage?.GraphInput ?? 0, tokenUsage?.GraphOutput ?? 0, tokenUsage?.HasGraphUsage == true)}}<span class="chip">전체 진행률 {{progress}}%</span></div></header>
            {{alert}}<main><aside id="agents"><h2>에이전트</h2>{{DashboardPanels.RenderAgents(state, plan)}}</aside>
            <section id="flow"><div class="flow-heading"><div><h2>계층형 작업 흐름</h2>{{DashboardNavigation.Breadcrumb(state.ProjectId, state.WorkflowId, plan, parentNodeId)}}</div></div><div id="graph">{{DashboardGraph.Render(plan, state, parentNodeId)}}</div></section>
            <aside id="details"><h2>작업 상세</h2>{{DashboardPanels.RenderDetails(state, plan, selectedNodeId, parentNodeId)}}<h2 class="history-title">{{(selectedNodeId is null ? "실시간 활동" : "선택 노드 활동")}}</h2>{{DashboardPanels.RenderActivities(activities ?? [], plan, selectedNodeId)}}<h2 class="history-title">{{(selectedNodeId is null ? "상태 기록" : "선택 노드 상태 기록")}}</h2>{{DashboardPanels.RenderHistory(events, plan, selectedNodeId)}}</aside></main>
            {{DashboardLiveUpdates.Render(state.ProjectId, state.WorkflowId)}}
            </body></html>
            """;
    }

    private static (string Label, bool Static) LiveLabel(IReadOnlyList<string> statuses)
    {
        if (statuses.Count > 0 && statuses.All(status => status == "SUCCESS")) return ("그래프 완료", true);
        if (statuses.Any(status => status is "IN_PROGRESS" or "VALIDATING" or "RETRYING"))
            return ("자동 갱신 연결 중", false);
        if (statuses.Any(status => status == "BLOCKED")) return ("사용자 확인 대기", true);
        if (statuses.Any(status => status == "FAILED")) return ("오류로 종료", true);
        if (statuses.Any(status => status == "CANCELLED")) return ("작업 중단", true);
        return ("작업 대기", true);
    }

    private static string TokenChip(string label, long input, long output, bool available, long? cachedInput = null)
        => available
            ? $"<span class=\"chip token-chip\" tabindex=\"0\" aria-label=\"{TokenLabel(input, output, cachedInput)}\">{label} 토큰 · IN {Compact(input)} · OUT {Compact(output)}<span class=\"token-tooltip\" role=\"tooltip\">{TokenTooltip(input, output, cachedInput)}</span></span>"
            : $"<span class=\"chip token-chip muted\">{label} 토큰 · 수집 대기</span>";

    private static string TokenTooltip(long input, long output, long? cachedInput)
        => cachedInput is { } cached
            ? $"<span><b>신규 입력</b><strong>{input - cached:N0}</strong></span><span><b>캐시 입력</b><strong>{cached:N0}</strong></span><span><b>출력</b><strong>{output:N0}</strong></span>"
            : $"<span><b>입력</b><strong>{input:N0}</strong></span><span><b>출력</b><strong>{output:N0}</strong></span>";

    private static string TokenLabel(long input, long output, long? cachedInput)
        => cachedInput is { } cached
            ? $"신규 입력 {input - cached:N0}, 캐시 입력 {cached:N0}, 출력 {output:N0}"
            : $"입력 {input:N0}, 출력 {output:N0}";

    private static string Compact(long value) => value switch
    {
        >= 1_000_000 => $"{value / 1_000_000d:0.0}M",
        >= 1_000 => $"{value / 1_000d:0.0}K",
        _ => value.ToString()
    };
}
