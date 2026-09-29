using System.IO.Compression;
using System.IO;
using System.Text.Json;
using System.Linq;

namespace SMSR.App.Mvp;

public sealed class WorkflowExportService(EventStore events, ActivityJsonlStore activity, string exportRoot, Func<string>? dashboardTheme = null)
{
    // ponytail: one archive job prevents user-triggered exports from competing for CPU and disk.
    private readonly SemaphoreSlim _exportGate = new(1, 1);

    public async Task<ExportResult> ExportAsync(string projectId, string workflowId, CancellationToken cancellationToken = default)
    {
        await _exportGate.WaitAsync(cancellationToken);
        try
        {
            var state = await events.GetStateAsync(projectId, workflowId, cancellationToken);
            var plan = await events.GetPlanAsync(projectId, workflowId, cancellationToken);
            var context = await events.GetWorkflowContextAsync(projectId, workflowId, cancellationToken);
            var recent = await events.GetRecentEventsAsync(projectId, workflowId, cancellationToken);
            var revisions = await events.GetPlanRevisionsAsync(projectId, workflowId, cancellationToken);
            var timeline = await events.GetTimelineEventsAsync(projectId, workflowId, 1000, cancellationToken);
            var evidence = await events.GetEvidenceAsync(projectId, workflowId, cancellationToken);
            var eventCount = await events.GetWorkflowEventCountAsync(projectId, workflowId, cancellationToken);
            var activities = activity.ReadLatest(projectId, workflowId, 100);
            var summary = await events.GetLatestSummaryAsync(projectId, workflowId, cancellationToken) ?? new WorkflowSummary(projectId, workflowId, "요약이 없습니다.", DateTimeOffset.UtcNow);
            var name = DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N")[..8];
            var directory = Path.Combine(exportRoot, name);
            Directory.CreateDirectory(directory);
            await File.WriteAllTextAsync(Path.Combine(directory, "dashboard.html"), DashboardPage.Render(state, plan, recent,
                dashboardTheme?.Invoke(), activities: activities, tokenUsage: activity.ReadTokenUsage(projectId, workflowId), context: context,
                revisions: revisions, timelineEvents: timeline, evidence: evidence, timelineEventCount: eventCount), cancellationToken);
            await File.WriteAllTextAsync(Path.Combine(directory, "workflow-state.json"), JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true }), cancellationToken);
            await File.WriteAllTextAsync(Path.Combine(directory, "workflow-context.json"), JsonSerializer.Serialize(context, new JsonSerializerOptions { WriteIndented = true }), cancellationToken);
            await File.WriteAllTextAsync(Path.Combine(directory, "plan-revisions.json"), JsonSerializer.Serialize(revisions, new JsonSerializerOptions { WriteIndented = true }), cancellationToken);
            await File.WriteAllTextAsync(Path.Combine(directory, "evidence-links.json"), JsonSerializer.Serialize(evidence, new JsonSerializerOptions { WriteIndented = true }), cancellationToken);
            await events.WriteEventsJsonLinesAsync(projectId, workflowId, Path.Combine(directory, "events.jsonl"), cancellationToken);
            activity.CopyTo(projectId, workflowId, Path.Combine(directory, "activity.jsonl"));
            await File.WriteAllTextAsync(Path.Combine(directory, "summary.md"), summary.Content, cancellationToken);
            var zipPath = Path.Combine(exportRoot, name + ".zip");
            await Task.Run(() => ZipFile.CreateFromDirectory(directory, zipPath), cancellationToken);
            return new(directory, zipPath);
        }
        finally { _exportGate.Release(); }
    }
}
