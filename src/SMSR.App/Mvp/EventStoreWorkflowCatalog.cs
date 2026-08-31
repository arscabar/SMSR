using Microsoft.Data.Sqlite;

namespace SMSR.App.Mvp;

public sealed partial class EventStore
{
    public async Task<IReadOnlyList<WorkflowCatalogEntry>> GetWorkflowCatalogAsync(
        string projectId, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        var command = connection.CreateCommand();
        command.CommandText = """
            WITH workflows AS (
              SELECT workflow_id FROM events WHERE project_id=$projectId
              UNION SELECT workflow_id FROM plan_nodes WHERE project_id=$projectId
              UNION SELECT workflow_id FROM agent_heartbeats WHERE project_id=$projectId
            ), activity AS (
              SELECT workflow_id, MAX(at) updated_at FROM (
                SELECT workflow_id, created_at_utc at FROM events WHERE project_id=$projectId
                UNION ALL SELECT workflow_id, heartbeat_at_utc FROM agent_heartbeats WHERE project_id=$projectId
                UNION ALL SELECT workflow_id, created_at_utc FROM summaries WHERE project_id=$projectId
              ) GROUP BY workflow_id
            )
            SELECT w.workflow_id,
              (SELECT COUNT(*) FROM plan_nodes p WHERE p.project_id=$projectId AND p.workflow_id=w.workflow_id),
              (SELECT COUNT(*) FROM current_state s WHERE s.project_id=$projectId AND s.workflow_id=w.workflow_id
                AND s.status IN ('SUCCESS','FAILED','BLOCKED')),
              a.updated_at
            FROM workflows w LEFT JOIN activity a ON a.workflow_id=w.workflow_id
            ORDER BY a.updated_at IS NULL, a.updated_at DESC, w.workflow_id LIMIT 200;
            """;
        command.Parameters.AddWithValue("$projectId", projectId);
        var result = new List<WorkflowCatalogEntry>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var nodes = reader.GetInt32(1);
            var terminal = reader.GetInt32(2);
            var status = nodes == 0 ? "NO_PLAN" : terminal == nodes ? "TERMINAL" : "ACTIVE";
            result.Add(new(reader.GetString(0), nodes, status,
                reader.IsDBNull(3) ? null : DateTimeOffset.Parse(reader.GetString(3))));
        }
        return result;
    }
}
