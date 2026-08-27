using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace SMSR.App.Mvp;

public sealed partial class EventStore(string databasePath)
{
    private readonly string _connectionString = new SqliteConnectionStringBuilder { DataSource = databasePath, Pooling = false, DefaultTimeout = 5 }.ToString();
    // ponytail: one SQLite writer per local store; replace with bounded retries if measured throughput requires it.
    private readonly SemaphoreSlim _writeGate = new(1, 1);

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        var command = connection.CreateCommand();
        command.CommandText = """
            PRAGMA journal_mode=WAL;
            CREATE TABLE IF NOT EXISTS events (
              event_id TEXT PRIMARY KEY, project_id TEXT NOT NULL, workflow_id TEXT NOT NULL,
              node_id TEXT NOT NULL, agent_id TEXT NOT NULL, event_type TEXT NOT NULL,
              status TEXT NOT NULL, summary TEXT, error TEXT, payload_json TEXT NOT NULL,
              created_at_utc TEXT NOT NULL);
            CREATE INDEX IF NOT EXISTS ix_events_workflow_node ON events(project_id, workflow_id, node_id, created_at_utc);
            CREATE TABLE IF NOT EXISTS summaries (
              id INTEGER PRIMARY KEY, project_id TEXT NOT NULL, workflow_id TEXT NOT NULL,
              source_last_event_id TEXT, content TEXT NOT NULL, created_at_utc TEXT NOT NULL);
            CREATE INDEX IF NOT EXISTS ix_summaries_workflow ON summaries(project_id, workflow_id, created_at_utc);
            """;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<bool> RecordAsync(RecordEventRequest request, CancellationToken cancellationToken = default)
    {
        await _writeGate.WaitAsync(cancellationToken);
        try
        {
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            var command = connection.CreateCommand();
            command.CommandText = """
                INSERT INTO events VALUES ($eventId, $projectId, $workflowId, $nodeId, $agentId, $eventType,
                $status, $summary, $error, $payload, $createdAt) ON CONFLICT(event_id) DO NOTHING;
                """;
            var values = new Dictionary<string, object?>
            {
                ["$eventId"] = request.EventId, ["$projectId"] = request.ProjectId, ["$workflowId"] = request.WorkflowId,
                ["$nodeId"] = request.NodeId, ["$agentId"] = request.AgentId, ["$eventType"] = request.EventType,
                ["$status"] = request.Status, ["$summary"] = request.Summary, ["$error"] = request.Error,
                ["$payload"] = JsonSerializer.Serialize(request), ["$createdAt"] = DateTimeOffset.UtcNow.ToString("O")
            };
            foreach (var (name, value) in values) command.Parameters.AddWithValue(name, value ?? DBNull.Value);
            return await command.ExecuteNonQueryAsync(cancellationToken) == 1;
        }
        finally { _writeGate.Release(); }
    }

}
