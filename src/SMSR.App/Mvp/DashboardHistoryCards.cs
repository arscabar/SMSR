using System.Text;

namespace SMSR.App.Mvp;

internal static class DashboardHistoryCards
{
    public static string Render(IReadOnlyList<RecentEvent> events, WorkflowPlan plan, bool allEvents = false)
    {
        if (events.Count == 0) return "<p class=\"empty\">최근 기록이 없습니다.</p>";
        var visible = allEvents ? events.OrderByDescending(item => item.CreatedAt).Take(50).ToArray()
            : events.GroupBy(item => item.NodeId, StringComparer.Ordinal)
                .Select(group => group.OrderByDescending(item => item.CreatedAt).First())
                .OrderByDescending(item => item.CreatedAt).Take(12).ToArray();
        var html = new StringBuilder("<div class=\"history-tools\"><button id=\"toggle-status-cards\" type=\"button\">전체 접기</button></div><div class=\"status-cards\">");
        foreach (var item in visible)
        {
            var status = StatusLabel(item.Status);
            var title = plan.Nodes.FirstOrDefault(node => node.NodeId == item.NodeId)?.Title ?? item.NodeId;
            var detail = item.Error ?? item.Summary ?? "기록된 설명이 없습니다.";
            var artifacts = item.Artifacts is { Count: > 0 }
                ? $"<div class=\"status-artifacts\"><span>산출물</span>{DashboardPanels.Encode(string.Join(" · ", item.Artifacts))}</div>" : "";
            html.Append($"<details class=\"status-card {StatusClass(item.Status)}\" data-record-id=\"{DashboardPanels.Encode(item.NodeId)}\" open><summary><span class=\"status-card-title\">{DashboardPanels.Encode(title)}</span><span class=\"status-badge\">{status}</span></summary><div class=\"status-card-body\"><time>{item.CreatedAt.ToLocalTime():yyyy-MM-dd HH:mm:ss} · {DashboardPanels.Encode(item.NodeId)}</time><p>{DashboardPanels.Encode(detail)}</p><div class=\"status-meta\">담당 {DashboardPanels.Encode(item.AgentId)} · 진척도 {item.ProgressPercentage ?? (item.Status == "SUCCESS" ? 100 : 0)}% · 재시도 {item.RetryCount}회</div>{artifacts}</div></details>");
        }
        return html.Append("</div>").ToString();
    }

    private static string StatusClass(string status) => status switch
    {
        "SUCCESS" => "success",
        "FAILED" or "BLOCKED" => "error",
        "IN_PROGRESS" or "VALIDATING" or "RETRYING" => "active",
        "CANCELLED" => "cancelled",
        _ => "pending"
    };

    private static string StatusLabel(string status) => status switch
    {
        "SUCCESS" => "완료",
        "FAILED" => "실패",
        "BLOCKED" => "차단",
        "CANCELLED" => "중단",
        "IN_PROGRESS" => "진행 상태",
        "VALIDATING" => "검증 상태",
        "RETRYING" => "재시도 상태",
        _ => "대기"
    };
}
