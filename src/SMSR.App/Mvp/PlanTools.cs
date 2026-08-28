using System.ComponentModel;
using System.IO;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace SMSR.App.Mvp;

[McpServerToolType]
public sealed class PlanTools(EventStore events, WorkflowEventNotifier notifier)
{
    [McpServerTool(Name = "save_plan"), Description("계층형 계획, 의존성, 담당 에이전트 역할과 완료 조건을 저장합니다. parentNodeId로 드릴다운 계층을 만듭니다.")]
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
    public async Task<string> RecordLifecycle(string sessionId, string cwd, string eventName, string? turnId = null,
        string? agentId = null, string? agentRole = null, string? nodeId = null, int retryCount = 0)
    {
        var projectId = Path.GetFileName(cwd.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        if (string.IsNullOrWhiteSpace(projectId)) projectId = "workspace";
        var status = eventName.Contains("STOP", StringComparison.OrdinalIgnoreCase) ? "STOPPED" : "ACTIVE";
        var request = new AgentHeartbeatRequest(projectId, sessionId, agentId ?? "codex-main", agentRole ?? "coordinator", status, nodeId, eventName, retryCount);
        if (EventValidation.Validate(request) is { } error) return JsonSerializer.Serialize(new { error });
        var heartbeat = await events.RecordHeartbeatAsync(request);
        notifier.Publish(projectId, sessionId);
        return JsonSerializer.Serialize(new { projectId, workflowId = sessionId, turnId, heartbeat });
    }
}
