using Microsoft.Data.Sqlite;

namespace SMSR.App.Mvp;

public sealed partial class EventStore
{
    public async Task RecordAgentActivityAsync(ActivityRecord record, CancellationToken cancellationToken = default)
    {
        var lifecycle = record.Event is "AGENT_STARTED" or "AGENT_STOPPED";
        if (!lifecycle && record.Model is null && record.ReasoningEffort is null) return;
        if (string.IsNullOrWhiteSpace(record.AgentId) || record.AgentId.Length > 128) return;
        await _writeGate.WaitAsync(cancellationToken);
        try
        {
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            var command = connection.CreateCommand();
            command.CommandText = """
                INSERT INTO agent_heartbeats(project_id, workflow_id, agent_id, agent_role, status,
                    retry_count, heartbeat_at_utc, metadata_json)
                VALUES ($project, $workflow, $agent, $role, $status, 0, $at, $metadata)
                ON CONFLICT(project_id, workflow_id, agent_id) DO UPDATE SET
                    agent_role=CASE WHEN $lifecycle=1 AND agent_heartbeats.heartbeat_at_utc <= excluded.heartbeat_at_utc
                        THEN excluded.agent_role ELSE agent_heartbeats.agent_role END,
                    status=CASE WHEN $lifecycle=1 AND agent_heartbeats.heartbeat_at_utc <= excluded.heartbeat_at_utc
                        THEN excluded.status ELSE agent_heartbeats.status END,
                    heartbeat_at_utc=CASE WHEN $lifecycle=1 AND agent_heartbeats.heartbeat_at_utc <= excluded.heartbeat_at_utc
                        THEN excluded.heartbeat_at_utc ELSE agent_heartbeats.heartbeat_at_utc END,
                    metadata_json=CASE WHEN $hasMetadata=1 AND agent_heartbeats.heartbeat_at_utc <= excluded.heartbeat_at_utc
                        THEN json_patch(agent_heartbeats.metadata_json, excluded.metadata_json)
                        ELSE agent_heartbeats.metadata_json END;
                """;
            command.Parameters.AddWithValue("$project", record.ProjectId);
            command.Parameters.AddWithValue("$workflow", record.WorkflowId);
            command.Parameters.AddWithValue("$agent", record.AgentId);
            command.Parameters.AddWithValue("$role", record.AgentRole ?? "coordinator");
            command.Parameters.AddWithValue("$status", record.Event == "AGENT_STARTED" ? "ACTIVE"
                : record.Event == "AGENT_STOPPED" ? "STOPPED" : "IDLE");
            command.Parameters.AddWithValue("$at", record.TimestampUtc.ToString("O"));
            command.Parameters.AddWithValue("$metadata", new AgentMetadata(record.Model, record.ReasoningEffort, record.ParentAgentId).ToJson());
            command.Parameters.AddWithValue("$lifecycle", lifecycle ? 1 : 0);
            command.Parameters.AddWithValue("$hasMetadata", record.Model is not null || record.ReasoningEffort is not null || record.ParentAgentId is not null ? 1 : 0);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        finally { _writeGate.Release(); }
    }
}
