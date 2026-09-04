using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace SMSR.App.Mvp;

public sealed partial class EventStore
{
    public async Task RecordDailyActivityAsync(DailyActivityRequest request,
        CancellationToken cancellationToken = default)
    {
        await _writeGate.WaitAsync(cancellationToken);
        try
        {
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            var command = connection.CreateCommand();
            command.CommandText = """
                INSERT INTO daily_activities(activity_id, project_id, task_id, title, summary, status,
                  files_json, verifications_json, artifacts_json, workflow_id, agent_id, recorded_at_utc)
                VALUES($activityId,$projectId,$taskId,$title,$summary,$status,$files,$verifications,
                  $artifacts,$workflowId,$agentId,$recordedAt)
                ON CONFLICT(activity_id) DO UPDATE SET title=excluded.title, summary=excluded.summary,
                  status=excluded.status, files_json=excluded.files_json,
                  verifications_json=excluded.verifications_json, artifacts_json=excluded.artifacts_json,
                  workflow_id=excluded.workflow_id, agent_id=excluded.agent_id,
                  recorded_at_utc=excluded.recorded_at_utc;
                """;
            command.Parameters.AddWithValue("$activityId", request.ActivityId);
            command.Parameters.AddWithValue("$projectId", request.ProjectId);
            command.Parameters.AddWithValue("$taskId", request.TaskId);
            command.Parameters.AddWithValue("$title", request.Title.Trim());
            command.Parameters.AddWithValue("$summary", request.Summary.Trim());
            command.Parameters.AddWithValue("$status", request.Status);
            command.Parameters.AddWithValue("$files", JsonSerializer.Serialize(request.Files ?? []));
            command.Parameters.AddWithValue("$verifications", JsonSerializer.Serialize(request.Verifications ?? []));
            command.Parameters.AddWithValue("$artifacts", JsonSerializer.Serialize(request.Artifacts ?? []));
            command.Parameters.AddWithValue("$workflowId", (object?)request.WorkflowId ?? DBNull.Value);
            command.Parameters.AddWithValue("$agentId", (object?)request.AgentId ?? DBNull.Value);
            command.Parameters.AddWithValue("$recordedAt", DateTimeOffset.UtcNow.ToString("O"));
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        finally { _writeGate.Release(); }
    }

    public async Task<IReadOnlyList<DailyActivity>> GetDailyActivitiesAsync(
        DateTimeOffset startUtc, DateTimeOffset endUtc, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        var command = connection.CreateCommand();
        command.CommandText = "SELECT activity_id,project_id,task_id,title,summary,status,files_json,verifications_json,artifacts_json,workflow_id,agent_id,recorded_at_utc FROM daily_activities WHERE recorded_at_utc >= $start AND recorded_at_utc < $end ORDER BY recorded_at_utc DESC, rowid DESC;";
        command.Parameters.AddWithValue("$start", startUtc.ToString("O"));
        command.Parameters.AddWithValue("$end", endUtc.ToString("O"));
        var result = new List<DailyActivity>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) result.Add(ReadDailyActivity(reader));
        return result;
    }

    public async Task<DateTimeOffset?> GetLatestDailyActivityAtAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        var command = connection.CreateCommand();
        command.CommandText = "SELECT MAX(recorded_at_utc) FROM daily_activities;";
        var value = await command.ExecuteScalarAsync(cancellationToken);
        return value is string text ? DateTimeOffset.Parse(text) : null;
    }

    private static DailyActivity ReadDailyActivity(SqliteDataReader reader)
        => new(reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3),
            reader.GetString(4), reader.GetString(5), ReadList(reader.GetString(6)),
            ReadList(reader.GetString(7)), ReadList(reader.GetString(8)),
            reader.IsDBNull(9) ? null : reader.GetString(9), reader.IsDBNull(10) ? null : reader.GetString(10),
            DateTimeOffset.Parse(reader.GetString(11)));

    private static IReadOnlyList<string> ReadList(string json)
        => JsonSerializer.Deserialize<string[]>(json) ?? [];
}
