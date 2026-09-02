using Microsoft.Data.Sqlite;

namespace SMSR.App.Mvp;

public sealed partial class EventStore
{
    private static async Task CleanupRemovedPlanNodesAsync(SqliteConnection connection,
        SqliteTransaction transaction, string projectId, string workflowId, CancellationToken cancellationToken)
    {
        var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            DELETE FROM current_state WHERE project_id=$projectId AND workflow_id=$workflowId
              AND NOT EXISTS (SELECT 1 FROM plan_nodes p WHERE p.project_id=current_state.project_id
                AND p.workflow_id=current_state.workflow_id AND p.node_id=current_state.node_id);
            UPDATE agent_heartbeats SET node_id=NULL WHERE project_id=$projectId AND workflow_id=$workflowId
              AND node_id IS NOT NULL AND NOT EXISTS (SELECT 1 FROM plan_nodes p
                WHERE p.project_id=agent_heartbeats.project_id AND p.workflow_id=agent_heartbeats.workflow_id
                AND p.node_id=agent_heartbeats.node_id);
            """;
        command.Parameters.AddWithValue("$projectId", projectId);
        command.Parameters.AddWithValue("$workflowId", workflowId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
