namespace SMSR.App.Mvp;

internal static class WorkflowPlanUpdate
{
    private static readonly HashSet<string> Terminal = ["SUCCESS", "FAILED", "BLOCKED"];

    public static string? Validate(WorkflowPlan existing, IReadOnlyList<PlanNodeDefinition> updated)
    {
        if (existing.Nodes.Count == 0) return null;
        var updatedById = updated.ToDictionary(node => node.NodeId);
        var completed = existing.Nodes.Where(node =>
            DashboardHierarchy.DisplayStatus(node, existing.Nodes) == "SUCCESS").ToArray();
        if (completed.Any(node => !updatedById.TryGetValue(node.NodeId, out var next)
            || !Equivalent(node, next)))
            return "완료된 노드는 삭제하거나 변경할 수 없습니다. 후속 작업은 새 nodeId로 추가하세요.";
        var completedIds = completed.Select(node => node.NodeId).ToHashSet();
        var existingIds = existing.Nodes.Select(node => node.NodeId).ToHashSet();
        if (updated.Any(node => !existingIds.Contains(node.NodeId)
            && node.ParentNodeId is not null && completedIds.Contains(node.ParentNodeId)))
            return "완료된 노드 아래에는 작업을 추가할 수 없습니다. 새 노드를 형제 작업으로 추가하고 dependsOn으로 연결하세요.";
        if (!existing.Nodes.All(node => Terminal.Contains(
            DashboardHierarchy.DisplayStatus(node, existing.Nodes)))) return null;
        return Equivalent(existing.Nodes, updated) ? null
            : "완료된 그래프에는 새 작업을 추가할 수 없습니다. 새 그래프는 workflowId를 생략해 생성하세요.";
    }

    private static bool Equivalent(IReadOnlyList<PlanNodeState> existing, IReadOnlyList<PlanNodeDefinition> updated)
    {
        if (existing.Count != updated.Count) return false;
        return existing.Zip(updated).All(pair => pair.First.NodeId == pair.Second.NodeId
            && pair.First.Title == pair.Second.Title && pair.First.Weight == pair.Second.Weight
            && pair.First.DependsOn.SequenceEqual(pair.Second.DependsOn ?? [])
            && pair.First.ParentNodeId == pair.Second.ParentNodeId
            && pair.First.AssignedAgentId == pair.Second.AssignedAgentId
            && pair.First.AgentRole == pair.Second.AgentRole
            && pair.First.CompletionCriteria == pair.Second.CompletionCriteria);
    }

    private static bool Equivalent(PlanNodeState existing, PlanNodeDefinition updated)
        => existing.Title == updated.Title && existing.Weight == updated.Weight
            && existing.DependsOn.SequenceEqual(updated.DependsOn ?? [])
            && existing.ParentNodeId == updated.ParentNodeId
            && existing.AssignedAgentId == updated.AssignedAgentId
            && existing.AgentRole == updated.AgentRole
            && existing.CompletionCriteria == updated.CompletionCriteria;
}
