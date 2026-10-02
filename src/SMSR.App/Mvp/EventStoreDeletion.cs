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
            count.CommandText = $"SELECT COUNT(*) FROM (SELECT workflow_id FROM events{where} UNION SELECT workflow_id FROM plan_nodes{where} UNION SELECT workflow_id FROM plan_revisions{where} UNION SELECT workflow_id FROM workflow_evidence_links{where} UNION SELECT workflow_id FROM workflow_context{where} UNION SELECT workflow_id FROM agent_heartbeats{where});";
            AddScopeParameters(count, projectId, workflowId);
            var deletedWorkflows = Convert.ToInt32(await count.ExecuteScalarAsync(cancellationToken));
            foreach (var table in new[] { "workflow_evidence_links", "plan_revision_nodes", "plan_revisions", "events", "current_state", "plan_nodes", "summaries", "workflow_context", "agent_heartbeats", "daily_activities" })
            {
                var command = connection.CreateCommand();
                command.Transaction = transaction;
                var tableWhere = table == "daily_activities" && workflowId is not null
                    ? " WHERE project_id=$projectId AND workflow_id=$workflowId" : where;
                command.CommandText = $"DELETE FROM {table}{tableWhere};";
                AddScopeParameters(command, projectId, workflowId);
                await command.ExecuteNonQueryAsync(cancellationToken);
            }
            if (workflowId is null)
            {
                var cross = connection.CreateCommand();
                cross.Transaction = transaction;
                cross.CommandText = projectId is null ? "DELETE FROM graph_cross_repo_edges;"
                    : "DELETE FROM graph_cross_repo_edges WHERE source_project_id=$projectId OR target_project_id=$projectId;";
                AddScopeParameters(cross, projectId, null);
                await cross.ExecuteNonQueryAsync(cancellationToken);
                foreach (var table in new[] { "graph_deep_manifests", "graph_deep_edges", "graph_deep_status", "graph_index_formats", "graph_derived", "graph_feedback", "graph_revision_edges", "graph_revision_nodes",
                    "graph_issues", "graph_edges", "graph_nodes", "graph_files", "graph_projects" })
                {
                    var command = connection.CreateCommand();
                    command.Transaction = transaction;
                    command.CommandText = $"DELETE FROM {table}{where};";
                    AddScopeParameters(command, projectId, null);
                    await command.ExecuteNonQueryAsync(cancellationToken);
                }
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
