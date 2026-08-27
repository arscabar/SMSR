using Microsoft.Data.Sqlite;

namespace SMSR.App.Mvp;

public sealed partial class EventStore
{
    public async Task<bool> RecordAsync(RecordEventRequest request, CancellationToken cancellationToken = default)
    {
        await _writeGate.WaitAsync(cancellationToken);
        try
        {
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);
            var createdAt = DateTimeOffset.UtcNow.ToString("O");
            var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "INSERT INTO events VALUES ($eventId, $projectId, $workflowId, $nodeId, $agentId, $eventType, $status, $summary, $error, $payload, $createdAt) ON CONFLICT(event_id) DO NOTHING;";
            AddEventParameters(command, request, createdAt);
            if (await command.ExecuteNonQueryAsync(cancellationToken) != 1) return false;
            command.Parameters.Clear();
            command.CommandText = "INSERT INTO current_state VALUES ($projectId, $workflowId, $nodeId, $agentId, $status, $summary, $error, $createdAt) ON CONFLICT(project_id, workflow_id, node_id) DO UPDATE SET agent_id = excluded.agent_id, status = excluded.status, summary = excluded.summary, error = excluded.error, updated_at_utc = excluded.updated_at_utc;";
            AddStateParameters(command, request, createdAt);
            await command.ExecuteNonQueryAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return true;
        }
        finally { _writeGate.Release(); }
    }

}
