using Microsoft.Data.Sqlite;

namespace SMSR.App.Mvp;

internal static partial class EventStoreMigrations
{
    public static async Task BackupBeforeP0Async(SqliteConnection connection, string databasePath, CancellationToken ct)
    {
        var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name IN ('plan_revisions', 'plan_revision_nodes', 'workflow_evidence_links', 'schema_migrations');";
        if (Convert.ToInt32(await command.ExecuteScalarAsync(ct)) == 4) return;
        var backupPath = databasePath + ".bak-p0-" + DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmssfff");
        await using var backup = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = backupPath, Pooling = false }.ToString());
        await backup.OpenAsync(ct);
        connection.BackupDatabase(backup);
    }

    public static async Task BackfillEvidenceAsync(SqliteConnection connection, CancellationToken ct)
    {
        var command = connection.CreateCommand();
        command.CommandText = "SELECT 1 FROM schema_migrations WHERE name='evidence-v1';";
        if (await command.ExecuteScalarAsync(ct) is not null) return;
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(ct);
        command.Transaction = transaction;
        command.CommandText = """
            INSERT OR IGNORE INTO workflow_evidence_links(project_id, workflow_id, node_id, event_id, reference, created_at_utc)
            SELECT e.project_id, e.workflow_id, e.node_id, e.event_id, artifact.value, e.created_at_utc
            FROM events e, json_each(CASE WHEN json_valid(e.payload_json) THEN e.payload_json ELSE '{}' END, '$.Artifacts') artifact
            WHERE artifact.type='text' AND trim(artifact.value)<>'';
            INSERT INTO schema_migrations(name) VALUES ('evidence-v1');
            """;
        await command.ExecuteNonQueryAsync(ct);
        await transaction.CommitAsync(ct);
    }
}
