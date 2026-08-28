using Microsoft.Data.Sqlite;

namespace SMSR.App.Mvp;

public sealed partial class EventStore
{
    public async Task<IReadOnlyList<AgentState>> GetAgentsAsync(string projectId, string workflowId, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        var command = connection.CreateCommand();
        command.CommandText = "SELECT agent_id, agent_role, status, node_id, summary, retry_count, heartbeat_at_utc FROM agent_heartbeats WHERE project_id = $projectId AND workflow_id = $workflowId ORDER BY heartbeat_at_utc DESC;";
        command.Parameters.AddWithValue("$projectId", projectId);
        command.Parameters.AddWithValue("$workflowId", workflowId);
        var agents = new List<AgentState>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var heartbeatAt = DateTimeOffset.Parse(reader.GetString(6));
            var status = reader.GetString(2);
            var isStale = status == "ACTIVE" && DateTimeOffset.UtcNow - heartbeatAt > TimeSpan.FromSeconds(90);
            agents.Add(new(reader.GetString(0), reader.GetString(1), status, reader.IsDBNull(3) ? null : reader.GetString(3), reader.IsDBNull(4) ? null : reader.GetString(4), reader.GetInt32(5), heartbeatAt, isStale));
        }
        return agents;
    }
}
