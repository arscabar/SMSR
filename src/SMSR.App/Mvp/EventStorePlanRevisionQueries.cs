using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace SMSR.App.Mvp;

public sealed partial class EventStore
{
    public async Task<IReadOnlyList<PlanRevision>> GetPlanRevisionsAsync(string projectId, string workflowId,
        CancellationToken ct = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(ct);
        var command = connection.CreateCommand();
        command.CommandText = "SELECT revision, change_reason, created_at_utc FROM plan_revisions WHERE project_id=$projectId AND workflow_id=$workflowId ORDER BY revision;";
        command.Parameters.AddWithValue("$projectId", projectId);
        command.Parameters.AddWithValue("$workflowId", workflowId);
        var headers = new List<(int Revision, string? Reason, DateTimeOffset CreatedAt)>();
        await using (var reader = await command.ExecuteReaderAsync(ct))
            while (await reader.ReadAsync(ct))
                headers.Add((reader.GetInt32(0), reader.IsDBNull(1) ? null : reader.GetString(1),
                    DateTimeOffset.Parse(reader.GetString(2))));
        var revisions = new List<PlanRevision>(headers.Count);
        foreach (var header in headers)
        {
            command.Parameters.Clear();
            command.CommandText = "SELECT node_id, title, weight, depends_on_json, metadata_json FROM plan_revision_nodes WHERE project_id=$projectId AND workflow_id=$workflowId AND revision=$revision ORDER BY position;";
            command.Parameters.AddWithValue("$projectId", projectId);
            command.Parameters.AddWithValue("$workflowId", workflowId);
            command.Parameters.AddWithValue("$revision", header.Revision);
            var nodes = new List<PlanNodeDefinition>();
            await using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                var metadata = PlanNodeMetadata.Parse(reader.GetString(4));
                nodes.Add(new(reader.GetString(0), reader.GetString(1), reader.GetInt32(2),
                    JsonSerializer.Deserialize<string[]>(reader.GetString(3)) ?? [], metadata.ParentNodeId,
                    metadata.AssignedAgentId, metadata.AgentRole, metadata.CompletionCriteria));
            }
            revisions.Add(new(header.Revision, header.CreatedAt, header.Reason, nodes));
        }
        return revisions;
    }
}
