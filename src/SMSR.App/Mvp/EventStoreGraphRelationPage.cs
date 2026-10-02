using Microsoft.Data.Sqlite;

namespace SMSR.App.Mvp;

public sealed partial class EventStore
{
    internal async Task<(GraphEdge[] Edges, int Total)> GraphRelationSliceAsync(string projectId,
        string nodeId, string direction, string? relation, int offset, int limit, int revision, CancellationToken ct)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(ct);
        const string where = "project_id=$p0 AND revision=$p1 AND (($p3<>'outgoing' AND target_id=$p2) OR ($p3<>'incoming' AND source_id=$p2)) AND ($p4 IS NULL OR relation=$p4)";
        using var count = GraphSql.Create(connection, null, "SELECT COUNT(*) FROM graph_effective_edges WHERE " + where,
            projectId, revision, nodeId, direction, relation);
        var total = Convert.ToInt32(await count.ExecuteScalarAsync(ct));
        using var query = GraphSql.Create(connection, null, $"""
            SELECT source_id,target_id,relation,owner_path,source_line,resolution,confidence,provenance_json,evidence_json
            FROM graph_effective_edges WHERE {where}
            ORDER BY source_id,target_id,relation,owner_path,source_line,resolution,confidence LIMIT $p5 OFFSET $p6;
            """, projectId, revision, nodeId, direction, relation, limit, offset);
        await using var reader = await query.ExecuteReaderAsync(ct);
        var edges = new List<GraphEdge>();
        while (await reader.ReadAsync(ct)) edges.Add(ReadEdge(reader));
        return (edges.ToArray(), total);
    }
}
