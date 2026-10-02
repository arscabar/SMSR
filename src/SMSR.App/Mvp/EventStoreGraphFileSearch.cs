using Microsoft.Data.Sqlite;

namespace SMSR.App.Mvp;

public sealed partial class EventStore
{
    internal async Task<IReadOnlyList<GraphNode>> SearchGraphFilesAsync(string projectId,
        string query, string? kind, int revision, int offset, int limit, CancellationToken ct)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(ct);
        using var command = GraphSql.Create(connection, null, """
            SELECT f.node_id,f.owner_path,f.kind,f.label,f.source_path,f.source_line,f.content_hash,f.details_json
            FROM graph_revision_nodes f WHERE f.project_id=$p0 AND f.revision=$p1
            AND substr(f.node_id,1,5)='file:' AND ($kind IS NULL OR f.kind=$kind)
            AND (f.label LIKE $q ESCAPE '\' OR f.source_path LIKE $q ESCAPE '\'
                OR EXISTS (SELECT 1 FROM graph_revision_nodes s
                    WHERE s.project_id=f.project_id AND s.revision=f.revision AND s.owner_path=f.owner_path
                    AND s.kind='symbol' AND s.label LIKE $q ESCAPE '\'))
            ORDER BY f.source_path,f.node_id LIMIT $p2 OFFSET $p3;
            """, projectId, revision, limit, offset);
        command.Parameters.AddWithValue("$kind", (object?)kind ?? DBNull.Value);
        command.Parameters.AddWithValue("$q", "%" + query.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%");
        var nodes = new List<GraphNode>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct)) nodes.Add(ReadNode(reader));
        return nodes;
    }
}
