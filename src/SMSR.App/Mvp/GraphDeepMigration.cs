using Microsoft.Data.Sqlite;

namespace SMSR.App.Mvp;

internal static class GraphDeepMigration
{
    internal static async Task BackupAsync(SqliteConnection connection, CancellationToken ct)
    {
        using var check = connection.CreateCommand();
        check.CommandText = "SELECT 1 FROM sqlite_master WHERE type='table' AND name='graph_deep_edges';";
        if (await check.ExecuteScalarAsync(ct) is not null) return;
        check.CommandText = "SELECT 1 FROM graph_projects LIMIT 1;";
        if (await check.ExecuteScalarAsync(ct) is null) return;
        await using var backup = new SqliteConnection(new SqliteConnectionStringBuilder {
            DataSource = connection.DataSource + ".bak-deep-" + DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmssfff"),
            Pooling = false }.ToString());
        await backup.OpenAsync(ct);
        connection.BackupDatabase(backup);
    }
}
