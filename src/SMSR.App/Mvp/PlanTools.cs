using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace SMSR.App.Mvp;

[McpServerToolType]
public sealed class PlanTools(EventStore events, WorkflowEventNotifier notifier)
{
    [McpServerTool(Name = "save_plan"), Description("계층형 계획, 의존성, 담당 에이전트 역할과 완료 조건을 저장합니다. parentNodeId로 드릴다운 계층을 만듭니다.")]
    public async Task<string> SavePlan(string projectId, string workflowId, IReadOnlyList<PlanNodeDefinition> nodes)
    {
        if (PlanValidation.Validate(projectId, workflowId, nodes) is { } error) return JsonSerializer.Serialize(new { error });
        await events.SavePlanAsync(projectId, workflowId, nodes);
        notifier.Publish(projectId, workflowId);
        return JsonSerializer.Serialize(new { projectId, workflowId, nodeCount = nodes.Count });
    }

    [McpServerTool(Name = "get_plan"), Description("계획 노드와 최신 적용 상태를 조회합니다.")]
    public async Task<string> GetPlan(string projectId, string workflowId)
        => JsonSerializer.Serialize(await events.GetPlanAsync(projectId, workflowId));

    [McpServerTool(Name = "list_workflows"), Description("프로젝트의 기존 그래프를 최근 활동 순서로 조회합니다. 이전 그래프를 선택한 뒤 get_plan과 get_state로 불러오세요.")]
    public async Task<string> ListWorkflows(string projectId)
    {
        if (string.IsNullOrWhiteSpace(projectId) || projectId.Length > 128)
            return JsonSerializer.Serialize(new { error = "projectId가 올바르지 않습니다." });
        return JsonSerializer.Serialize(new
        {
            projectId,
            workflows = await events.GetWorkflowCatalogAsync(projectId)
        });
    }
}
