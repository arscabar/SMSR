using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace SMSR.App.Mvp;

public sealed partial class EventStore
{
    public async Task<IReadOnlyList<string>> GetGraphScopeAsync(string projectId, CancellationToken ct = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(ct);
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT scope_json FROM graph_projects WHERE project_id=$project;";
        command.Parameters.AddWithValue("$project", projectId);
        var value = await command.ExecuteScalarAsync(ct);
        return value is string json ? JsonSerializer.Deserialize<string[]>(json) ?? [] : [];
    }
}
