namespace SMSR.App.Mvp;

internal static class DashboardHierarchy
{
    private static readonly string[] Priority = ["BLOCKED", "FAILED", "RETRYING", "IN_PROGRESS", "VALIDATING"];

    public static string DisplayStatus(PlanNodeState node, IReadOnlyList<PlanNodeState> nodes)
        => DisplayStatus(node, nodes, new Dictionary<string, string>(), new HashSet<string>());

    private static string DisplayStatus(PlanNodeState node, IReadOnlyList<PlanNodeState> nodes,
        IDictionary<string, string> cache, ISet<string> visiting)
    {
        if (cache.TryGetValue(node.NodeId, out var cached)) return cached;
        if (!visiting.Add(node.NodeId)) return "PENDING";
        var children = nodes.Where(item => item.ParentNodeId == node.NodeId).ToArray();
        var status = node.Status;
        if (children.Length > 0)
        {
            var statuses = children.Select(child => DisplayStatus(child, nodes, cache, visiting)).ToArray();
            if (statuses.All(value => value == "SUCCESS")) status = "SUCCESS";
            else
            {
                status = "PENDING";
                foreach (var priority in Priority)
                    if (statuses.Contains(priority)) { status = priority; break; }
            }
        }
        if (status is "IN_PROGRESS" or "VALIDATING" or "RETRYING" or "SUCCESS")
        {
            var map = nodes.ToDictionary(item => item.NodeId);
            if (node.DependsOn.Any(id => !map.TryGetValue(id, out var dependency)
                || (visiting.Contains(id) || IsAncestor(id, node, map)
                    ? dependency.Status : DisplayStatus(dependency, nodes, cache, visiting)) != "SUCCESS"))
                status = "PENDING";
        }
        visiting.Remove(node.NodeId);
        cache[node.NodeId] = status;
        return status;
    }

    private static bool IsAncestor(string id, PlanNodeState node, IReadOnlyDictionary<string, PlanNodeState> map)
    {
        var parentId = node.ParentNodeId;
        while (parentId is not null && map.TryGetValue(parentId, out var parent))
        {
            if (parentId == id) return true;
            parentId = parent.ParentNodeId;
        }
        return false;
    }
}
