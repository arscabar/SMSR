using Microsoft.Data.Sqlite;

namespace SMSR.App.Mvp;

public sealed partial class EventStore
{
    public async Task<WorkflowState> GetStateAsync(string projectId, string workflowId, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        var command = connection.CreateCommand();
        command.CommandText = """
            SELECT node_id, agent_id, status, summary, error, updated_at_utc, metadata_json FROM current_state
            WHERE project_id = $projectId AND workflow_id = $workflowId ORDER BY node_id;
            """;
        command.Parameters.AddWithValue("$projectId", projectId);
        command.Parameters.AddWithValue("$workflowId", workflowId);
        var nodes = new List<StateNode>();
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
            while (await reader.ReadAsync(cancellationToken))
            {
                var metadata = EventMetadata.Parse(reader.GetString(6));
                nodes.Add(new(reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.IsDBNull(3) ? null : reader.GetString(3), reader.IsDBNull(4) ? null : reader.GetString(4), DateTimeOffset.Parse(reader.GetString(5)), metadata.AgentRole, metadata.ProgressPercentage, metadata.RetryCount, metadata.NextAction, metadata.Artifacts, metadata.HeartbeatAt));
            }
        return new(projectId, workflowId, nodes, await GetAgentsAsync(projectId, workflowId, cancellationToken));
    }

    public async Task<IReadOnlyList<RecentEvent>> GetRecentEventsAsync(string projectId, string workflowId, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        var command = connection.CreateCommand();
        command.CommandText = """
            SELECT node_id, agent_id, status, summary, error, created_at_utc, payload_json FROM events
            WHERE project_id = $projectId AND workflow_id = $workflowId
            ORDER BY created_at_utc DESC, rowid DESC LIMIT 10;
            """;
        command.Parameters.AddWithValue("$projectId", projectId);
        command.Parameters.AddWithValue("$workflowId", workflowId);
        var events = new List<RecentEvent>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var request = EventPayload.Parse(reader.GetString(6));
            events.Add(new(reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.IsDBNull(3) ? null : reader.GetString(3), reader.IsDBNull(4) ? null : reader.GetString(4), DateTimeOffset.Parse(reader.GetString(5)), request?.AgentRole, request?.ProgressPercentage, request?.RetryCount ?? 0, request?.Artifacts));
        }
        return events;
    }
}
