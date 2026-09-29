using Microsoft.Data.Sqlite;

namespace SMSR.App.Mvp;

public sealed record WorkflowEvidenceLink(string NodeId, string EventId, string Reference, DateTimeOffset CreatedAt);

public sealed partial class EventStore
{
    private static async Task SaveEvidenceAsync(SqliteConnection connection, SqliteTransaction transaction,
        RecordEventRequest request, string createdAt, CancellationToken ct)
    {
        if (request.Artifacts is not { Count: > 0 }) return;
        var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "INSERT OR IGNORE INTO workflow_evidence_links(project_id, workflow_id, node_id, event_id, reference, created_at_utc) VALUES ($projectId, $workflowId, $nodeId, $eventId, $reference, $createdAt);";
        command.Parameters.AddWithValue("$projectId", request.ProjectId);
        command.Parameters.AddWithValue("$workflowId", request.WorkflowId);
        command.Parameters.AddWithValue("$nodeId", request.NodeId);
        command.Parameters.AddWithValue("$eventId", request.EventId);
        command.Parameters.AddWithValue("$createdAt", createdAt);
        var reference = command.Parameters.Add("$reference", SqliteType.Text);
        foreach (var artifact in request.Artifacts.Where(value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.Ordinal))
        {
            reference.Value = artifact;
            await command.ExecuteNonQueryAsync(ct);
        }
    }

    public async Task<IReadOnlyList<WorkflowEvidenceLink>> GetEvidenceAsync(string projectId, string workflowId,
        CancellationToken ct = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(ct);
        var command = connection.CreateCommand();
        command.CommandText = "SELECT node_id, event_id, reference, created_at_utc FROM workflow_evidence_links WHERE project_id=$projectId AND workflow_id=$workflowId ORDER BY created_at_utc, event_id;";
        command.Parameters.AddWithValue("$projectId", projectId);
        command.Parameters.AddWithValue("$workflowId", workflowId);
        var links = new List<WorkflowEvidenceLink>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            links.Add(new(reader.GetString(0), reader.GetString(1), reader.GetString(2), DateTimeOffset.Parse(reader.GetString(3))));
        return links;
    }
}
