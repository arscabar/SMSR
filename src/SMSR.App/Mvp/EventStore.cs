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
            CREATE TABLE IF NOT EXISTS plan_nodes (
              project_id TEXT NOT NULL, workflow_id TEXT NOT NULL, node_id TEXT NOT NULL,
              title TEXT NOT NULL, weight INTEGER NOT NULL, depends_on_json TEXT NOT NULL,
              PRIMARY KEY(project_id, workflow_id, node_id));
            CREATE TABLE IF NOT EXISTS current_state (
              project_id TEXT NOT NULL, workflow_id TEXT NOT NULL, node_id TEXT NOT NULL,
              agent_id TEXT NOT NULL, status TEXT NOT NULL, summary TEXT, error TEXT, updated_at_utc TEXT NOT NULL,
              PRIMARY KEY(project_id, workflow_id, node_id));
            INSERT INTO current_state(project_id, workflow_id, node_id, agent_id, status, summary, error, updated_at_utc)
            SELECT project_id, workflow_id, node_id, agent_id, status, summary, error, created_at_utc FROM (
              SELECT *, ROW_NUMBER() OVER (PARTITION BY project_id, workflow_id, node_id ORDER BY created_at_utc DESC, rowid DESC) AS position FROM events)
            WHERE position = 1 AND NOT EXISTS (SELECT 1 FROM current_state);
            CREATE TABLE IF NOT EXISTS summaries (
              id INTEGER PRIMARY KEY, project_id TEXT NOT NULL, workflow_id TEXT NOT NULL,
              source_last_event_id TEXT, content TEXT NOT NULL, created_at_utc TEXT NOT NULL);
            CREATE INDEX IF NOT EXISTS ix_summaries_workflow ON summaries(project_id, workflow_id, created_at_utc);
            """;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
