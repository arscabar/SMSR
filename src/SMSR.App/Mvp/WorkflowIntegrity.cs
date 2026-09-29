namespace SMSR.App.Mvp;

internal static class WorkflowIntegrity
{
    public static IReadOnlyList<string> Find(WorkflowPlan plan)
    {
        var issues = new List<string>();
        var nodes = plan.Nodes.ToDictionary(node => node.NodeId);
        foreach (var node in plan.Nodes.Where(item => item.Status == "SUCCESS"))
        {
            var children = plan.Nodes.Where(item => item.ParentNodeId == node.NodeId
                && DashboardHierarchy.DisplayStatus(item, plan.Nodes) != "SUCCESS").Select(item => item.Title).ToArray();
            if (children.Length > 0)
                issues.Add($"{node.Title}: 완료로 기록됐지만 하위 작업 미완료 ({string.Join(", ", children)})");
            var dependencies = node.DependsOn.Where(id => !nodes.TryGetValue(id, out var dependency)
                || DashboardHierarchy.DisplayStatus(dependency, plan.Nodes) != "SUCCESS").ToArray();
            if (dependencies.Length > 0)
                issues.Add($"{node.Title}: 완료로 기록됐지만 선행 작업 미완료 ({string.Join(", ", dependencies)})");
        }
        return issues;
    }
}
