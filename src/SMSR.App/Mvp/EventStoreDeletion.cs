using Microsoft.Data.Sqlite;

namespace SMSR.App.Mvp;

public sealed partial class EventStore
{
    public Task<int> DeleteWorkflowAsync(string projectId, string workflowId,
        CancellationToken cancellationToken = default)
        => DeleteScopeAsync(projectId, workflowId, cancellationToken);

    public Task<int> DeleteProjectAsync(string projectId, CancellationToken cancellationToken = default)
        => DeleteScopeAsync(projectId, null, cancellationToken);

    public Task<int> DeleteAllAsync(CancellationToken cancellationToken = default)
        => DeleteScopeAsync(null, null, cancellationToken);

    private async Task<int> DeleteScopeAsync(string? projectId, string? workflowId,
        CancellationToken cancellationToken)
    {
        await _writeGate.WaitAsync(cancellationToken);
        try
        {
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);
            var where = projectId is null ? "" : workflowId is null
                ? " WHERE project_id=$projectId" : " WHERE project_id=$projectId AND workflow_id=$workflowId";
            var count = connection.CreateCommand();
            count.Transaction = transaction;
            count.CommandText = $"SELECT COUNT(*) FROM (SELECT workflow_id FROM events{where} UNION SELECT workflow_id FROM plan_nodes{where} UNION SELECT workflow_id FROM agent_heartbeats{where});";
            AddScopeParameters(count, projectId, workflowId);
            var deletedWorkflows = Convert.ToInt32(await count.ExecuteScalarAsync(cancellationToken));
            foreach (var table in new[] { "events", "current_state", "plan_nodes", "summaries", "agent_heartbeats" })
            {
                var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = $"DELETE FROM {table}{where};";
                AddScopeParameters(command, projectId, workflowId);
                await command.ExecuteNonQueryAsync(cancellationToken);
            }
            await transaction.CommitAsync(cancellationToken);
            return deletedWorkflows;
        }
        finally { _writeGate.Release(); }
    }

    private static void AddScopeParameters(SqliteCommand command, string? projectId, string? workflowId)
    {
        if (projectId is not null) command.Parameters.AddWithValue("$projectId", projectId);
        if (workflowId is not null) command.Parameters.AddWithValue("$workflowId", workflowId);
    }
}
