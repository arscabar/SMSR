using Microsoft.Data.Sqlite;

namespace SMSR.App.Mvp;

public sealed partial class EventStore
{
    public async Task<IReadOnlyList<GraphNode>> GetGraphRevisionNodesAsync(string projectId, int revision, CancellationToken ct = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(ct);
        using var command = GraphSql.Create(connection, null, """
            SELECT node_id,owner_path,kind,label,source_path,source_line,content_hash
            FROM graph_revision_nodes WHERE project_id=$p0 AND revision=$p1;
            """, projectId, revision);
        var nodes = new List<GraphNode>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct)) nodes.Add(new(reader.GetString(0), reader.GetString(1), reader.GetString(2),
            reader.GetString(3), reader.GetString(4), reader.GetInt32(5), reader.GetString(6)));
        return nodes;
    }

    public async Task<IReadOnlyList<GraphEdge>> GetGraphRevisionEdgesAsync(string projectId, int revision, CancellationToken ct = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(ct);
        using var command = GraphSql.Create(connection, null, """
            SELECT source_id,target_id,relation,owner_path,source_line,resolution,confidence
            FROM graph_revision_edges WHERE project_id=$p0 AND revision=$p1;
            """, projectId, revision);
        var edges = new List<GraphEdge>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct)) edges.Add(new(reader.GetString(0), reader.GetString(1), reader.GetString(2),
            reader.GetString(3), reader.GetInt32(4), reader.GetString(5), reader.GetString(6)));
        return edges;
    }
}
