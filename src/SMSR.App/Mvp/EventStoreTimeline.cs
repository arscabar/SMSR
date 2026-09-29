using Microsoft.Data.Sqlite;

namespace SMSR.App.Mvp;

public sealed partial class EventStore
{
    public async Task<IReadOnlyList<WorkflowEvent>> GetTimelineEventsAsync(string projectId, string workflowId,
        int limit = 200, CancellationToken ct = default)
    {
        if (limit is < 1 or > 1000) throw new ArgumentOutOfRangeException(nameof(limit));
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(ct);
        var command = connection.CreateCommand();
        command.CommandText = "SELECT event_id, node_id, agent_id, event_type, status, summary, error, created_at_utc, payload_json FROM events WHERE project_id=$projectId AND workflow_id=$workflowId ORDER BY created_at_utc DESC, rowid DESC LIMIT $limit;";
        command.Parameters.AddWithValue("$projectId", projectId);
        command.Parameters.AddWithValue("$workflowId", workflowId);
        command.Parameters.AddWithValue("$limit", limit);
        var entries = new List<WorkflowEvent>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct)) entries.Add(ReadEvent(reader));
        entries.Reverse();
        return entries;
    }

    public async Task<long> GetWorkflowEventCountAsync(string projectId, string workflowId, CancellationToken ct = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(ct);
        var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM events WHERE project_id=$projectId AND workflow_id=$workflowId;";
        command.Parameters.AddWithValue("$projectId", projectId);
        command.Parameters.AddWithValue("$workflowId", workflowId);
        return Convert.ToInt64(await command.ExecuteScalarAsync(ct));
    }
}
