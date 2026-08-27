using System.IO.Compression;
using System.IO;
using System.Text.Json;
using System.Linq;

namespace SMSR.App.Mvp;

public sealed class WorkflowExportService(EventStore events, string exportRoot)
{
    public async Task<ExportResult> ExportAsync(string projectId, string workflowId, CancellationToken cancellationToken = default)
    {
        var state = await events.GetStateAsync(projectId, workflowId, cancellationToken);
        var records = await events.GetEventsAsync(projectId, workflowId, cancellationToken);
        var summary = await events.GetLatestSummaryAsync(projectId, workflowId, cancellationToken) ?? new WorkflowSummary(projectId, workflowId, "요약이 없습니다.", DateTimeOffset.UtcNow);
        var name = DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N")[..8];
        var directory = Path.Combine(exportRoot, name);
        Directory.CreateDirectory(directory);
        var recent = records.TakeLast(10).Select(item => new RecentEvent(item.NodeId, item.AgentId, item.Status, item.Summary, item.Error, item.CreatedAt)).ToArray();
        await File.WriteAllTextAsync(Path.Combine(directory, "dashboard.html"), DashboardPage.Render(state, recent), cancellationToken);
        await File.WriteAllTextAsync(Path.Combine(directory, "workflow-state.json"), JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true }), cancellationToken);
        await File.WriteAllLinesAsync(Path.Combine(directory, "events.jsonl"), records.Select(record => JsonSerializer.Serialize(record)), cancellationToken);
        await File.WriteAllTextAsync(Path.Combine(directory, "summary.md"), summary.Content, cancellationToken);
        var zipPath = Path.Combine(exportRoot, name + ".zip");
        ZipFile.CreateFromDirectory(directory, zipPath);
        return new(directory, zipPath);
    }
}
