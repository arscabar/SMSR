using Microsoft.Data.Sqlite;

namespace SMSR.App.Mvp;

public sealed partial class EventStore
{
    internal const string GraphFormat = "commonmark-1.3.2-canonical-path-v17-graphify-0.9.73-knowledge";

    internal async Task<bool> IsGraphFormatCurrentAsync(string projectId, CancellationToken ct)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(ct);
        using var command = GraphSql.Create(connection, null,
            "SELECT format FROM graph_index_formats WHERE project_id=$p0;", projectId);
        return await command.ExecuteScalarAsync(ct) as string == GraphFormat;
    }
}
