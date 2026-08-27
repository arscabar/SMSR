using Microsoft.Data.Sqlite;

namespace SMSR.App.Mvp;

public sealed partial class EventStore
{
    public async Task<IReadOnlyList<WorkflowEvent>> GetEventsAsync(string projectId, string workflowId, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        var command = connection.CreateCommand();
        command.CommandText = """
            SELECT event_id, node_id, agent_id, event_type, status, summary, error, created_at_utc FROM events
            WHERE project_id = $projectId AND workflow_id = $workflowId ORDER BY created_at_utc, rowid;
            """;
        command.Parameters.AddWithValue("$projectId", projectId);
        command.Parameters.AddWithValue("$workflowId", workflowId);
        var events = new List<WorkflowEvent>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            events.Add(new(reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3), reader.GetString(4), reader.IsDBNull(5) ? null : reader.GetString(5), reader.IsDBNull(6) ? null : reader.GetString(6), DateTimeOffset.Parse(reader.GetString(7))));
        return events;
    }
}
