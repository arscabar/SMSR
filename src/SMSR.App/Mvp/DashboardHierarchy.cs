namespace SMSR.App.Mvp;

internal static class DashboardHierarchy
{
    private static readonly string[] Priority = ["BLOCKED", "FAILED", "RETRYING", "IN_PROGRESS", "VALIDATING"];

    public static string DisplayStatus(PlanNodeState node, IReadOnlyList<PlanNodeState> nodes)
    {
        var children = nodes.Where(item => item.ParentNodeId == node.NodeId).ToArray();
        if (children.Length == 0) return node.Status;
        var statuses = children.Select(child => DisplayStatus(child, nodes)).ToArray();
        if (statuses.All(status => status == "SUCCESS")) return "SUCCESS";
        foreach (var status in Priority)
            if (statuses.Contains(status)) return status;
        return node.Status;
    }
}
