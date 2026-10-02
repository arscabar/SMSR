using Microsoft.Data.Sqlite;

namespace SMSR.App.Mvp;

public sealed partial class EventStore
{
    public async Task<IReadOnlyList<GraphNode>> GetGraphRoutesAsync(string projectId, int limit, CancellationToken ct = default, int? revision = null)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(ct);
        var table = revision.HasValue ? "graph_revision_nodes" : "graph_nodes";
        var version = revision.HasValue ? "AND revision=$revision" : "";
        using var command = GraphSql.Create(connection, null, $"""
            SELECT node_id,owner_path,kind,label,source_path,source_line,content_hash,details_json
            FROM {table} WHERE project_id=$p0 {version} AND kind IN ('api_route','mcp_tool')
            ORDER BY source_path,source_line LIMIT $p1;
            """, projectId, limit);
        if (revision.HasValue) command.Parameters.AddWithValue("$revision", revision.Value);
        var nodes = new List<GraphNode>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct)) nodes.Add(ReadNode(reader));
        return nodes;
    }
}
