using Microsoft.Data.Sqlite;

namespace SMSR.App.Mvp;

public sealed partial class EventStore
{
    public async Task SaveSummaryAsync(WorkflowSummary summary, string? sourceEventId, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO summaries(project_id, workflow_id, source_last_event_id, content, created_at_utc) VALUES ($projectId, $workflowId, $sourceEventId, $content, $createdAt);";
        command.Parameters.AddWithValue("$projectId", summary.ProjectId);
        command.Parameters.AddWithValue("$workflowId", summary.WorkflowId);
        command.Parameters.AddWithValue("$sourceEventId", sourceEventId ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("$content", summary.Content);
        command.Parameters.AddWithValue("$createdAt", summary.CreatedAt.ToString("O"));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<WorkflowSummary?> GetLatestSummaryAsync(string projectId, string workflowId, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        var command = connection.CreateCommand();
        command.CommandText = "SELECT content, created_at_utc FROM summaries WHERE project_id = $projectId AND workflow_id = $workflowId ORDER BY created_at_utc DESC, id DESC LIMIT 1;";
        command.Parameters.AddWithValue("$projectId", projectId);
        command.Parameters.AddWithValue("$workflowId", workflowId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? new(projectId, workflowId, reader.GetString(0), DateTimeOffset.Parse(reader.GetString(1))) : null;
    }
}
