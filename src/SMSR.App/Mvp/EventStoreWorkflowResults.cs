using Microsoft.Data.Sqlite;

namespace SMSR.App.Mvp;

internal static class EventStoreWorkflowResults
{
    private const string Automatic = "자동 요약(완료 기록): ";

    public static async Task FillMissingAsync(SqliteConnection connection, SqliteTransaction? transaction,
        string? projectId, string? workflowId, CancellationToken cancellationToken)
    {
        var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            WITH finished AS (
              SELECT p.project_id, p.workflow_id
              FROM plan_nodes p LEFT JOIN current_state s
                ON s.project_id=p.project_id AND s.workflow_id=p.workflow_id AND s.node_id=p.node_id
              WHERE ($projectId IS NULL OR p.project_id=$projectId)
                AND ($workflowId IS NULL OR p.workflow_id=$workflowId)
              GROUP BY p.project_id, p.workflow_id
              HAVING COUNT(*)=SUM(CASE WHEN s.status IN ('SUCCESS','FAILED','CANCELLED') THEN 1 ELSE 0 END)
            ), endings AS (
              SELECT f.project_id, f.workflow_id,
                (SELECT COALESCE(NULLIF(trim(e.summary),''), NULLIF(trim(e.error),''))
                 FROM events e JOIN plan_nodes p ON p.project_id=e.project_id
                   AND p.workflow_id=e.workflow_id AND p.node_id=e.node_id
                 WHERE e.project_id=f.project_id AND e.workflow_id=f.workflow_id
                   AND e.status IN ('SUCCESS','FAILED','BLOCKED','CANCELLED')
                   AND (NULLIF(trim(e.summary),'') IS NOT NULL OR NULLIF(trim(e.error),'') IS NOT NULL)
                 ORDER BY e.created_at_utc DESC, e.rowid DESC LIMIT 1) note
              FROM finished f
            )
            INSERT INTO workflow_context(project_id, workflow_id, reason, approach, result, updated_at_utc)
            SELECT project_id, workflow_id, NULL, NULL, $automatic || note, $updatedAt
            FROM endings WHERE note IS NOT NULL
            ON CONFLICT(project_id, workflow_id) DO UPDATE SET
              result=excluded.result, updated_at_utc=excluded.updated_at_utc
            WHERE workflow_context.result IS NULL OR trim(workflow_context.result)=''
              OR substr(workflow_context.result,1,length($automatic))=$automatic;
            """;
        command.Parameters.AddWithValue("$projectId", (object?)projectId ?? DBNull.Value);
        command.Parameters.AddWithValue("$workflowId", (object?)workflowId ?? DBNull.Value);
        command.Parameters.AddWithValue("$automatic", Automatic);
        command.Parameters.AddWithValue("$updatedAt", DateTimeOffset.UtcNow.ToString("O"));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
