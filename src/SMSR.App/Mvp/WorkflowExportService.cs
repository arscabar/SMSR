using System.IO.Compression;
using System.IO;
using System.Text.Json;
using System.Linq;

namespace SMSR.App.Mvp;

public sealed class WorkflowExportService(EventStore events, string exportRoot)
{
    // ponytail: one archive job prevents user-triggered exports from competing for CPU and disk.
    private readonly SemaphoreSlim _exportGate = new(1, 1);

    public async Task<ExportResult> ExportAsync(string projectId, string workflowId, CancellationToken cancellationToken = default)
    {
        await _exportGate.WaitAsync(cancellationToken);
        try
        {
            var state = await events.GetStateAsync(projectId, workflowId, cancellationToken);
            var recent = await events.GetRecentEventsAsync(projectId, workflowId, cancellationToken);
            var summary = await events.GetLatestSummaryAsync(projectId, workflowId, cancellationToken) ?? new WorkflowSummary(projectId, workflowId, "요약이 없습니다.", DateTimeOffset.UtcNow);
            var name = DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N")[..8];
            var directory = Path.Combine(exportRoot, name);
            Directory.CreateDirectory(directory);
            await File.WriteAllTextAsync(Path.Combine(directory, "dashboard.html"), DashboardPage.Render(state, recent), cancellationToken);
            await File.WriteAllTextAsync(Path.Combine(directory, "workflow-state.json"), JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true }), cancellationToken);
            await events.WriteEventsJsonLinesAsync(projectId, workflowId, Path.Combine(directory, "events.jsonl"), cancellationToken);
            await File.WriteAllTextAsync(Path.Combine(directory, "summary.md"), summary.Content, cancellationToken);
            var zipPath = Path.Combine(exportRoot, name + ".zip");
            await Task.Run(() => ZipFile.CreateFromDirectory(directory, zipPath), cancellationToken);
            return new(directory, zipPath);
        }
        finally { _exportGate.Release(); }
    }
}
