using Microsoft.Data.Sqlite;
using System.Text.Json;

namespace SMSR.App.Mvp;

public sealed partial class EventStore
{
    internal async Task<IReadOnlyList<GraphRoleJob>> GraphRoleJobsAsync(string projectId, CancellationToken ct)
    {
        await using var connection = new SqliteConnection(_connectionString); await connection.OpenAsync(ct);
        using var command = GraphSql.Create(connection, null,
            "SELECT payload FROM graph_derived WHERE project_id=$p0 AND kind='node-role-job' ORDER BY created_at_utc,item_key;", projectId);
        await using var reader = await command.ExecuteReaderAsync(ct); var jobs = new List<GraphRoleJob>();
        while (await reader.ReadAsync(ct))
            if (JsonSerializer.Deserialize<GraphRoleJob>(reader.GetString(0), GraphWorker.Json) is { } job) jobs.Add(job);
        return jobs;
    }
}
