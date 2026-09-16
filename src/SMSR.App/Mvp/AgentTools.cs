using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace SMSR.App.Mvp;

[McpServerToolType]
public sealed class AgentTools(EventStore events, WorkflowEventNotifier notifier, OperatorInstructionQueue? instructions = null)
{
    [McpServerTool(Name = "record_heartbeat"), Description("호출한 에이전트 자신의 역할과 생존 상태를 SMSR로 전송합니다. 응답에 operatorInstruction이 있으면 사용자가 대시보드에서 선택한 지시이므로 즉시 반영합니다.")]
    public async Task<string> RecordHeartbeat(
        string projectId, string workflowId, string agentId, string agentRole,
        string status = "ACTIVE", string? nodeId = null, string? summary = null, int retryCount = 0)
    {
        var request = new AgentHeartbeatRequest(projectId, workflowId, agentId, agentRole, status, nodeId, summary, retryCount);
        if (EventValidation.Validate(request) is { } error) return JsonSerializer.Serialize(new { error });
        var plan = await events.GetPlanAsync(projectId, workflowId);
        if (plan.Nodes.Count == 0)
            return JsonSerializer.Serialize(new { error = "계획이 없습니다. save_plan으로 그래프를 먼저 만든 뒤 heartbeat를 기록하세요." });
        if (nodeId is not null && plan.Nodes.All(item => item.NodeId != nodeId))
            return JsonSerializer.Serialize(new { error = "계획에 없는 노드입니다. 같은 workflowId로 save_plan을 먼저 갱신하세요." });
        var state = await events.RecordHeartbeatAsync(request);
        notifier.Publish(projectId, workflowId);
        var instruction = status == "ACTIVE" && nodeId is not null
            ? instructions?.Take(projectId, workflowId, nodeId) : null;
        return instruction is null ? JsonSerializer.Serialize(state)
            : JsonSerializer.Serialize(new { state, operatorInstruction = instruction });
    }
}
