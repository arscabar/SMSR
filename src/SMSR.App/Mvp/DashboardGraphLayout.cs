namespace SMSR.App.Mvp;

internal sealed record DashboardGraphLayout(
    int Width, int Height, IReadOnlyDictionary<string, (int X, int Y)> Positions)
{
    public const int NodeWidth = 230, NodeHeight = 78;
    private const int XGap = 34, YGap = 72, Margin = 36;

    public static DashboardGraphLayout Create(WorkflowPlan plan)
    {
        var map = plan.Nodes.ToDictionary(node => node.NodeId);
        var levels = new Dictionary<string, int>();
        int Level(PlanNodeState node)
        {
            if (levels.TryGetValue(node.NodeId, out var saved)) return saved;
            var dependencies = node.DependsOn.Where(map.ContainsKey).Select(id => Level(map[id]) + 1);
            return levels[node.NodeId] = dependencies.DefaultIfEmpty(0).Max();
        }
        foreach (var node in plan.Nodes) Level(node);
        var rows = plan.Nodes.GroupBy(node => levels[node.NodeId]).OrderBy(group => group.Key)
            .Select(group => group.ToArray()).ToArray();
        var largest = rows.Max(row => row.Length);
        var width = Margin * 2 + largest * NodeWidth + (largest - 1) * XGap;
        var height = Margin * 2 + rows.Length * NodeHeight + (rows.Length - 1) * YGap;
        var positions = new Dictionary<string, (int X, int Y)>();
        for (var rowIndex = 0; rowIndex < rows.Length; rowIndex++)
        {
            var rowWidth = rows[rowIndex].Length * NodeWidth + (rows[rowIndex].Length - 1) * XGap;
            var startX = (width - rowWidth) / 2;
            for (var index = 0; index < rows[rowIndex].Length; index++)
                positions[rows[rowIndex][index].NodeId] =
                    (startX + index * (NodeWidth + XGap), Margin + rowIndex * (NodeHeight + YGap));
        }
        return new(width, height, positions);
    }
}
