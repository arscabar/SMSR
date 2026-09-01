namespace SMSR.App.Mvp;

internal static class WorkflowDependencyGate
{
    public static string? Validate(RecordEventRequest request, WorkflowPlan plan)
    {
        if (request.Status is not ("IN_PROGRESS" or "VALIDATING" or "RETRYING" or "SUCCESS")) return null;
        var node = plan.Nodes.FirstOrDefault(item => item.NodeId == request.NodeId);
        if (node is null) return null;
        if (request.Status == "SUCCESS")
        {
            var incompleteChildren = plan.Nodes.Where(item => item.ParentNodeId == node.NodeId)
                .Where(item => DashboardHierarchy.DisplayStatus(item, plan.Nodes) != "SUCCESS")
                .Select(item => item.NodeId).ToArray();
            if (incompleteChildren.Length > 0)
                return $"하위 작업이 완료되지 않았습니다: {string.Join(", ", incompleteChildren)}. 모든 하위 노드를 SUCCESS(100%)로 완료하세요.";
        }
        if (node.DependsOn.Count == 0) return null;
        var map = plan.Nodes.ToDictionary(item => item.NodeId);
        var blocked = node.DependsOn.Where(id => !map.TryGetValue(id, out var dependency)
            || DashboardHierarchy.DisplayStatus(dependency, plan.Nodes) != "SUCCESS").ToArray();
        return blocked.Length == 0 ? null
            : $"선행 작업이 완료되지 않았습니다: {string.Join(", ", blocked)}. 선행 노드를 SUCCESS(100%)로 완료한 뒤 시작하세요.";
    }
}
