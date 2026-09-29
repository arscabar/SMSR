using System.ComponentModel;
using ModelContextProtocol.Server;

namespace SMSR.App.Mvp;

[McpServerToolType]
public sealed class StdioPlanTools(McpHttpGateway gateway)
{
    [McpServerTool(Name = "save_plan"), Description("reason·approach는 작업 배경·방식, changeReason은 이번 계획 변경 이유입니다.")]
    public Task<string> SavePlan(string projectId, IReadOnlyList<PlanNodeDefinition> nodes, string? workflowId = null,
        string? reason = null, string? approach = null, string? changeReason = null)
        => gateway.CallAsync("save_plan", new { projectId, nodes, workflowId, reason, approach, changeReason });

    [McpServerTool(Name = "get_plan"), Description("계획 노드와 최신 적용 상태를 조회합니다.")]
    public Task<string> GetPlan(string projectId, string workflowId)
        => gateway.CallAsync("get_plan", new { projectId, workflowId });

    [McpServerTool(Name = "get_plan_revisions"), Description("계획 버전과 변경 이유·노드 스냅샷을 조회합니다.")]
    public Task<string> GetPlanRevisions(string projectId, string workflowId)
        => gateway.CallAsync("get_plan_revisions", new { projectId, workflowId });

    [McpServerTool(Name = "list_workflows"), Description("프로젝트의 기존 그래프를 최근 활동 순서로 조회합니다. 이전 그래프를 선택한 뒤 get_plan과 get_state로 불러오세요.")]
    public Task<string> ListWorkflows(string projectId)
        => gateway.CallAsync("list_workflows", new { projectId });
}
