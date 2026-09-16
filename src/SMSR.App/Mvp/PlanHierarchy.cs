namespace SMSR.App.Mvp;

internal static class PlanHierarchy
{
    public static IReadOnlyList<PlanNodeDefinition> Flatten(IReadOnlyList<PlanNodeDefinition> nodes)
    {
        var result = new List<PlanNodeDefinition>();
        foreach (var node in nodes) Add(node, node.ParentNodeId, result);
        return result;
    }

    private static void Add(PlanNodeDefinition node, string? parentId, List<PlanNodeDefinition> result)
    {
        result.Add(node with { ParentNodeId = parentId, Children = null });
        foreach (var child in node.Children ?? []) Add(child, node.NodeId, result);
    }
}
