using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace SMSR.App.Mvp;

[McpServerToolType]
public sealed class WorkflowTools(EventStore events, WorkflowEventNotifier notifier, WorkflowSummaryService summaries, WorkflowExportService exports)
{
    [McpServerTool(Name = "record_event"), Description("호출한 에이전트 자신의 노드 상태, 진행률, 재시도, 다음 작업과 산출물을 중복 없이 기록합니다.")]
    public async Task<string> RecordEvent(
        string eventId, string projectId, string workflowId, string nodeId, string agentId,
        string eventType, string status, string? summary = null, string? error = null,
        IReadOnlyList<string>? commands = null, IReadOnlyList<string>? artifacts = null,
        string? agentRole = null, int? progressPercentage = null, int retryCount = 0,
        string? nextAction = null)
    {
        var request = new RecordEventRequest(eventId, projectId, workflowId, nodeId, agentId, eventType, status, summary, error, commands, artifacts, agentRole, progressPercentage, retryCount, nextAction);
        var validationError = EventValidation.Validate(request);
        if (validationError is not null) return JsonSerializer.Serialize(new { error = validationError });
        var inserted = await events.RecordAsync(request);
        if (inserted) notifier.Publish(projectId, workflowId);
        return JsonSerializer.Serialize(new { eventId, duplicate = !inserted });
    }

    [McpServerTool(Name = "get_state"), Description("프로젝트 워크플로우의 최신 노드 상태를 조회합니다.")]
    public async Task<string> GetState(string projectId, string workflowId)
        => JsonSerializer.Serialize(await events.GetStateAsync(projectId, workflowId));

    [McpServerTool(Name = "generate_summary"), Description("현재 상태와 이벤트 기반의 로컬 요약을 생성해 저장합니다.")]
    public async Task<string> GenerateSummary(string projectId, string workflowId)
        => JsonSerializer.Serialize(await summaries.GenerateAsync(projectId, workflowId));

    [McpServerTool(Name = "save_summary"), Description("외부에서 생성한 요약을 저장합니다.")]
    public async Task<string> SaveSummary(string projectId, string workflowId, string content)
    {
        if (EventValidation.ValidateWorkflowIds(projectId, workflowId) is { } error) return JsonSerializer.Serialize(new { error });
        if (string.IsNullOrWhiteSpace(content) || content.Length > 10000) return JsonSerializer.Serialize(new { error = "content는 1~10,000자여야 합니다." });
        var summary = new WorkflowSummary(projectId, workflowId, content, DateTimeOffset.UtcNow);
        await events.SaveSummaryAsync(summary, null);
        return JsonSerializer.Serialize(summary);
    }

    [McpServerTool(Name = "export_workflow"), Description("워크플로우 기록을 HTML, Markdown, JSON, ZIP으로 내보냅니다.")]
    public async Task<string> ExportWorkflow(string projectId, string workflowId)
        => JsonSerializer.Serialize(await exports.ExportAsync(projectId, workflowId));
}
