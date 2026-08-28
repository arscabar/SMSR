namespace SMSR.App.Mvp;

public sealed record PlanNodeDefinition(
    string NodeId,
    string Title,
    int Weight = 1,
    IReadOnlyList<string>? DependsOn = null,
    string? ParentNodeId = null,
    string? AssignedAgentId = null,
    string? AgentRole = null,
    string? CompletionCriteria = null);

public sealed record PlanNodeState(
    string NodeId, string Title, int Weight, IReadOnlyList<string> DependsOn,
    string Status, string? Summary, string? Error, DateTimeOffset? UpdatedAt,
    string? ParentNodeId = null, string? AssignedAgentId = null,
    string? AgentRole = null, string? CompletionCriteria = null);

public sealed record WorkflowPlan(string ProjectId, string WorkflowId, IReadOnlyList<PlanNodeState> Nodes);

public static class PlanValidation
{
    public static string? Validate(string projectId, string workflowId, IReadOnlyList<PlanNodeDefinition>? nodes)
    {
        if (EventValidation.ValidateWorkflowIds(projectId, workflowId) is { } error) return error;
        if (nodes is null || nodes.Count is < 1 or > 200) return "nodes는 1~200개여야 합니다.";
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var node in nodes)
        {
            if (string.IsNullOrWhiteSpace(node.NodeId) || node.NodeId.Length > 128 || string.IsNullOrWhiteSpace(node.Title) || node.Title.Length > 200 || node.Weight is < 1 or > 1000)
                return "노드 ID·제목·가중치가 올바르지 않습니다.";
            if (node.AssignedAgentId?.Length > 128 || node.AgentRole?.Length > 128 || node.CompletionCriteria?.Length > 2000)
                return "담당 에이전트·역할은 128자, 완료 조건은 2,000자 이하여야 합니다.";
            if (!ids.Add(node.NodeId)) return "nodeId는 중복될 수 없습니다.";
        }
        foreach (var node in nodes)
        {
            if (node.DependsOn?.Any(id => !ids.Contains(id) || id == node.NodeId) == true) return "dependsOn은 다른 계획 노드 ID여야 합니다.";
            if (node.ParentNodeId is not null && (!ids.Contains(node.ParentNodeId) || node.ParentNodeId == node.NodeId)) return "parentNodeId는 다른 계획 노드 ID여야 합니다.";
        }
        return HasCycle(nodes, node => node.DependsOn ?? []) || HasCycle(nodes, node => node.ParentNodeId is null ? [] : [node.ParentNodeId])
            ? "계획 노드 관계에 순환이 있습니다." : null;
    }

    private static bool HasCycle(IReadOnlyList<PlanNodeDefinition> nodes, Func<PlanNodeDefinition, IReadOnlyList<string>> edges)
    {
        var map = nodes.ToDictionary(node => node.NodeId, edges);
        var visiting = new HashSet<string>();
        var visited = new HashSet<string>();
        bool Visit(string id)
        {
            if (visited.Contains(id)) return false;
            if (!visiting.Add(id)) return true;
            var circular = map[id].Any(Visit);
            visiting.Remove(id);
            visited.Add(id);
            return circular;
        }
        return map.Keys.Any(Visit);
    }
}
