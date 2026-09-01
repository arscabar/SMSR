using System.Net;
using System.Text;

namespace SMSR.App.Mvp;

internal static class DashboardPanels
{
    private static readonly HashSet<string> Active = ["IN_PROGRESS", "VALIDATING", "RETRYING"];
    private static readonly HashSet<string> Error = ["FAILED", "BLOCKED", "RETRYING"];

    public static string RenderAgents(WorkflowState state, WorkflowPlan plan)
    {
        var agents = state.Agents ?? [];
        if (agents.Count == 0) return "<p class=\"empty\">기록된 heartbeat가 없습니다.</p>";
        var html = new StringBuilder();
        foreach (var agent in agents.OrderBy(item => item.IsStale ? 1 : item.Status == "ACTIVE" ? 0 : 2))
        {
            var kind = agent.IsStale || agent.Status == "FAILED" ? " error" : agent.Status == "ACTIVE" ? " active" : "";
            var status = agent.IsStale ? "STALE" : agent.Status;
            html.Append($"<article class=\"agent{kind}\"><div class=\"agent-line\"><span class=\"agent-name\">{Encode(agent.AgentId)}</span><span class=\"badge\">{Encode(status)}</span></div><div class=\"agent-role\">{Encode(agent.AgentRole)}</div><div class=\"task muted\">{Encode(agent.NodeId ?? "대기")} · 재시도 {agent.RetryCount}회<br>{agent.LastHeartbeatAt:HH:mm:ss} heartbeat</div></article>");
        }
        return html.ToString();
    }

    public static string RenderDetails(WorkflowState state, WorkflowPlan plan, string? selectedNodeId = null, string? parentNodeId = null)
    {
        var stateNode = selectedNodeId is null ? state.Nodes.Where(item => plan.Nodes.Any(planNode => planNode.NodeId == item.NodeId && planNode.ParentNodeId == parentNodeId)).OrderBy(item => Active.Contains(item.Status) ? 0 : 1).ThenByDescending(item => item.UpdatedAt).FirstOrDefault() : state.Nodes.FirstOrDefault(item => item.NodeId == selectedNodeId);
        var planNode = plan.Nodes.FirstOrDefault(item => item.NodeId == (selectedNodeId ?? stateNode?.NodeId));
        if (planNode is null && stateNode is null) return "<p class=\"empty\">노드를 선택하면 상세 정보가 표시됩니다.</p>";
        var nodeId = planNode?.NodeId ?? stateNode!.NodeId;
        var artifacts = stateNode?.Artifacts is { Count: > 0 } ? string.Join("\n", stateNode.Artifacts) : "-";
        return $"<dl class=\"detail\"><dt>작업</dt><dd>{Encode(nodeId)} · {Encode(planNode?.Title ?? nodeId)}</dd><dt>상태 / 진행률</dt><dd>{Encode(stateNode?.Status ?? "PENDING")} · {WorkflowProgress.Value(stateNode)}%</dd><dt>담당 / 역할</dt><dd>{Encode(stateNode?.AgentId ?? planNode?.AssignedAgentId ?? "-")} · {Encode(stateNode?.AgentRole ?? planNode?.AgentRole ?? "-")}</dd><dt>재시도</dt><dd>{stateNode?.RetryCount ?? 0}회</dd><dt>현재 작업</dt><dd>{Encode(stateNode?.Error ?? stateNode?.Summary ?? "-")}</dd><dt>다음 작업</dt><dd>{Encode(stateNode?.NextAction ?? "-")}</dd><dt>완료 조건</dt><dd>{Encode(planNode?.CompletionCriteria ?? "-")}</dd><dt>산출물</dt><dd>{Encode(artifacts)}</dd><dt>갱신</dt><dd>{stateNode?.UpdatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss") ?? "-"}</dd></dl>";
    }

    public static string RenderHistory(IReadOnlyList<RecentEvent> events)
    {
        if (events.Count == 0) return "<p class=\"empty\">최근 기록이 없습니다.</p>";
        var html = new StringBuilder("<ul class=\"history\">");
        foreach (var item in events.Take(8))
            html.Append($"<li><b>{Encode(item.NodeId)}</b> {Encode(item.Status)}<br>{Encode(item.Error ?? item.Summary ?? "-")}</li>");
        return html.Append("</ul>").ToString();
    }

    public static string RenderActivities(IReadOnlyList<ActivityRecord> activities)
    {
        if (activities.Count == 0) return "<p class=\"empty\">활성 그래프의 에이전트 활동이 없습니다.</p>";
        var html = new StringBuilder("<ul class=\"history activity\">");
        foreach (var item in activities.Take(12))
        {
            var subject = item.NodeId ?? item.AgentId ?? item.SessionId;
            var detail = item.ToolName is null ? item.Category : $"{item.Category} · {item.ToolName}";
            html.Append($"<li><b>{Encode(subject)}</b> {Encode(item.Event)}<br>{Encode(detail)} · {item.TimestampUtc.ToLocalTime():HH:mm:ss}</li>");
        }
        return html.Append("</ul>").ToString();
    }

    public static string Encode(string value) => WebUtility.HtmlEncode(value);
}
