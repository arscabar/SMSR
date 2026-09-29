namespace SMSR.App.Mvp;

internal static partial class DashboardTimeline
{
    private static string Revision(PlanRevision item)
    {
        var nodes = string.Join("", item.Nodes.Select(node =>
        {
            var relations = string.Join(" · ", new[] { node.ParentNodeId is null ? null : $"상위 {node.ParentNodeId}",
                node.DependsOn is { Count: > 0 } ? $"선행 {string.Join(", ", node.DependsOn)}" : null }.Where(value => value is not null));
            return $"<li>{DashboardPanels.Encode(node.Title)} <small>{DashboardPanels.Encode(node.NodeId)}{(relations.Length == 0 ? "" : " · " + DashboardPanels.Encode(relations))}</small></li>";
        }));
        return $"<li class=\"timeline-revision\"><time>{item.CreatedAt.ToLocalTime():yyyy-MM-dd HH:mm:ss}</time><details><summary>계획 v{item.Revision} · {DashboardPanels.Encode(item.ChangeReason ?? "변경 이유 미기록")} · {item.Nodes.Count}개 노드</summary><ul>{nodes}</ul></details></li>";
    }

    private static string Event(WorkflowEvent item, WorkflowPlan plan)
    {
        var title = plan.Nodes.FirstOrDefault(node => node.NodeId == item.NodeId)?.Title ?? item.NodeId;
        var anchor = DashboardPanels.Encode("event-" + Uri.EscapeDataString(item.EventId));
        var summary = DashboardPanels.Encode(item.Error ?? item.Summary ?? "설명 미기록");
        var artifacts = item.Artifacts is { Count: > 0 }
            ? $"<p>산출물: {DashboardPanels.Encode(string.Join(" · ", item.Artifacts))}</p>" : "";
        return $"<li id=\"{anchor}\"><time>{item.CreatedAt.ToLocalTime():yyyy-MM-dd HH:mm:ss}</time><strong>{DashboardPanels.Encode(title)} · {StatusLabel(item.Status)}</strong><p>{summary}</p>{artifacts}</li>";
    }

    private static string StatusLabel(string status) => status switch
    {
        "SUCCESS" => "완료",
        "FAILED" => "실패",
        "BLOCKED" => "차단",
        "CANCELLED" => "중단",
        "IN_PROGRESS" => "진행",
        "VALIDATING" => "검증",
        "RETRYING" => "재시도",
        _ => "대기"
    };
}
