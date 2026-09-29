using Microsoft.Data.Sqlite;

namespace SMSR.App.Mvp;

internal static class EventStoreGraphSchema
{
    public static async Task BackupBeforeP1Async(SqliteConnection connection, string path, CancellationToken ct)
    {
        var check = connection.CreateCommand();
        check.CommandText = "SELECT 1 FROM pragma_table_info('graph_issues') WHERE name='candidates_json';";
        if (await check.ExecuteScalarAsync(ct) is not null) return;
        var backupPath = path + ".bak-p1-" + DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmssfff");
        await using var backup = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = backupPath, Pooling = false }.ToString());
        await backup.OpenAsync(ct);
        connection.BackupDatabase(backup);
    }

    public static async Task CreateAsync(SqliteConnection connection, CancellationToken ct)
    {
        var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS graph_projects (
              project_id TEXT PRIMARY KEY, root_path TEXT NOT NULL,
              revision INTEGER NOT NULL, indexed_at_utc TEXT NOT NULL,
              scope_json TEXT NOT NULL DEFAULT '[]');
            CREATE TABLE IF NOT EXISTS graph_derived (
              project_id TEXT NOT NULL, kind TEXT NOT NULL, item_key TEXT NOT NULL,
              revision INTEGER NOT NULL, payload TEXT NOT NULL, created_at_utc TEXT NOT NULL,
              PRIMARY KEY(project_id,kind,item_key));
            CREATE TABLE IF NOT EXISTS graph_files (
              project_id TEXT NOT NULL, path TEXT NOT NULL, content_hash TEXT NOT NULL,
              kind TEXT NOT NULL, PRIMARY KEY(project_id,path));
            CREATE TABLE IF NOT EXISTS graph_index_formats (
              project_id TEXT PRIMARY KEY, format TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS graph_nodes (
              project_id TEXT NOT NULL, node_id TEXT NOT NULL, owner_path TEXT NOT NULL,
              kind TEXT NOT NULL, label TEXT NOT NULL, source_path TEXT NOT NULL,
              source_line INTEGER NOT NULL, content_hash TEXT NOT NULL,
              PRIMARY KEY(project_id,node_id));
            CREATE INDEX IF NOT EXISTS ix_graph_nodes_owner ON graph_nodes(project_id,owner_path);
            CREATE INDEX IF NOT EXISTS ix_graph_nodes_label ON graph_nodes(project_id,label);
            CREATE TABLE IF NOT EXISTS graph_edges (
              project_id TEXT NOT NULL, source_id TEXT NOT NULL, target_id TEXT NOT NULL,
              relation TEXT NOT NULL, owner_path TEXT NOT NULL, source_line INTEGER NOT NULL,
              resolution TEXT NOT NULL, confidence TEXT NOT NULL,
              PRIMARY KEY(project_id,source_id,target_id,relation,owner_path,source_line),
              FOREIGN KEY(project_id,source_id) REFERENCES graph_nodes(project_id,node_id) ON DELETE CASCADE,
              FOREIGN KEY(project_id,target_id) REFERENCES graph_nodes(project_id,node_id) ON DELETE CASCADE);
            CREATE INDEX IF NOT EXISTS ix_graph_edges_owner ON graph_edges(project_id,owner_path);
            CREATE INDEX IF NOT EXISTS ix_graph_edges_target ON graph_edges(project_id,target_id);
            CREATE TABLE IF NOT EXISTS graph_issues (
              project_id TEXT NOT NULL, owner_path TEXT NOT NULL, source_line INTEGER NOT NULL,
              relation TEXT NOT NULL, reason TEXT NOT NULL, candidates_json TEXT NOT NULL DEFAULT '[]',
              PRIMARY KEY(project_id,owner_path,source_line,relation,reason));
            CREATE TABLE IF NOT EXISTS graph_revision_nodes (
              project_id TEXT NOT NULL, revision INTEGER NOT NULL, node_id TEXT NOT NULL,
              owner_path TEXT NOT NULL, kind TEXT NOT NULL, label TEXT NOT NULL,
              source_path TEXT NOT NULL, source_line INTEGER NOT NULL, content_hash TEXT NOT NULL,
              PRIMARY KEY(project_id,revision,node_id));
            CREATE TABLE IF NOT EXISTS graph_revision_edges (
              project_id TEXT NOT NULL, revision INTEGER NOT NULL,
              source_id TEXT NOT NULL, target_id TEXT NOT NULL, relation TEXT NOT NULL,
              owner_path TEXT NOT NULL, source_line INTEGER NOT NULL,
              resolution TEXT NOT NULL, confidence TEXT NOT NULL,
              PRIMARY KEY(project_id,revision,source_id,target_id,relation,owner_path,source_line));
            CREATE INDEX IF NOT EXISTS ix_graph_revision_edges_target ON graph_revision_edges(project_id,revision,target_id);
            CREATE TABLE IF NOT EXISTS graph_feedback (
              feedback_id TEXT PRIMARY KEY, project_id TEXT NOT NULL,
              source_id TEXT NOT NULL, target_id TEXT NOT NULL, relation TEXT NOT NULL,
              owner_path TEXT NOT NULL, source_line INTEGER NOT NULL,
              verdict TEXT NOT NULL, source_hash TEXT NOT NULL, created_at_utc TEXT NOT NULL);
            CREATE INDEX IF NOT EXISTS ix_graph_feedback_source ON graph_feedback(project_id,source_id,created_at_utc DESC);
            CREATE TABLE IF NOT EXISTS graph_cross_repo_edges (
              source_project_id TEXT NOT NULL, source_id TEXT NOT NULL,
              target_project_id TEXT NOT NULL, target_id TEXT NOT NULL,
              owner_path TEXT NOT NULL, source_line INTEGER NOT NULL,
              PRIMARY KEY(source_project_id,source_id,target_project_id,target_id,owner_path,source_line));
            CREATE INDEX IF NOT EXISTS ix_graph_cross_target ON graph_cross_repo_edges(target_project_id,target_id);
            """;
        await command.ExecuteNonQueryAsync(ct);
        command.CommandText = "SELECT 1 FROM pragma_table_info('graph_projects') WHERE name='scope_json';";
        if (await command.ExecuteScalarAsync(ct) is null)
        {
            command.CommandText = "ALTER TABLE graph_projects ADD COLUMN scope_json TEXT NOT NULL DEFAULT '[]';";
            await command.ExecuteNonQueryAsync(ct);
        }
        command.CommandText = "SELECT 1 FROM pragma_table_info('graph_issues') WHERE name='candidates_json';";
        if (await command.ExecuteScalarAsync(ct) is null)
        {
            command.CommandText = "ALTER TABLE graph_issues ADD COLUMN candidates_json TEXT NOT NULL DEFAULT '[]';";
            await command.ExecuteNonQueryAsync(ct);
        }
        command.CommandText = """
            INSERT OR IGNORE INTO graph_revision_nodes
            SELECT n.project_id,p.revision,n.node_id,n.owner_path,n.kind,n.label,n.source_path,n.source_line,n.content_hash
            FROM graph_nodes n JOIN graph_projects p ON p.project_id=n.project_id;
            INSERT OR IGNORE INTO graph_revision_edges
            SELECT e.project_id,p.revision,e.source_id,e.target_id,e.relation,e.owner_path,e.source_line,e.resolution,e.confidence
            FROM graph_edges e JOIN graph_projects p ON p.project_id=e.project_id;
            """;
        await command.ExecuteNonQueryAsync(ct);
    }

    public static async Task BackupBeforeP2Async(SqliteConnection connection, string path, CancellationToken ct)
    {
        using var check = connection.CreateCommand();
        check.CommandText = "SELECT 1 FROM sqlite_master WHERE type='table' AND name='graph_revision_nodes';";
        if (await check.ExecuteScalarAsync(ct) is not null) return;
        var backupPath = path + ".bak-p2-" + DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmssfff");
        await using var backup = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = backupPath, Pooling = false }.ToString());
        await backup.OpenAsync(ct);
        connection.BackupDatabase(backup);
    }

    public static async Task BackupBeforeCrossRepoAsync(SqliteConnection connection, string path, CancellationToken ct)
    {
        using var check = connection.CreateCommand();
        check.CommandText = "SELECT 1 FROM sqlite_master WHERE type='table' AND name='graph_cross_repo_edges';";
        if (await check.ExecuteScalarAsync(ct) is not null) return;
        var backupPath = path + ".bak-crossrepo-" + DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmssfff");
        await using var backup = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = backupPath, Pooling = false }.ToString());
        await backup.OpenAsync(ct);
        connection.BackupDatabase(backup);
    }
}
