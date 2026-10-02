using Microsoft.Data.Sqlite;

namespace SMSR.App.Mvp;

public sealed partial class EventStore
{
    internal async Task<GraphNode[]> GraphRolePartialMembersAsync(string projectId, int revision,
        string type, string[] names, CancellationToken ct)
    {
        if (names.Length == 0) return [];
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(ct);
        var args = new object[] { projectId, revision, type }.Concat(names.Cast<object>()).ToArray();
        var placeholders = string.Join(',', Enumerable.Range(3, names.Length).Select(i => "$p" + i));
        using var command = GraphSql.Create(connection, null, $"""
            SELECT n.node_id,n.owner_path,n.kind,n.label,n.source_path,n.source_line,n.content_hash,n.details_json
            FROM graph_revision_nodes n JOIN graph_revision_nodes c ON c.project_id=n.project_id AND c.revision=n.revision
            AND c.node_id=coalesce(json_extract(n.details_json,'$.ownerNodeId'),json_extract(n.details_json,'$.OwnerNodeId'))
            WHERE n.project_id=$p0 AND n.revision=$p1 AND c.label=$p2
            AND coalesce(json_extract(c.details_json,'$.entityKind'),json_extract(c.details_json,'$.EntityKind')) IN ('class','struct')
            AND replace(ltrim(n.label,'.'),'()','') IN ({placeholders})
            ORDER BY n.source_path,n.source_line,n.node_id LIMIT 9;
            """, args);
        var nodes = new List<GraphNode>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct)) nodes.Add(ReadNode(reader));
        return nodes.ToArray();
    }
}
