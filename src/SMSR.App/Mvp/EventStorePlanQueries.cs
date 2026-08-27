using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace SMSR.App.Mvp;

public sealed partial class EventStore
{
    public async Task<WorkflowPlan> GetPlanAsync(string projectId, string workflowId, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        var command = connection.CreateCommand();
        command.CommandText = """
            SELECT p.node_id, p.title, p.weight, p.depends_on_json, s.status, s.summary, s.error, s.updated_at_utc FROM plan_nodes p
            LEFT JOIN current_state s ON s.project_id = p.project_id AND s.workflow_id = p.workflow_id AND s.node_id = p.node_id
            WHERE p.project_id = $projectId AND p.workflow_id = $workflowId ORDER BY p.rowid;
            """;
        command.Parameters.AddWithValue("$projectId", projectId);
        command.Parameters.AddWithValue("$workflowId", workflowId);
        var nodes = new List<PlanNodeState>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            nodes.Add(new(reader.GetString(0), reader.GetString(1), reader.GetInt32(2), JsonSerializer.Deserialize<string[]>(reader.GetString(3)) ?? [], reader.IsDBNull(4) ? "PENDING" : reader.GetString(4), reader.IsDBNull(5) ? null : reader.GetString(5), reader.IsDBNull(6) ? null : reader.GetString(6), reader.IsDBNull(7) ? null : DateTimeOffset.Parse(reader.GetString(7))));
        return new(projectId, workflowId, nodes);
    }
}
