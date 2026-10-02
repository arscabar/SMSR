using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace SMSR.App.Mvp;

internal static class GraphSemanticInvalidation
{
    internal static async Task ApplyAsync(SqliteConnection c, SqliteTransaction tx, string project, GraphScan scan, CancellationToken ct)
    {
        using var query = GraphSql.Create(c, tx, "SELECT payload FROM graph_derived WHERE project_id=$p0 AND kind='document-semantic';", project);
        var reports = new List<GraphSemanticReport>();
        await using (var reader = await query.ExecuteReaderAsync(ct))
            while (await reader.ReadAsync(ct)) reports.Add(JsonSerializer.Deserialize<GraphSemanticReport>(reader.GetString(0), GraphWorker.Json)!);
        var files = scan.Files.ToDictionary(f => f.Path, f => f.Hash, StringComparer.Ordinal);
        foreach (var report in reports)
            if (report.ContractVersion != "semantic-v1" || report.Dependencies.Any(p => !files.TryGetValue(p.Key, out var hash) || hash != p.Value))
                await GraphSemanticWrites.ClearAsync(c, tx, project, report.Path, ct);
        // Retain the safe report as STALE evidence, not as current graph facts or a successful new analysis.
    }
}
