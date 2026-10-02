using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace SMSR.App.Mvp;

internal static class GraphSemanticWrites
{
    internal static async Task ClearAsync(SqliteConnection c, SqliteTransaction tx, string project, string path, CancellationToken ct)
    {
        await GraphSql.ExecuteAsync(c, tx, "DELETE FROM graph_hyperedges WHERE project_id=$p0 AND owner_path=$p1 AND hyperedge_id LIKE 'semantic:%';", ct, project, path);
        await GraphSql.ExecuteAsync(c, tx, "DELETE FROM graph_nodes WHERE project_id=$p0 AND owner_path=$p1 AND node_id LIKE 'semantic:%';", ct, project, path);
        await GraphSql.ExecuteAsync(c, tx, "DELETE FROM graph_edges WHERE project_id=$p0 AND owner_path=$p1 AND relation IN ('REQUIRES','JUSTIFIES','DESCRIBES','SEMANTIC_CANDIDATE');", ct, project, path);
    }
    internal static async Task ReplaceAsync(SqliteConnection c, SqliteTransaction tx, string project, string path,
        GraphNode[] nodes, GraphEdge[] edges, CancellationToken ct)
    {
        await ClearAsync(c, tx, project, path, ct);
        foreach (var n in nodes)
        {
            GraphKnowledgeValidation.Node(n);
            await GraphSql.ExecuteAsync(c, tx, "INSERT INTO graph_nodes VALUES($p0,$p1,$p2,$p3,$p4,$p5,$p6,$p7,$p8);",
                ct, project, n.NodeId, n.OwnerPath, n.Kind, n.Label, n.SourcePath, n.Line, n.Hash, JsonSerializer.Serialize(n.Details, GraphWorker.Json));
        }
        foreach (var e in edges)
            await GraphSql.ExecuteAsync(c, tx, "INSERT OR IGNORE INTO graph_edges VALUES($p0,$p1,$p2,$p3,$p4,$p5,$p6,$p7,$p8);",
                ct, project, e.SourceId, e.TargetId, e.Relation, e.OwnerPath, e.SourceLine, e.Resolution, e.Confidence,
                JsonSerializer.Serialize(e.Evidence! with { CapturedAt = DateTimeOffset.UtcNow }, GraphWorker.Json));
    }
}
