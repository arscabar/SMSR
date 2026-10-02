using Microsoft.Data.Sqlite;

namespace SMSR.App.Mvp;

public sealed partial class EventStore
{
    internal async Task<IReadOnlyList<GraphNode>> GraphChildrenAsync(string projectId,
        GraphNode parent, int revision, int offset, int limit, CancellationToken ct)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(ct);
        using var command = GraphSql.Create(connection, null, """
            SELECT s.node_id,s.owner_path,s.kind,s.label,s.source_path,s.source_line,s.content_hash,s.details_json
            FROM graph_revision_nodes s WHERE s.project_id=$p0 AND s.revision=$p1
            AND s.kind='symbol' AND s.owner_path=$p2 AND
            (coalesce(json_extract(s.details_json,'$.ownerNodeId'),json_extract(s.details_json,'$.OwnerNodeId'))=$p3
             OR ($file=1 AND (coalesce(json_extract(s.details_json,'$.ownerNodeId'),json_extract(s.details_json,'$.OwnerNodeId')) IS NULL
                 OR NOT EXISTS (SELECT 1 FROM graph_revision_nodes p WHERE p.project_id=s.project_id
                     AND p.revision=s.revision AND p.owner_path=s.owner_path
                     AND p.node_id=coalesce(json_extract(s.details_json,'$.ownerNodeId'),json_extract(s.details_json,'$.OwnerNodeId'))))))
            ORDER BY s.source_line,s.node_id LIMIT $p4 OFFSET $p5;
            """, projectId, revision, parent.OwnerPath, parent.NodeId, limit, offset);
        command.Parameters.AddWithValue("$file", parent.NodeId.StartsWith("file:", StringComparison.Ordinal) ? 1 : 0);
        var nodes = new List<GraphNode>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct)) nodes.Add(ReadNode(reader));
        return nodes;
    }
}
