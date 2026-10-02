using System.IO;
using Microsoft.Data.Sqlite;

namespace SMSR.App.Mvp;

internal static class GraphKnowledgeMigrationSelfCheck
{
    internal static async Task RunAsync(string root)
    {
        var path = Path.Combine(root, "legacy.db");
        await using (var connection = new SqliteConnection("Data Source=" + path))
        {
            await connection.OpenAsync();
            using var command = connection.CreateCommand();
            command.CommandText = """
                CREATE TABLE graph_projects(project_id TEXT PRIMARY KEY,root_path TEXT NOT NULL,revision INTEGER NOT NULL,indexed_at_utc TEXT NOT NULL);
                INSERT INTO graph_projects VALUES('old','legacy',1,'2026-09-30T00:00:00Z');
                CREATE TABLE graph_nodes(project_id TEXT NOT NULL,node_id TEXT NOT NULL,owner_path TEXT NOT NULL,kind TEXT NOT NULL,label TEXT NOT NULL,
                  source_path TEXT NOT NULL,source_line INTEGER NOT NULL,content_hash TEXT NOT NULL,PRIMARY KEY(project_id,node_id));
                INSERT INTO graph_nodes VALUES('old','file:old.cs','old.cs','code','old.cs','old.cs',1,'legacy-hash');
                """;
            await command.ExecuteNonQueryAsync();
        }
        var store = new EventStore(path);
        await store.InitializeAsync();
        if ((await store.GetGraphNodeAsync("old", "file:old.cs", revision: 1))?.Details is not null
            || (await new GraphQueryService(store).CoverageAsync("old")).MetadataUpgradeRequired != true)
            throw new Exception("Legacy metadata fabricated");
        var backups = Directory.GetFiles(root, "legacy.db.bak-knowledge-*");
        if (backups.Length != 1) throw new Exception("Knowledge migration backup missing");
        await store.InitializeAsync();
        if (Directory.GetFiles(root, "legacy.db.bak-knowledge-*").Length != 1)
            throw new Exception("Migration not idempotent");
        var restored = Path.Combine(root, "restored.db");
        File.Copy(backups[0], restored);
        var restoredStore = new EventStore(restored);
        await restoredStore.InitializeAsync();
        if ((await restoredStore.GetGraphNodeAsync("old", "file:old.cs"))?.Hash != "legacy-hash")
            throw new Exception("Backup restore lost original data");
    }
}
