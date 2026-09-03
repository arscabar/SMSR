using Microsoft.Data.Sqlite;

namespace SMSR.App.Mvp;

public sealed partial class EventStore
{
    public async Task<IReadOnlyList<WorkflowCalendarEntry>> GetWorkflowCalendarAsync(
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        var command = connection.CreateCommand();
        command.CommandText = """
            WITH workflows AS (
              SELECT project_id, workflow_id FROM events
              UNION SELECT project_id, workflow_id FROM plan_nodes
              UNION SELECT project_id, workflow_id FROM agent_heartbeats
            ), activity AS (
              SELECT project_id, workflow_id, MAX(at) updated_at FROM (
                SELECT project_id, workflow_id, created_at_utc at FROM events
                UNION ALL SELECT project_id, workflow_id, heartbeat_at_utc FROM agent_heartbeats
                UNION ALL SELECT project_id, workflow_id, created_at_utc FROM summaries
              ) GROUP BY project_id, workflow_id
            )
            SELECT w.project_id, w.workflow_id,
              COALESCE(
                (SELECT p.title FROM plan_nodes p WHERE p.project_id=w.project_id AND p.workflow_id=w.workflow_id
                  ORDER BY json_extract(p.metadata_json, '$.ParentNodeId') IS NOT NULL, p.rowid LIMIT 1),
                (SELECT e.summary FROM events e WHERE e.project_id=w.project_id AND e.workflow_id=w.workflow_id
                  AND e.summary IS NOT NULL AND trim(e.summary) <> '' ORDER BY e.created_at_utc DESC, e.rowid DESC LIMIT 1)),
              (SELECT COUNT(*) FROM plan_nodes p WHERE p.project_id=w.project_id AND p.workflow_id=w.workflow_id),
              (SELECT COUNT(*) FROM plan_nodes p JOIN current_state s ON s.project_id=p.project_id
                AND s.workflow_id=p.workflow_id AND s.node_id=p.node_id WHERE p.project_id=w.project_id
                AND p.workflow_id=w.workflow_id AND s.status IN ('SUCCESS','FAILED','BLOCKED')),
              a.updated_at
            FROM workflows w LEFT JOIN activity a ON a.project_id=w.project_id AND a.workflow_id=w.workflow_id
            ORDER BY a.updated_at IS NULL, a.updated_at DESC, w.project_id, w.workflow_id DESC LIMIT 1000;
            """;
        var result = new List<WorkflowCalendarEntry>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var nodes = reader.GetInt32(3);
            var terminal = reader.GetInt32(4);
            var status = nodes == 0 ? "NO_PLAN" : terminal == nodes ? "TERMINAL" : "ACTIVE";
            result.Add(new(reader.GetString(0), reader.GetString(1), reader.IsDBNull(2) ? null : reader.GetString(2),
                nodes, status, reader.IsDBNull(5) ? null : DateTimeOffset.Parse(reader.GetString(5))));
        }
        return result;
    }
}
