namespace SMSR.App.Mvp;

internal static class DashboardCurrentNode
{
    private static readonly HashSet<string> Active = ["IN_PROGRESS", "VALIDATING", "RETRYING"];

    public static string? Visible(WorkflowPlan plan, WorkflowState state, IReadOnlySet<string> visibleIds)
    {
        var map = plan.Nodes.ToDictionary(node => node.NodeId);
        bool IsActive(string id) => map.TryGetValue(id, out var node)
            && Active.Contains(DashboardHierarchy.DisplayStatus(node, plan.Nodes));
        var nodeCandidates = state.Nodes.Where(node => Active.Contains(node.Status) && IsActive(node.NodeId))
            .Select(node => new { node.NodeId, Timestamp = node.UpdatedAt });
        var agentCandidates = (state.Agents ?? []).Where(agent => !agent.IsStale && agent.Status == "ACTIVE"
                && agent.NodeId is not null && IsActive(agent.NodeId))
            .Select(agent => new { NodeId = agent.NodeId!, Timestamp = agent.LastHeartbeatAt });
        var currentId = nodeCandidates.Concat(agentCandidates).OrderByDescending(item => item.Timestamp)
            .Select(item => item.NodeId).FirstOrDefault();
        for (var id = currentId; id is not null && map.TryGetValue(id, out var node); id = node.ParentNodeId)
            if (visibleIds.Contains(id)) return id;
        return null;
    }
}
