using Microsoft.Data.Sqlite;

namespace SMSR.App.Mvp;

internal static class GraphKnowledgeSchema
{
    internal static async Task EnsureAsync(SqliteConnection connection, CancellationToken ct)
    {
        using var check = connection.CreateCommand();
        check.CommandText = "SELECT 1 FROM pragma_table_info('graph_nodes') WHERE name='details_json';";
        var migrate = await check.ExecuteScalarAsync(ct) is null;
        check.CommandText = "SELECT 1 FROM graph_projects LIMIT 1;";
        if (migrate && await check.ExecuteScalarAsync(ct) is not null)
        {
            await using var backup = new SqliteConnection(new SqliteConnectionStringBuilder {
                DataSource = connection.DataSource + ".bak-knowledge-" + DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmssfff"),
                Pooling = false }.ToString());
            await backup.OpenAsync(ct);
            connection.BackupDatabase(backup);
        }
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(ct);
        foreach (var (table, column) in new[] { ("graph_nodes", "details_json"),
            ("graph_revision_nodes", "details_json"), ("graph_edges", "evidence_json"),
            ("graph_revision_edges", "evidence_json") })
        {
            using var command = GraphSql.Create(connection, transaction,
                $"SELECT 1 FROM pragma_table_info('{table}') WHERE name='{column}';");
            if (await command.ExecuteScalarAsync(ct) is null)
                await GraphSql.ExecuteAsync(connection, transaction,
                    $"ALTER TABLE {table} ADD COLUMN {column} TEXT;", ct);
        }
        await GraphSql.ExecuteAsync(connection, transaction, """
            CREATE INDEX IF NOT EXISTS ix_graph_revision_nodes_owner ON graph_revision_nodes(project_id,revision,owner_path,kind);
            CREATE TABLE IF NOT EXISTS graph_hyperedges (
              project_id TEXT NOT NULL, hyperedge_id TEXT NOT NULL, owner_path TEXT NOT NULL,
              payload_json TEXT NOT NULL, PRIMARY KEY(project_id,hyperedge_id));
            CREATE TABLE IF NOT EXISTS graph_revision_hyperedges (
              project_id TEXT NOT NULL, revision INTEGER NOT NULL, hyperedge_id TEXT NOT NULL,
              owner_path TEXT NOT NULL, payload_json TEXT NOT NULL,
              PRIMARY KEY(project_id,revision,hyperedge_id));
            """, ct);
        await transaction.CommitAsync(ct);
    }
}
