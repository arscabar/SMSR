using Microsoft.Data.Sqlite;

namespace SMSR.App.Mvp;

public sealed partial class EventStore
{
    private static Task UpsertHeartbeatAsync(SqliteConnection connection, SqliteTransaction? transaction, RecordEventRequest request, string createdAt, CancellationToken cancellationToken)
        => UpsertHeartbeatAsync(connection, transaction, new AgentHeartbeatRequest(request.ProjectId, request.WorkflowId, request.AgentId, request.AgentRole ?? "worker", AgentStatus(request.Status), request.NodeId, request.Summary ?? request.Error, request.RetryCount), createdAt, cancellationToken);

    private static async Task UpsertHeartbeatAsync(SqliteConnection connection, SqliteTransaction? transaction, AgentHeartbeatRequest request, string createdAt, CancellationToken cancellationToken)
    {
        var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "INSERT INTO agent_heartbeats(project_id, workflow_id, agent_id, agent_role, status, node_id, summary, retry_count, heartbeat_at_utc) VALUES ($projectId, $workflowId, $agentId, $agentRole, $status, $nodeId, $summary, $retryCount, $heartbeatAt) ON CONFLICT(project_id, workflow_id, agent_id) DO UPDATE SET agent_role = excluded.agent_role, status = excluded.status, node_id = excluded.node_id, summary = excluded.summary, retry_count = excluded.retry_count, heartbeat_at_utc = excluded.heartbeat_at_utc;";
        command.Parameters.AddWithValue("$projectId", request.ProjectId);
        command.Parameters.AddWithValue("$workflowId", request.WorkflowId);
        command.Parameters.AddWithValue("$agentId", request.AgentId);
        command.Parameters.AddWithValue("$agentRole", request.AgentRole);
        command.Parameters.AddWithValue("$status", request.Status);
        command.Parameters.AddWithValue("$nodeId", request.NodeId ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("$summary", request.Summary ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("$retryCount", request.RetryCount);
        command.Parameters.AddWithValue("$heartbeatAt", createdAt);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static string AgentStatus(string nodeStatus) => nodeStatus switch
    {
        "SUCCESS" => "IDLE",
        "FAILED" or "BLOCKED" => "FAILED",
        _ => "ACTIVE"
    };
}
