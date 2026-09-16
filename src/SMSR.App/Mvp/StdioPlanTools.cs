using System.ComponentModel;
using ModelContextProtocol.Server;

namespace SMSR.App.Mvp;

[McpServerToolType]
public sealed class StdioPlanTools(McpHttpGateway gateway)
{
    [McpServerTool(Name = "save_plan"), Description("미완료 작업의 children에 하위 작업을 넣으면 서버가 parentNodeId 기반 드릴다운 계층으로 자동 저장합니다. 새 그래프는 workflowId를 생략하면 읽기 쉬운 ID를 생성합니다.")]
    public Task<string> SavePlan(string projectId, IReadOnlyList<PlanNodeDefinition> nodes, string? workflowId = null)
        => gateway.CallAsync("save_plan", new { projectId, nodes, workflowId });

    [McpServerTool(Name = "get_plan"), Description("계획 노드와 최신 적용 상태를 조회합니다.")]
    public Task<string> GetPlan(string projectId, string workflowId)
        => gateway.CallAsync("get_plan", new { projectId, workflowId });

    [McpServerTool(Name = "list_workflows"), Description("프로젝트의 기존 그래프를 최근 활동 순서로 조회합니다. 이전 그래프를 선택한 뒤 get_plan과 get_state로 불러오세요.")]
    public Task<string> ListWorkflows(string projectId)
        => gateway.CallAsync("list_workflows", new { projectId });
}
