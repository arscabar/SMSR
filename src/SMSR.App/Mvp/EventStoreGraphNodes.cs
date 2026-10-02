using Microsoft.Data.Sqlite;

namespace SMSR.App.Mvp;

public sealed partial class EventStore
{
    public async Task<GraphNode?> GetGraphNodeAsync(string projectId, string nodeId, CancellationToken ct = default, int? revision = null)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(ct);
        using var command = GraphSql.Create(connection, null, $"""
            SELECT node_id,owner_path,kind,label,source_path,source_line,content_hash,details_json
            FROM {(revision.HasValue ? "graph_revision_nodes" : "graph_nodes")} WHERE project_id=$p0 AND node_id=$p1
            {(revision.HasValue ? "AND revision=$revision" : "")};
            """, projectId, nodeId);
        if (revision.HasValue) command.Parameters.AddWithValue("$revision", revision.Value);
        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? ReadNode(reader) : null;
    }

    public async Task<IReadOnlyList<GraphNode>> SearchGraphNodesAsync(string projectId, string query,
        int limit = 101, CancellationToken ct = default, int? revision = null, string? kind = null, int offset = 0)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(ct);
        using var command = GraphSql.Create(connection, null, $"""
            SELECT node_id,owner_path,kind,label,source_path,source_line,content_hash,details_json
            FROM {(revision.HasValue ? "graph_revision_nodes" : "graph_nodes")} WHERE project_id=$p0
            {(revision.HasValue ? "AND revision=$revision" : "")} AND ($kind IS NULL OR ($kind='files' AND kind<>'heading') OR ($kind='code' AND kind IN ('code','symbol')) OR ($kind='document' AND kind IN ('document','concept','requirement','rationale')) OR kind=$kind)
            AND (label LIKE $p1 ESCAPE '\' OR source_path LIKE $p1 ESCAPE '\')
            ORDER BY CASE WHEN kind='document' THEN 0 WHEN kind IN ('code','symbol') THEN 1 ELSE 2 END, source_path, source_line, node_id
            LIMIT $p2 OFFSET $offset;
            """, projectId, "%" + query.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%", limit);
        if (revision.HasValue) command.Parameters.AddWithValue("$revision", revision.Value);
        command.Parameters.AddWithValue("$kind", (object?)kind ?? DBNull.Value);
        command.Parameters.AddWithValue("$offset", offset);
        var nodes = new List<GraphNode>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct)) nodes.Add(ReadNode(reader));
        return nodes;
    }

    public async Task<IReadOnlyList<GraphNode>> GetGraphNodesAsync(string projectId,
        IReadOnlyCollection<string> ids, CancellationToken ct = default, int? revision = null)
    {
        if (ids.Count == 0) return [];
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(ct);
        var nodes = new List<GraphNode>();
        foreach (var chunk in ids.Chunk(500))
        {
            var placeholders = string.Join(",", Enumerable.Range(1, chunk.Length).Select(index => "$p" + index));
            using var command = GraphSql.Create(connection, null, $"""
                SELECT node_id,owner_path,kind,label,source_path,source_line,content_hash,details_json
                FROM {(revision.HasValue ? "graph_revision_nodes" : "graph_nodes")} WHERE project_id=$p0
                {(revision.HasValue ? "AND revision=$revision" : "")} AND node_id IN ({placeholders});
                """, [projectId, .. chunk.Cast<object>()]);
            if (revision.HasValue) command.Parameters.AddWithValue("$revision", revision.Value);
            await using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct)) nodes.Add(ReadNode(reader));
        }
        return nodes;
    }

    private static GraphNode ReadNode(SqliteDataReader reader)
        => new(reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3),
            reader.GetString(4), reader.GetInt32(5), reader.GetString(6), reader.IsDBNull(7) ? null
                : System.Text.Json.JsonSerializer.Deserialize<GraphEntityDetails>(reader.GetString(7), GraphWorker.Json));
}
