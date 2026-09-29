using Microsoft.Data.Sqlite;

namespace SMSR.App.Mvp;

public sealed partial class EventStore
{
    public async Task<IReadOnlyList<string>> GetGraphNodeIdsAsync(string projectId, int limit, CancellationToken ct = default, int? revision = null)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(ct);
        using var command = GraphSql.Create(connection, null,
            revision.HasValue ? "SELECT node_id FROM graph_revision_nodes WHERE project_id=$p0 AND revision=$revision ORDER BY node_id LIMIT $p1;"
                : "SELECT node_id FROM graph_nodes WHERE project_id=$p0 ORDER BY node_id LIMIT $p1;", projectId, limit);
        if (revision.HasValue) command.Parameters.AddWithValue("$revision", revision.Value);
        var ids = new List<string>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct)) ids.Add(reader.GetString(0));
        return ids;
    }
}
