using Microsoft.Data.Sqlite;

namespace SMSR.App.Mvp;

public sealed partial class EventStore
{
    private static async Task<(GraphEdge Edge, string Provenance)[]> ReadDeepRowsAsync(SqliteConnection connection,
        SqliteTransaction transaction, string projectId, int revision, CancellationToken ct)
    {
        using var query = GraphSql.Create(connection, transaction, """
            SELECT source_id,target_id,relation,owner_path,source_line,resolution,confidence,provenance_json
            FROM graph_deep_edges WHERE project_id=$p0 AND revision=$p1
            ORDER BY source_id,target_id,owner_path,source_line;
            """, projectId, revision);
        var rows = new List<(GraphEdge, string)>();
        await using var reader = await query.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct)) rows.Add((new(reader.GetString(0), reader.GetString(1), reader.GetString(2),
            reader.GetString(3), reader.GetInt32(4), reader.GetString(5), reader.GetString(6)), reader.GetString(7)));
        return rows.ToArray();
    }

    private static async Task SaveDeepRevisionAsync(SqliteConnection connection, SqliteTransaction transaction,
        string projectId, int revision, (GraphEdge Edge, string Provenance)[] rows, CancellationToken ct)
    {
        await GraphSql.ExecuteAsync(connection, transaction, """
            INSERT INTO graph_revision_nodes SELECT project_id,$p1,node_id,owner_path,kind,label,source_path,source_line,content_hash,details_json
            FROM graph_nodes WHERE project_id=$p0;
            INSERT INTO graph_revision_edges SELECT project_id,$p1,source_id,target_id,relation,owner_path,source_line,resolution,confidence,evidence_json
            FROM graph_edges WHERE project_id=$p0;
            INSERT INTO graph_revision_hyperedges SELECT project_id,$p1,hyperedge_id,owner_path,payload_json
            FROM graph_hyperedges WHERE project_id=$p0;
            UPDATE graph_projects SET revision=$p1 WHERE project_id=$p0;
            """, ct, projectId, revision);
        foreach (var (edge, provenance) in rows)
            await GraphSql.ExecuteAsync(connection, transaction, """
                INSERT INTO graph_deep_edges VALUES ($p0,$p1,$p2,$p3,$p4,$p5,$p6,$p7,$p8,$p9);
                """, ct, projectId, revision, edge.SourceId, edge.TargetId, edge.Relation, edge.OwnerPath,
                edge.SourceLine, edge.Resolution, edge.Confidence, provenance);
    }
}
