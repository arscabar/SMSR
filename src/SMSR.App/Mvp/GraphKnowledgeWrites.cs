using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace SMSR.App.Mvp;

internal static class GraphKnowledgeWrites
{
    internal static async Task ValidateEvidenceAsync(SqliteConnection connection, SqliteTransaction transaction,
        string projectId, string path, GraphSourceEvidence? evidence, CancellationToken ct)
    {
        GraphKnowledgeValidation.Evidence(evidence);
        if (evidence is null) return;
        using var hash = GraphSql.Create(connection, transaction,
            "SELECT content_hash FROM graph_files WHERE project_id=$p0 AND path=$p1;", projectId, path);
        if (!string.Equals(await hash.ExecuteScalarAsync(ct) as string, evidence.SourceHash, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("관계 근거의 원문 지문이 현재 파일과 다릅니다.");
    }

    internal static async Task ApplyAsync(SqliteConnection connection, SqliteTransaction transaction,
        string projectId, GraphScan scan, CancellationToken ct)
    {
        foreach (var path in scan.ChangedPaths.Concat(scan.ReparsedDocs).Concat(scan.RemovedPaths).Distinct())
            await GraphSql.ExecuteAsync(connection, transaction,
                "DELETE FROM graph_hyperedges WHERE project_id=$p0 AND owner_path=$p1 AND (hyperedge_id NOT LIKE 'semantic:%' OR $p2);", ct, projectId, path, scan.ChangedPaths.Contains(path) || scan.RemovedPaths.Contains(path));
        foreach (var item in scan.Hyperedges ?? [])
        {
            GraphHyperedgeValidation.Validate(item);
            await ValidateEvidenceAsync(connection, transaction, projectId, item.OwnerPath, item.Evidence, ct);
            await GraphSql.ExecuteAsync(connection, transaction, """
                INSERT INTO graph_hyperedges VALUES ($p0,$p1,$p2,$p3)
                ON CONFLICT(project_id,hyperedge_id) DO UPDATE SET owner_path=$p2,payload_json=$p3;
                """, ct, projectId, item.HyperedgeId, item.OwnerPath, JsonSerializer.Serialize(item with {
                    Evidence = item.Evidence with { CapturedAt = item.Evidence.CapturedAt ?? DateTimeOffset.UtcNow }
                }, GraphWorker.Json));
        }
        await GraphHyperedgeIntegrity.CheckAsync(connection, transaction, projectId, scan, ct);
    }
}
