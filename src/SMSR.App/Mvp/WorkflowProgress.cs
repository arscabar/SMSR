namespace SMSR.App.Mvp;

internal static class WorkflowProgress
{
    public static int Value(string status, int? progressPercentage)
        => status == "SUCCESS" ? 100 : Math.Clamp(progressPercentage ?? 0, 0, 100);

    public static int Value(StateNode? node)
        => node is null ? 0 : Value(node.Status, node.ProgressPercentage);

    public static int Overall(WorkflowPlan plan, WorkflowState state)
    {
        var parentIds = plan.Nodes.Where(node => node.ParentNodeId is not null)
            .Select(node => node.ParentNodeId).ToHashSet();
        var leaves = plan.Nodes.Where(node => !parentIds.Contains(node.NodeId)).ToArray();
        var totalWeight = leaves.Sum(node => node.Weight);
        if (totalWeight == 0) return 0;
        var states = state.Nodes.ToDictionary(node => node.NodeId);
        return leaves.Sum(node =>
        {
            var stateNode = states.GetValueOrDefault(node.NodeId);
            if (DashboardHierarchy.DisplayStatus(node, plan.Nodes) == "PENDING"
                && (stateNode?.Status ?? node.Status) != "PENDING") return 0;
            return node.Weight * Value(stateNode?.Status ?? node.Status, stateNode?.ProgressPercentage);
        }) / totalWeight;
    }
}
