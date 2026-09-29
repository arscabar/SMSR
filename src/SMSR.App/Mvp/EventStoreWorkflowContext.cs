using Microsoft.Data.Sqlite;

namespace SMSR.App.Mvp;

public sealed partial class EventStore
{
    public async Task SaveWorkflowContextAsync(WorkflowContext context, CancellationToken cancellationToken = default)
    {
        if (EventValidation.Validate(context) is { } error) throw new ArgumentException(error);
        await _writeGate.WaitAsync(cancellationToken);
        try
        {
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            var command = connection.CreateCommand();
            command.CommandText = """
                INSERT INTO workflow_context(project_id, workflow_id, reason, approach, result, updated_at_utc)
                VALUES ($projectId, $workflowId, $reason, $approach, $result, $updatedAt)
                ON CONFLICT(project_id, workflow_id) DO UPDATE SET
                  reason=COALESCE(excluded.reason, workflow_context.reason),
                  approach=COALESCE(excluded.approach, workflow_context.approach),
                  result=COALESCE(excluded.result, workflow_context.result),
                  updated_at_utc=excluded.updated_at_utc;
                """;
            command.Parameters.AddWithValue("$projectId", context.ProjectId);
            command.Parameters.AddWithValue("$workflowId", context.WorkflowId);
            command.Parameters.AddWithValue("$reason", Value(context.Reason));
            command.Parameters.AddWithValue("$approach", Value(context.Approach));
            command.Parameters.AddWithValue("$result", Value(context.Result));
            command.Parameters.AddWithValue("$updatedAt", context.UpdatedAt.ToString("O"));
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        finally { _writeGate.Release(); }
    }

    public async Task<WorkflowContext?> GetWorkflowContextAsync(string projectId, string workflowId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        var command = connection.CreateCommand();
        command.CommandText = "SELECT reason, approach, result, updated_at_utc FROM workflow_context WHERE project_id=$projectId AND workflow_id=$workflowId;";
        command.Parameters.AddWithValue("$projectId", projectId);
        command.Parameters.AddWithValue("$workflowId", workflowId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken)
            ? new(projectId, workflowId, Text(reader, 0), Text(reader, 1), Text(reader, 2), DateTimeOffset.Parse(reader.GetString(3)))
            : null;
    }

    private static object Value(string? text) => string.IsNullOrWhiteSpace(text) ? DBNull.Value : text;
    private static string? Text(SqliteDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
}
