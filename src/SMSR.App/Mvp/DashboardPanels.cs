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
            html.Append($"""
                <article class="agent{kind}">
                  <div class="agent-line"><span class="agent-name">{Encode(AgentName(agent.AgentId))}</span><span class="badge">{Encode(AgentStatus(agent))}</span></div>
                  <div class="agent-focus"><span>현재 담당</span><strong>{Encode(nodeTitle)}</strong></div>
                  <div class="agent-facts"><div><span>역할</span><strong>{Encode(Role(agent.AgentRole))}</strong></div><div><span>마지막 신호</span><strong>{agent.LastHeartbeatAt.ToLocalTime():HH:mm:ss}</strong></div></div>
                  <details class="tech-details"><summary>기술 정보</summary><code>에이전트 {Encode(agent.AgentId)}</code><code>노드 {Encode(agent.NodeId ?? "-")}</code><span>재시도 {agent.RetryCount}회</span></details>
                </article>
                """);
        }
        return html.ToString();
    }

    public static string RenderDetails(WorkflowState state, WorkflowPlan plan, string? selectedNodeId = null, string? parentNodeId = null)
    {
        var stateNode = selectedNodeId is null ? state.Nodes.Where(item => plan.Nodes.Any(planNode => planNode.NodeId == item.NodeId && planNode.ParentNodeId == parentNodeId)).OrderBy(item => Active.Contains(item.Status) ? 0 : 1).ThenByDescending(item => item.UpdatedAt).FirstOrDefault() : state.Nodes.FirstOrDefault(item => item.NodeId == selectedNodeId);
        var planNode = plan.Nodes.FirstOrDefault(item => item.NodeId == (selectedNodeId ?? stateNode?.NodeId));
        if (planNode is null && stateNode is null) return "<p class=\"empty\">노드를 선택하면 상세 정보가 표시됩니다.</p>";
        var nodeId = planNode?.NodeId ?? stateNode!.NodeId;
        var status = stateNode?.Status ?? planNode?.Status ?? "PENDING";
        var agentId = stateNode?.AgentId ?? planNode?.AssignedAgentId ?? "-";
        var role = stateNode?.AgentRole ?? planNode?.AgentRole ?? "-";
        var currentLabel = stateNode?.Error is not null ? "문제 내용" : status == "SUCCESS" ? "결과" : "현재 작업";
        return $"""
            <article class="detail-card">
              <div class="detail-head"><h3>{Encode(planNode?.Title ?? nodeId)}</h3><span class="detail-status {StatusClass(status)}">{StatusLabel(status)} · {WorkflowProgress.Value(stateNode)}%</span></div>
              <div class="detail-meta"><div><span>담당 · 역할</span><strong>{Encode(AgentName(agentId))} · {Encode(Role(role))}</strong></div><span>재시도 {(stateNode?.RetryCount ?? 0)}회</span></div>
              {DetailSection(currentLabel, stateNode?.Error ?? stateNode?.Summary, "primary")}
              {CriteriaSection(planNode?.CompletionCriteria)}
              {ArtifactSection(stateNode?.Artifacts)}
              <time class="detail-updated">마지막 갱신 {stateNode?.UpdatedAt.ToLocalTime().ToString("MM-dd HH:mm") ?? "-"}</time>
            </article>
            """;
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
                ? $" · <span class=\"running-time\" data-start=\"{item.TimestampUtc.ToUnixTimeMilliseconds()}\">실행 중</span>" : "";
            html.Append($"<li{(running ? " class=\"running\"" : "")}><b>{Encode(subject)}</b> {Encode(ActivityEvent(item.Event))}<br>{Encode(detail)} · {item.TimestampUtc.ToLocalTime():HH:mm:ss}{live}</li>");
        }
        return html.Append("</ul>").ToString();
    }

    public static string Encode(string value) => WebUtility.HtmlEncode(value);

    private static string DetailSection(string title, string? content, string kind = "")
        => string.IsNullOrWhiteSpace(content) ? "" : $"<section class=\"detail-section {kind}\"><h4>{Encode(title)}</h4><p>{Encode(content)}</p></section>";

    private static string CriteriaSection(string? content)
        => string.IsNullOrWhiteSpace(content) ? "" : $"<details class=\"detail-criteria\"><summary>완료 기준</summary><p>{Encode(content)}</p></details>";

    private static string ArtifactSection(IReadOnlyList<string>? artifacts)
        => artifacts is not { Count: > 0 } ? "" : $"<section class=\"detail-section\"><h4>산출물</h4><ul>{string.Join("", artifacts.Select(item => $"<li>{Encode(item)}</li>"))}</ul></section>";

    private static string AgentName(string value)
        => value == "root" ? "주 에이전트" : value == "-" ? "미지정" : $"에이전트 {Short(value)}";

    private static string StatusClass(string status) => status switch
    {
        "SUCCESS" => "success", "FAILED" or "BLOCKED" => "error",
        "IN_PROGRESS" or "VALIDATING" or "RETRYING" => "active", "CANCELLED" => "cancelled", _ => "pending"
    };

    private static string StatusLabel(string status) => status switch
    {
        "SUCCESS" => "완료", "FAILED" => "실패", "BLOCKED" => "확인 필요", "CANCELLED" => "중단",
        "IN_PROGRESS" => "진행 상태", "VALIDATING" => "검증 상태", "RETRYING" => "재시도 상태", _ => "대기"
    };

    private static string AgentStatus(AgentState agent) => agent.IsStale ? "응답 지연" : agent.Status switch
    {
        "ACTIVE" => "연결됨", "IDLE" => "대기", "STOPPED" => "종료", "FAILED" => "실패", _ => agent.Status
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
        "primary" => "주 작업", "worker" => "보조 작업", "analysis" => "분석", "design" => "설계",
        "implementation-validation" => "구현 · 검증", "coordinator" => "조정", "implementer" or "implementation" => "구현",
        "validator" or "validation" or "tester" => "검증", "reviewer" or "review" => "검토", "release" => "배포", "-" => "미지정",
        _ => role.Replace("-", " · ")
    };

    private static string Short(string value) => value.Length <= 18 ? value : $"{value[..8]}…{value[^4..]}";
}
