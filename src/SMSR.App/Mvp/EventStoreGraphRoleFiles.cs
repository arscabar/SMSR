using Microsoft.Data.Sqlite;

namespace SMSR.App.Mvp;

public sealed partial class EventStore
{
    internal async Task<IReadOnlyList<(GraphNode Node, bool HasRole)>> GraphRoleFilesAsync(string projectId,
        int revision, CancellationToken ct)
    {
        await using var connection = new SqliteConnection(_connectionString); await connection.OpenAsync(ct);
        using var command = GraphSql.Create(connection, null, """
            SELECT n.node_id,n.owner_path,n.kind,n.label,n.source_path,n.source_line,n.content_hash,n.details_json,
                EXISTS(SELECT 1 FROM graph_derived d WHERE d.project_id=n.project_id AND d.kind='node-role' AND d.item_key=n.node_id)
            FROM graph_revision_nodes n WHERE n.project_id=$p0 AND n.revision=$p1 AND n.kind='code'
                AND substr(n.node_id,1,5)='file:' ORDER BY n.source_path;
            """, projectId, revision);
        var files = new List<(GraphNode, bool)>(); await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct)) files.Add((ReadNode(reader), reader.GetBoolean(8)));
        return files;
    }
}
