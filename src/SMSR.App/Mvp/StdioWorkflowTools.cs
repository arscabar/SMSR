using System.ComponentModel;
using ModelContextProtocol.Server;

namespace SMSR.App.Mvp;

[McpServerToolType]
public sealed class StdioWorkflowTools(McpHttpGateway gateway)
{
    [McpServerTool(Name = "record_event"), Description("노드 시작과 상태·진행률·검증·재시도·다음 작업·산출물 변경 즉시 호출하여 진행을 실시간 기록합니다. 작업 종료 시 몰아서 보내지 마세요.")]
    public Task<string> RecordEvent(
        string eventId, string projectId, string workflowId, string nodeId, string agentId,
        string eventType, string status, string? summary = null, string? error = null,
        IReadOnlyList<string>? commands = null, IReadOnlyList<string>? artifacts = null,
        string? agentRole = null, int? progressPercentage = null, int retryCount = 0,
        string? nextAction = null)
        => gateway.CallAsync("record_event", new
        {
            eventId,
            projectId,
            workflowId,
            nodeId,
            agentId,
            eventType,
            status,
            summary,
            error,
            commands,
            artifacts,
            agentRole,
            progressPercentage,
            retryCount,
            nextAction
        });

    [McpServerTool(Name = "get_state"), Description("프로젝트 워크플로우의 최신 노드 상태를 조회합니다.")]
    public Task<string> GetState(string projectId, string workflowId)
        => gateway.CallAsync("get_state", new { projectId, workflowId });

    [McpServerTool(Name = "generate_summary"), Description("현재 상태와 이벤트 기반의 로컬 요약을 생성해 저장합니다.")]
    public Task<string> GenerateSummary(string projectId, string workflowId)
        => gateway.CallAsync("generate_summary", new { projectId, workflowId });

    [McpServerTool(Name = "save_summary"), Description("외부에서 생성한 요약을 저장합니다.")]
    public Task<string> SaveSummary(string projectId, string workflowId, string content)
        => gateway.CallAsync("save_summary", new { projectId, workflowId, content });

    [McpServerTool(Name = "export_workflow"), Description("워크플로우 기록을 HTML, Markdown, JSON, ZIP으로 내보냅니다.")]
    public Task<string> ExportWorkflow(string projectId, string workflowId)
        => gateway.CallAsync("export_workflow", new { projectId, workflowId });
}
