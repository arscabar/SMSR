using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace SMSR.App.Mvp;

public sealed partial class EventStore
{
    internal async Task<GraphSemanticReport> SaveGraphSemanticAsync(string projectId, GraphSemanticReport report,
        GraphNode[] nodes, GraphEdge[] edges, GraphHyperedge[] groups, CancellationToken ct)
    {
        await _writeGate.WaitAsync(ct);
        try
        {
            await using var connection = new SqliteConnection(_connectionString); await connection.OpenAsync(ct);
            using (var keys = connection.CreateCommand()) { keys.CommandText = "PRAGMA foreign_keys=ON;"; await keys.ExecuteNonQueryAsync(ct); }
            await using var tx = (SqliteTransaction)await connection.BeginTransactionAsync(ct);
            var info = await ReadGraphInfoAsync(connection, tx, projectId, ct);
            if (info?.Revision != report.Revision) throw new InvalidOperationException("의미 분석 중 리비전이 변경되었습니다.");
            foreach (var p in report.Dependencies)
                await GraphKnowledgeWrites.ValidateEvidenceAsync(connection, tx, projectId, p.Key,
                    new(report.Model, report.ContractVersion, p.Value), ct);
            await GraphSemanticWrites.ReplaceAsync(connection, tx, projectId, report.Path, nodes, edges, ct);
            foreach (var group in groups)
                await GraphSql.ExecuteAsync(connection, tx, "INSERT INTO graph_hyperedges VALUES($p0,$p1,$p2,$p3);", ct,
                    projectId, group.HyperedgeId, group.OwnerPath, JsonSerializer.Serialize(group, GraphWorker.Json));
            var deep = await ReadDeepRowsAsync(connection, tx, projectId, info.Revision, ct);
            report = report with { Revision = info.Revision + 1 };
            await SaveDeepRevisionAsync(connection, tx, projectId, report.Revision, deep, ct);
            await GraphSql.ExecuteAsync(connection, tx, """
                INSERT INTO graph_deep_manifests
                SELECT project_id,$p2,kind,item_key,manifest_json FROM graph_deep_manifests
                WHERE project_id=$p0 AND revision=$p1;
                """, ct, projectId, info.Revision, report.Revision);
            await GraphSql.ExecuteAsync(connection, tx, """
                INSERT INTO graph_derived VALUES($p0,'document-semantic',$p1,$p2,$p3,$p4)
                ON CONFLICT(project_id,kind,item_key) DO UPDATE SET revision=$p2,payload=$p3,created_at_utc=$p4;
                """, ct, projectId, report.Path, report.Revision, JsonSerializer.Serialize(report, GraphWorker.Json), report.CapturedAt.ToString("O"));
            await tx.CommitAsync(ct); return report;
        }
        finally { _writeGate.Release(); }
    }
}
