using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using SMSR.App.Services;

namespace SMSR.App.Mvp;

[McpServerToolType]
public sealed class PlanTools(EventStore events, WorkflowEventNotifier notifier, TokenUsageRecorder? tokens = null)
{
    [McpServerTool(Name = "save_plan"), Description("새 그래프에서는 workflowId를 생략하면 날짜시간·프로젝트·작업명으로 ID를 생성합니다. 미완료 작업이 독립적인 하위 흐름으로 확장되면 부모 노드의 children에 하위 작업을 넣으세요. 서버가 parentNodeId 기반의 클릭 가능한 드릴다운 계층으로 자동 저장합니다. 같은 작업의 계획 변경이나 완료 후 관련 후속 작업은 같은 workflowId로 호출해 입력 순서와 새 노드를 반영합니다.")]
    public async Task<string> SavePlan(string projectId, IReadOnlyList<PlanNodeDefinition> nodes, string? workflowId = null)
    {
        var flattened = PlanHierarchy.Flatten(nodes);
        var opaqueNewId = Guid.TryParse(workflowId, out _) && !await events.WorkflowExistsAsync(projectId, workflowId!);
        var generated = string.IsNullOrWhiteSpace(workflowId) || opaqueNewId;
        var title = flattened.FirstOrDefault(node => string.IsNullOrWhiteSpace(node.ParentNodeId))?.Title
            ?? flattened.FirstOrDefault()?.Title;
        var resolvedWorkflowId = generated ? WorkflowIdGenerator.Create(projectId, title) : workflowId!;
        if (PlanValidation.Validate(projectId, resolvedWorkflowId, flattened) is { } error) return JsonSerializer.Serialize(new { error });
        var existing = await events.GetPlanAsync(projectId, resolvedWorkflowId);
        if (WorkflowPlanUpdate.Validate(existing, flattened) is { } updateError)
            return JsonSerializer.Serialize(new { error = updateError });
        await events.SavePlanAsync(projectId, resolvedWorkflowId, flattened);
        notifier.Publish(projectId, resolvedWorkflowId);
        var response = JsonSerializer.Serialize(new { projectId, workflowId = resolvedWorkflowId, generated, replacedOpaqueId = opaqueNewId, nodeCount = flattened.Count });
        var sessionId = flattened.Select(node => node.AssignedAgentId).FirstOrDefault(id => !string.IsNullOrWhiteSpace(id)) ?? resolvedWorkflowId;
        tokens?.Capture(projectId, resolvedWorkflowId, sessionId, flattened.FirstOrDefault()?.NodeId,
            JsonSerializer.Serialize(new { tool = "save_plan", projectId, workflowId, nodes = flattened }), response);
        return response;
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
