using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace SMSR.App.Mvp;

public sealed partial class EventStore
{
    public async Task SavePlanAsync(string projectId, string workflowId, IReadOnlyList<PlanNodeDefinition> nodes, CancellationToken cancellationToken = default)
    {
        await _writeGate.WaitAsync(cancellationToken);
        try
        {
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);
            var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "DELETE FROM plan_nodes WHERE project_id = $projectId AND workflow_id = $workflowId;";
            command.Parameters.AddWithValue("$projectId", projectId);
            command.Parameters.AddWithValue("$workflowId", workflowId);
            await command.ExecuteNonQueryAsync(cancellationToken);
            foreach (var node in nodes)
            {
                command.Parameters.Clear();
                command.CommandText = "INSERT INTO plan_nodes(project_id, workflow_id, node_id, title, weight, depends_on_json, metadata_json) VALUES ($projectId, $workflowId, $nodeId, $title, $weight, $dependsOn, $metadata);";
                command.Parameters.AddWithValue("$projectId", projectId);
                command.Parameters.AddWithValue("$workflowId", workflowId);
                command.Parameters.AddWithValue("$nodeId", node.NodeId);
                command.Parameters.AddWithValue("$title", node.Title);
                command.Parameters.AddWithValue("$weight", node.Weight);
                command.Parameters.AddWithValue("$dependsOn", JsonSerializer.Serialize(node.DependsOn ?? []));
                command.Parameters.AddWithValue("$metadata", JsonSerializer.Serialize(PlanNodeMetadata.From(node)));
                await command.ExecuteNonQueryAsync(cancellationToken);
            }
            await transaction.CommitAsync(cancellationToken);
        }
        finally { _writeGate.Release(); }
    }
}
