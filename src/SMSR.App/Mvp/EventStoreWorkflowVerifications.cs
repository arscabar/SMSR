using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace SMSR.App.Mvp;

public sealed partial class EventStore
{
    public async Task<IReadOnlyList<string>> GetWorkflowVerificationsAsync(string projectId, string workflowId,
        CancellationToken ct = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(ct);
        using var command = GraphSql.Create(connection, null, """
            SELECT verifications_json FROM daily_activities
            WHERE project_id=$p0 AND workflow_id=$p1 ORDER BY recorded_at_utc DESC LIMIT 100;
            """, projectId, workflowId);
        var verifications = new HashSet<string>(StringComparer.Ordinal);
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            foreach (var item in JsonSerializer.Deserialize<string[]>(reader.GetString(0)) ?? [])
                if (!string.IsNullOrWhiteSpace(item)) verifications.Add(item);
        return verifications.Take(100).ToArray();
    }
}
