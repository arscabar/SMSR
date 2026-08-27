using System.Linq;

namespace SMSR.App.Mvp;

public sealed class WorkflowSummaryService(EventStore events)
{
    public async Task<WorkflowSummary> GenerateAsync(string projectId, string workflowId, CancellationToken cancellationToken = default)
    {
        var state = await events.GetStateAsync(projectId, workflowId, cancellationToken);
        var latest = await events.GetLatestEventAsync(projectId, workflowId, cancellationToken);
        var statuses = string.Join(", ", state.Nodes.GroupBy(node => node.Status).OrderBy(group => group.Key).Select(group => $"{group.Key} {group.Count()}"));
        var content = $"# {projectId} / {workflowId}\n\n현재 노드: {state.Nodes.Count}개 ({(statuses.Length == 0 ? "기록 없음" : statuses)})\n\n최근 변경: {(latest is null ? "없음" : $"{latest.NodeId} · {latest.Status} · {latest.Summary ?? latest.Error ?? "내용 없음"}")}";
        var summary = new WorkflowSummary(projectId, workflowId, content, DateTimeOffset.UtcNow);
        await events.SaveSummaryAsync(summary, latest?.EventId, cancellationToken);
        return summary;
    }
}
