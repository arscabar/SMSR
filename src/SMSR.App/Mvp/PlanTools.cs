using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using SMSR.App.Services;

namespace SMSR.App.Mvp;

[McpServerToolType]
public sealed class PlanTools(EventStore events, WorkflowEventNotifier notifier, TokenUsageRecorder? tokens = null)
{
    [McpServerTool(Name = "save_plan"), Description("reason·approach는 작업 배경·방식, changeReason은 이번 계획 저장의 이유입니다. 계획 변경 이력은 버전별로 보존됩니다.")]
    public async Task<string> SavePlan(string projectId, IReadOnlyList<PlanNodeDefinition> nodes, string? workflowId = null,
        string? reason = null, string? approach = null, string? changeReason = null)
    {
        var flattened = PlanHierarchy.Flatten(nodes);
        var opaqueNewId = Guid.TryParse(workflowId, out _) && !await events.WorkflowExistsAsync(projectId, workflowId!);
        var generated = string.IsNullOrWhiteSpace(workflowId) || opaqueNewId;
        var title = flattened.FirstOrDefault(node => string.IsNullOrWhiteSpace(node.ParentNodeId))?.Title
            ?? flattened.FirstOrDefault()?.Title;
        var resolvedWorkflowId = generated ? WorkflowIdGenerator.Create(projectId, title) : workflowId!;
        if (PlanValidation.Validate(projectId, resolvedWorkflowId, flattened) is { } error) return JsonSerializer.Serialize(new { error });
        var context = new WorkflowContext(projectId, resolvedWorkflowId, reason, approach, null, DateTimeOffset.UtcNow);
        if ((!string.IsNullOrWhiteSpace(reason) || !string.IsNullOrWhiteSpace(approach))
            && EventValidation.Validate(context) is { } contextError) return JsonSerializer.Serialize(new { error = contextError });
        if (changeReason?.Length > 1000) return JsonSerializer.Serialize(new { error = "changeReason은 1,000자 이하여야 합니다." });
        var existing = await events.GetPlanAsync(projectId, resolvedWorkflowId);
        if (WorkflowPlanUpdate.Validate(existing, flattened) is { } updateError)
            return JsonSerializer.Serialize(new { error = updateError });
        await events.SavePlanAsync(projectId, resolvedWorkflowId, flattened,
            changeReason: string.IsNullOrWhiteSpace(changeReason) ? generated ? reason : null : changeReason);
        if (!string.IsNullOrWhiteSpace(reason) || !string.IsNullOrWhiteSpace(approach))
            await events.SaveWorkflowContextAsync(context);
        notifier.Publish(projectId, resolvedWorkflowId);
        var revision = (await events.GetPlanRevisionsAsync(projectId, resolvedWorkflowId)).Count;
        var response = JsonSerializer.Serialize(new { projectId, workflowId = resolvedWorkflowId, generated, replacedOpaqueId = opaqueNewId, nodeCount = flattened.Count, revision, contextSaved = !string.IsNullOrWhiteSpace(reason) || !string.IsNullOrWhiteSpace(approach) });
        var sessionId = flattened.Select(node => node.AssignedAgentId).FirstOrDefault(id => !string.IsNullOrWhiteSpace(id)) ?? resolvedWorkflowId;
        tokens?.Capture(projectId, resolvedWorkflowId, sessionId, flattened.FirstOrDefault()?.NodeId,
            JsonSerializer.Serialize(new { tool = "save_plan", projectId, workflowId, nodes = flattened }), response);
        return response;
    }

    [McpServerTool(Name = "get_plan"), Description("계획 노드와 최신 적용 상태를 조회합니다.")]
    public async Task<string> GetPlan(string projectId, string workflowId)
        => JsonSerializer.Serialize(await events.GetPlanAsync(projectId, workflowId));

    [McpServerTool(Name = "get_plan_revisions"), Description("계획 저장 시점의 버전·변경 이유·노드 스냅샷을 조회합니다.")]
    public async Task<string> GetPlanRevisions(string projectId, string workflowId)
        => JsonSerializer.Serialize(await events.GetPlanRevisionsAsync(projectId, workflowId));

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
