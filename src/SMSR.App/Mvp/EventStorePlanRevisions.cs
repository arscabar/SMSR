using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace SMSR.App.Mvp;

public sealed partial class EventStore
{
    private static async Task SavePlanRevisionAsync(SqliteConnection connection, SqliteTransaction transaction,
        string projectId, string workflowId, IReadOnlyList<PlanNodeDefinition> nodes, string? reason, CancellationToken ct)
    {
        var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT COALESCE(MAX(revision), 0) + 1 FROM plan_revisions WHERE project_id=$projectId AND workflow_id=$workflowId;";
        command.Parameters.AddWithValue("$projectId", projectId);
        command.Parameters.AddWithValue("$workflowId", workflowId);
        var revision = Convert.ToInt32(await command.ExecuteScalarAsync(ct));
        command.Parameters.Clear();
        command.CommandText = "INSERT INTO plan_revisions(project_id, workflow_id, revision, change_reason, created_at_utc) VALUES ($projectId, $workflowId, $revision, $reason, $createdAt);";
        command.Parameters.AddWithValue("$projectId", projectId);
        command.Parameters.AddWithValue("$workflowId", workflowId);
        command.Parameters.AddWithValue("$revision", revision);
        command.Parameters.AddWithValue("$reason", string.IsNullOrWhiteSpace(reason) ? DBNull.Value : reason);
        command.Parameters.AddWithValue("$createdAt", DateTimeOffset.UtcNow.ToString("O"));
        await command.ExecuteNonQueryAsync(ct);
        for (var position = 0; position < nodes.Count; position++)
        {
            var node = nodes[position];
            command.Parameters.Clear();
            command.CommandText = "INSERT INTO plan_revision_nodes(project_id, workflow_id, revision, position, node_id, title, weight, depends_on_json, metadata_json) VALUES ($projectId, $workflowId, $revision, $position, $nodeId, $title, $weight, $dependsOn, $metadata);";
            command.Parameters.AddWithValue("$projectId", projectId);
            command.Parameters.AddWithValue("$workflowId", workflowId);
            command.Parameters.AddWithValue("$revision", revision);
            command.Parameters.AddWithValue("$position", position);
            command.Parameters.AddWithValue("$nodeId", node.NodeId);
            command.Parameters.AddWithValue("$title", node.Title);
            command.Parameters.AddWithValue("$weight", node.Weight);
            command.Parameters.AddWithValue("$dependsOn", JsonSerializer.Serialize(node.DependsOn ?? []));
            command.Parameters.AddWithValue("$metadata", JsonSerializer.Serialize(PlanNodeMetadata.From(node)));
            await command.ExecuteNonQueryAsync(ct);
        }
    }
}
