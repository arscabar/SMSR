using System.ComponentModel;
using ModelContextProtocol.Server;

namespace SMSR.App.Mvp;

[McpServerToolType]
public sealed class StdioAgentTools(McpHttpGateway gateway)
{
    [McpServerTool(Name = "record_heartbeat"), Description("호출한 에이전트 자신의 역할과 생존 상태를 SMSR로 전송합니다. 응답에 operatorInstruction이 있으면 사용자가 선택한 지시이므로 즉시 반영합니다.")]
    public Task<string> RecordHeartbeat(
        string projectId, string workflowId, string agentId, string agentRole,
        string status = "ACTIVE", string? nodeId = null, string? summary = null, int retryCount = 0)
        => gateway.CallAsync("record_heartbeat", new
        {
            projectId,
            workflowId,
            agentId,
            agentRole,
            status,
            nodeId,
            summary,
            retryCount
        });
}
