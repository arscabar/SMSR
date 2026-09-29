using Microsoft.Data.Sqlite;

namespace SMSR.App.Mvp;

public sealed partial class EventStore
{
    public async Task<IReadOnlyList<(string Source, string Target)>> GetGraphEdgePairsAsync(
        string projectId, int limit, CancellationToken ct = default, int? revision = null)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(ct);
        using var command = GraphSql.Create(connection, null,
            revision.HasValue ? "SELECT source_id,target_id FROM graph_revision_edges WHERE project_id=$p0 AND revision=$revision LIMIT $p1;"
                : "SELECT source_id,target_id FROM graph_edges WHERE project_id=$p0 LIMIT $p1;", projectId, limit);
        if (revision.HasValue) command.Parameters.AddWithValue("$revision", revision.Value);
        var pairs = new List<(string Source, string Target)>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct)) pairs.Add((reader.GetString(0), reader.GetString(1)));
        return pairs;
    }
}
