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
            var kind = agent.IsStale ? " stale" : agent.Status == "FAILED" ? " error" : agent.Status == "ACTIVE" ? " active" : "";
            var nodeTitle = plan.Nodes.FirstOrDefault(node => node.NodeId == agent.NodeId)?.Title ?? "배정된 작업 없음";
            var agentName = agent.AgentId == "root" ? "주 에이전트" : $"에이전트 {Short(agent.AgentId)}";
            html.Append($"<article class=\"agent{kind}\"><div class=\"agent-line\"><span class=\"agent-name\">{Encode(agentName)}</span><span class=\"badge\">{Encode(AgentStatus(agent))}</span></div><div class=\"agent-role\">{Encode(Role(agent.AgentRole))}</div><div class=\"agent-task\">{Encode(nodeTitle)}</div><div class=\"task muted\">ID {Encode(agent.AgentId)} · 노드 {Encode(agent.NodeId ?? "-")} · 재시도 {agent.RetryCount}회<br>{agent.LastHeartbeatAt.ToLocalTime():HH:mm:ss} 마지막 신호</div></article>");
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

    public static string RenderHistory(IReadOnlyList<RecentEvent> events, WorkflowPlan plan, string? nodeId = null)
        => DashboardHistoryCards.Render(nodeId is null ? events : events.Where(item => item.NodeId == nodeId).ToArray(), plan, nodeId is not null);

    public static string RenderActivities(IReadOnlyList<ActivityRecord> activities, WorkflowPlan plan, string? nodeId = null)
    {
        if (nodeId is not null) activities = activities.Where(item => item.NodeId == nodeId).ToArray();
        if (activities.Count == 0) return "<p class=\"empty\">활성 그래프의 에이전트 활동이 없습니다.</p>";
        var html = new StringBuilder("<ul class=\"history activity\">");
        var completedTools = activities.Where(item => item.Event == "TOOL_COMPLETED" && item.ToolUseId is not null)
            .Select(item => item.ToolUseId!).ToHashSet(StringComparer.Ordinal);
        foreach (var item in activities.Take(12))
        {
            var subject = plan.Nodes.FirstOrDefault(node => node.NodeId == item.NodeId)?.Title
                ?? item.NodeId ?? item.AgentId ?? item.SessionId;
            var category = ActivityCategory(item.Category);
            var detail = item.ToolName is null ? category : $"{category} · {item.ToolName}";
            var running = item.Event == "TOOL_STARTED" && item.ToolUseId is not null
                && !completedTools.Contains(item.ToolUseId);
            var live = running
                ? $" · <span class=\"running-time\" data-start=\"{item.TimestampUtc.ToUnixTimeMilliseconds()}\">진행 중</span>" : "";
            html.Append($"<li{(running ? " class=\"running\"" : "")}><b>{Encode(subject)}</b> {Encode(ActivityEvent(item.Event))}<br>{Encode(detail)} · {item.TimestampUtc.ToLocalTime():HH:mm:ss}{live}</li>");
        }
        return html.Append("</ul>").ToString();
    }

    public static string Encode(string value) => WebUtility.HtmlEncode(value);

    private static string AgentStatus(AgentState agent) => agent.IsStale ? "응답 지연" : agent.Status switch
    {
        "ACTIVE" => "작업 중", "IDLE" => "대기", "STOPPED" => "종료", "FAILED" => "실패", _ => agent.Status
    };

    private static string ActivityEvent(string value) => value switch
    {
        "TOOL_STARTED" => "작업 시작", "TOOL_COMPLETED" => "작업 완료",
        "AGENT_STARTED" => "에이전트 시작", "AGENT_STOPPED" => "에이전트 종료",
        "TURN_STARTED" => "요청 시작", "TURN_STOPPED" => "요청 종료", _ => value
    };

    private static string ActivityCategory(string value) => value switch
    {
        "COMMAND" => "명령 실행", "FILE_EDIT" => "파일 변경", "VALIDATION" => "검증",
        "SMSR" => "SMSR 기록", "TOOL" => "도구", "LIFECYCLE" => "상태", _ => value
    };

    private static string Role(string role) => role switch
    {
        "implementation-validation" => "구현 · 검증", "coordinator" => "조정", "implementer" or "implementation" => "구현",
        "validator" or "validation" or "tester" => "검증", "reviewer" or "review" => "검토", "release" => "배포",
        _ => role.Replace("-", " · ")
    };

    private static string Short(string value) => value.Length <= 18 ? value : $"{value[..8]}…{value[^4..]}";
}
