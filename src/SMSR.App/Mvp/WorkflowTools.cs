using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using SMSR.App.Services;

namespace SMSR.App.Mvp;

[McpServerToolType]
public sealed class WorkflowTools(EventStore events, WorkflowEventNotifier notifier, WorkflowSummaryService summaries,
    WorkflowExportService exports, OperatorInstructionQueue? instructions = null, TokenUsageRecorder? tokens = null)
{
    [McpServerTool(Name = "record_event"), Description("노드 상태 변경 즉시 호출합니다. 응답에 operatorInstruction이 있으면 사용자가 대시보드에서 선택한 지시이므로 즉시 반영합니다. 최종 응답 전 남은 노드를 종결하세요.")]
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
        var plan = await events.GetPlanAsync(projectId, workflowId);
        if (WorkflowDependencyGate.Validate(request, plan) is { } dependencyError)
            return JsonSerializer.Serialize(new { error = dependencyError });
        var inserted = await events.RecordAsync(request);
        if (inserted) notifier.Publish(projectId, workflowId);
        if (status is not ("IN_PROGRESS" or "VALIDATING" or "RETRYING"))
            instructions?.Remove(projectId, workflowId, nodeId);
        var instruction = status is "IN_PROGRESS" or "VALIDATING" or "RETRYING"
            ? instructions?.Take(projectId, workflowId, nodeId) : null;
        var response = JsonSerializer.Serialize(new { eventId, duplicate = !inserted, operatorInstruction = instruction });
        tokens?.Capture(projectId, workflowId, agentId, nodeId,
            JsonSerializer.Serialize(new { tool = "record_event", request }), response);
        return response;
    }

    [McpServerTool(Name = "get_state"), Description("프로젝트 워크플로우의 최신 노드 상태를 조회합니다.")]
    public async Task<string> GetState(string projectId, string workflowId)
        => JsonSerializer.Serialize(await events.GetStateAsync(projectId, workflowId));

    [McpServerTool(Name = "get_workflow_context"), Description("그래프의 작업 배경·진행 방식·최종 결과를 조회합니다.")]
    public async Task<string> GetWorkflowContext(string projectId, string workflowId)
        => JsonSerializer.Serialize(await events.GetWorkflowContextAsync(projectId, workflowId));

    [McpServerTool(Name = "get_workflow_timeline"), Description("계획 버전, 시간순 실행 이벤트, 산출물 근거와 과거 상태 불일치를 조회합니다. 이벤트는 최근 1,000개까지 반환합니다.")]
    public async Task<string> GetWorkflowTimeline(string projectId, string workflowId)
    {
        if (EventValidation.ValidateWorkflowIds(projectId, workflowId) is { } error)
            return JsonSerializer.Serialize(new { error });
        var plan = await events.GetPlanAsync(projectId, workflowId);
        var revisions = await events.GetPlanRevisionsAsync(projectId, workflowId);
        var timeline = await events.GetTimelineEventsAsync(projectId, workflowId, 1000);
        var count = await events.GetWorkflowEventCountAsync(projectId, workflowId);
        return JsonSerializer.Serialize(new { revisions, events = timeline, eventCount = count,
            truncated = count > timeline.Count, evidence = await events.GetEvidenceAsync(projectId, workflowId),
            integrityIssues = WorkflowIntegrity.Find(plan) });
    }

    [McpServerTool(Name = "save_workflow_context"), Description("그래프의 작업 배경·진행 방식·최종 결과를 저장합니다. 지정하지 않은 항목은 기존 기록을 유지합니다.")]
    public async Task<string> SaveWorkflowContext(string projectId, string workflowId, string? reason = null,
        string? approach = null, string? result = null)
    {
        var context = new WorkflowContext(projectId, workflowId, reason, approach, result, DateTimeOffset.UtcNow);
        if (string.IsNullOrWhiteSpace(reason) && string.IsNullOrWhiteSpace(approach) && string.IsNullOrWhiteSpace(result))
            return JsonSerializer.Serialize(new { error = "reason, approach, result 중 하나가 필요합니다." });
        if (EventValidation.Validate(context) is { } error) return JsonSerializer.Serialize(new { error });
        if ((await events.GetPlanAsync(projectId, workflowId)).Nodes.Count == 0)
            return JsonSerializer.Serialize(new { error = "계획이 없습니다. save_plan으로 그래프를 먼저 만드세요." });
        await events.SaveWorkflowContextAsync(context);
        notifier.Publish(projectId, workflowId);
        return JsonSerializer.Serialize(await events.GetWorkflowContextAsync(projectId, workflowId));
    }

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
