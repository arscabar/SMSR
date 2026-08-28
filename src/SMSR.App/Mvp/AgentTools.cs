using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace SMSR.App.Mvp;

[McpServerToolType]
public sealed class AgentTools(EventStore events, WorkflowEventNotifier notifier)
{
    [McpServerTool(Name = "record_heartbeat"), Description("호출한 에이전트 자신의 역할과 생존 상태를 SMSR로 전송합니다. SMSR은 에이전트를 호출하지 않습니다.")]
    public async Task<string> RecordHeartbeat(
        string projectId, string workflowId, string agentId, string agentRole,
        string status = "ACTIVE", string? nodeId = null, string? summary = null, int retryCount = 0)
    {
        var request = new AgentHeartbeatRequest(projectId, workflowId, agentId, agentRole, status, nodeId, summary, retryCount);
        if (EventValidation.Validate(request) is { } error) return JsonSerializer.Serialize(new { error });
        var state = await events.RecordHeartbeatAsync(request);
        notifier.Publish(projectId, workflowId);
        return JsonSerializer.Serialize(state);
    }
}
