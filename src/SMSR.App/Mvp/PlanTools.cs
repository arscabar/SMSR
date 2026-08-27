using System.ComponentModel;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace SMSR.App.Mvp;

[McpServerToolType]
public sealed class PlanTools(EventStore events, WorkflowEventNotifier notifier)
{
    [McpServerTool(Name = "save_plan"), Description("구조화된 계획 노드와 의존성을 저장합니다. 기록 이벤트의 nodeId가 계획 노드에 적용됩니다.")]
    public async Task<string> SavePlan(string projectId, string workflowId, IReadOnlyList<PlanNodeDefinition> nodes)
    {
        if (PlanValidation.Validate(projectId, workflowId, nodes) is { } error) return JsonSerializer.Serialize(new { error });
        await events.SavePlanAsync(projectId, workflowId, nodes);
        notifier.Publish(projectId, workflowId);
        return JsonSerializer.Serialize(new { projectId, workflowId, nodeCount = nodes.Count });
    }

    [McpServerTool(Name = "get_plan"), Description("계획 노드와 최신 적용 상태를 조회합니다.")]
    public async Task<string> GetPlan(string projectId, string workflowId)
        => JsonSerializer.Serialize(await events.GetPlanAsync(projectId, workflowId));

    [McpServerTool(Name = "record_lifecycle"), Description("Codex 훅의 최소 세션 활동을 기록합니다. 계획 노드 상태에는 영향을 주지 않습니다.")]
    public async Task<string> RecordLifecycle(string sessionId, string cwd, string eventName, string? turnId = null)
    {
        var projectId = Path.GetFileName(cwd.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        if (string.IsNullOrWhiteSpace(projectId)) projectId = "workspace";
        var eventId = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{sessionId}:{turnId}:{eventName}")));
        var request = new RecordEventRequest(eventId, projectId, sessionId, "_codex_session", "codex-hook", "NODE_STATUS_CHANGED", "IN_PROGRESS", eventName, null, null, null);
        if (EventValidation.Validate(request) is { } error) return JsonSerializer.Serialize(new { error });
        var inserted = await events.RecordAsync(request);
        if (inserted) notifier.Publish(projectId, sessionId);
        return JsonSerializer.Serialize(new { eventId, duplicate = !inserted, projectId, workflowId = sessionId });
    }
}
