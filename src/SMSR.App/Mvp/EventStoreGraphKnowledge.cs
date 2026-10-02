using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace SMSR.App.Mvp;

public sealed partial class EventStore
{
    internal async Task<GraphHyperedge[]> HyperedgeSliceAsync(string projectId, int revision,
        string? nodeId, int offset, int limit, CancellationToken ct)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(ct);
        using var query = GraphSql.Create(connection, null, """
            SELECT payload_json FROM graph_revision_hyperedges h WHERE project_id=$p0 AND revision=$p1
              AND ($p2 IS NULL OR EXISTS(SELECT 1 FROM json_each(h.payload_json,'$.members') m
                WHERE json_extract(m.value,'$.nodeId')=$p2))
            ORDER BY hyperedge_id LIMIT $p3 OFFSET $p4;
            """, projectId, revision, nodeId, limit, offset);
        var items = new List<GraphHyperedge>();
        await using var rows = await query.ExecuteReaderAsync(ct);
        while (await rows.ReadAsync(ct)) items.Add(JsonSerializer.Deserialize<GraphHyperedge>(rows.GetString(0), GraphWorker.Json)!);
        return items.ToArray();
    }
}
