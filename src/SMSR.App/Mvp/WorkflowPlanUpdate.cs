namespace SMSR.App.Mvp;

internal static class WorkflowPlanUpdate
{
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
        var additions = updated.Where(node => !existingIds.Contains(node.NodeId)).ToArray();
        if (completed.Length == existing.Nodes.Count && additions.Length > 0
            && additions.Any(node => !Anchored(node, updatedById, completedIds, new HashSet<string>())))
            return "완료된 그래프의 관련 후속 작업은 새 형제 또는 최상위 노드로 추가하고 기존 완료 노드를 dependsOn으로 연결하세요. 별도 작업은 workflowId를 생략해 새 그래프로 생성하세요.";
        return null;
    }

    private static bool Anchored(PlanNodeDefinition node,
        IReadOnlyDictionary<string, PlanNodeDefinition> updated, ISet<string> completed, ISet<string> visiting)
    {
        if (!visiting.Add(node.NodeId)) return false;
        var links = (node.DependsOn ?? []).Concat(node.ParentNodeId is null ? [] : [node.ParentNodeId]);
        var result = links.Any(id => completed.Contains(id)
            || updated.TryGetValue(id, out var linked) && Anchored(linked, updated, completed, visiting));
        visiting.Remove(node.NodeId);
        return result;
    }

    private static bool Equivalent(PlanNodeState existing, PlanNodeDefinition updated)
        => existing.Title == updated.Title && existing.Weight == updated.Weight
            && existing.DependsOn.SequenceEqual(updated.DependsOn ?? [])
            && existing.ParentNodeId == updated.ParentNodeId
            && existing.AssignedAgentId == updated.AssignedAgentId
            && existing.AgentRole == updated.AgentRole
            && existing.CompletionCriteria == updated.CompletionCriteria;
}
