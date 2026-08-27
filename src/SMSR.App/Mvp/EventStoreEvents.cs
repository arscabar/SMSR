using System.IO;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace SMSR.App.Mvp;

public sealed partial class EventStore
{
    public async Task<WorkflowEvent?> GetLatestEventAsync(string projectId, string workflowId, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        var command = connection.CreateCommand();
        command.CommandText = """
            SELECT event_id, node_id, agent_id, event_type, status, summary, error, created_at_utc FROM events
            WHERE project_id = $projectId AND workflow_id = $workflowId ORDER BY created_at_utc DESC, rowid DESC LIMIT 1;
            """;
        command.Parameters.AddWithValue("$projectId", projectId);
        command.Parameters.AddWithValue("$workflowId", workflowId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? ReadEvent(reader) : null;
    }

    public async Task WriteEventsJsonLinesAsync(string projectId, string workflowId, string path, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        var command = connection.CreateCommand();
        command.CommandText = "SELECT event_id, node_id, agent_id, event_type, status, summary, error, created_at_utc FROM events WHERE project_id = $projectId AND workflow_id = $workflowId ORDER BY created_at_utc, rowid;";
        command.Parameters.AddWithValue("$projectId", projectId);
        command.Parameters.AddWithValue("$workflowId", workflowId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        await using var writer = new StreamWriter(path, false, new UTF8Encoding(false));
        while (await reader.ReadAsync(cancellationToken))
            await writer.WriteLineAsync(JsonSerializer.Serialize(ReadEvent(reader)));
    }

    private static WorkflowEvent ReadEvent(SqliteDataReader reader)
        => new(reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3), reader.GetString(4), reader.IsDBNull(5) ? null : reader.GetString(5), reader.IsDBNull(6) ? null : reader.GetString(6), DateTimeOffset.Parse(reader.GetString(7)));
}
