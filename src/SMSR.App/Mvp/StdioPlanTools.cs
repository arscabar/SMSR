using System.ComponentModel;
using ModelContextProtocol.Server;

namespace SMSR.App.Mvp;

[McpServerToolType]
public sealed class StdioPlanTools(McpHttpGateway gateway)
{
    [McpServerTool(Name = "save_plan"), Description("구조화한 계획 노드를 저장합니다.")]
    public Task<string> SavePlan(string projectId, string workflowId, IReadOnlyList<PlanNodeDefinition> nodes)
        => gateway.CallAsync("save_plan", new { projectId, workflowId, nodes });

    [McpServerTool(Name = "get_plan"), Description("계획 노드와 상태를 조회합니다.")]
    public Task<string> GetPlan(string projectId, string workflowId) => gateway.CallAsync("get_plan", new { projectId, workflowId });

    [McpServerTool(Name = "record_lifecycle"), Description("Codex 세션 활동을 기록합니다.")]
    public Task<string> RecordLifecycle(string sessionId, string cwd, string eventName, string? turnId = null)
        => gateway.CallAsync("record_lifecycle", new { sessionId, cwd, eventName, turnId });
}
