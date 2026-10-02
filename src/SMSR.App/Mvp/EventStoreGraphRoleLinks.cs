using Microsoft.Data.Sqlite;

namespace SMSR.App.Mvp;

public sealed partial class EventStore
{
    internal async Task<GraphEdge[]> GraphRoleEdgesAsync(string projectId, GraphNode node,
        int revision, CancellationToken ct)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(ct);
        using var command = GraphSql.Create(connection, null, """
            SELECT e.source_id,e.target_id,e.relation,e.owner_path,e.source_line,e.resolution,
              e.confidence,e.provenance_json,e.evidence_json FROM graph_effective_edges e
            JOIN graph_revision_nodes s ON s.project_id=e.project_id AND s.revision=e.revision AND s.node_id=e.source_id
            JOIN graph_revision_nodes t ON t.project_id=e.project_id AND t.revision=e.revision AND t.node_id=e.target_id
            WHERE e.project_id=$p0 AND e.revision=$p1 AND e.relation NOT IN ('CONTAINS','DEFINES')
              AND (e.relation NOT IN ('CALLS','INDIRECT_CALL') OR (e.resolution='RESOLVED' AND e.confidence='EXTRACTED'))
              AND (e.source_id=$p2 OR e.target_id=$p2 OR ($p3=1 AND (s.owner_path=$p4 OR t.owner_path=$p4)))
            ORDER BY CASE WHEN e.relation='CALLS' THEN 0 ELSE 1 END,e.source_id,e.target_id,e.relation,e.owner_path,e.source_line
            LIMIT 21;
            """, projectId, revision, node.NodeId, node.NodeId.StartsWith("file:") ? 1 : 0, node.OwnerPath);
        await using var reader = await command.ExecuteReaderAsync(ct);
        var edges = new List<GraphEdge>();
        while (await reader.ReadAsync(ct)) edges.Add(ReadEdge(reader));
        return edges.ToArray();
    }
}
