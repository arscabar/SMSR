namespace SMSR.App.Mvp;

internal static class WorkflowDependencyGate
{
    public static string? Validate(RecordEventRequest request, WorkflowPlan plan)
    {
        if (plan.Nodes.Count == 0)
            return "계획이 없습니다. save_plan으로 그래프를 먼저 만든 뒤 이벤트를 기록하세요.";
        var node = plan.Nodes.FirstOrDefault(item => item.NodeId == request.NodeId);
        if (node is null) return "계획에 없는 노드입니다. 같은 workflowId로 save_plan을 먼저 갱신하세요.";
        if (node.Status == "SUCCESS" && request.Status != "SUCCESS")
            return "이미 완료된 노드는 다시 진행 상태로 변경할 수 없습니다. 후속 작업을 새 nodeId로 계획에 추가하세요.";
        if (request.Status is not ("IN_PROGRESS" or "VALIDATING" or "RETRYING" or "SUCCESS")) return null;
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
