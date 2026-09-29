using Microsoft.Data.Sqlite;

namespace SMSR.App.Mvp;

internal static class GraphFormatMigration
{
    public static async Task BackupAsync(SqliteConnection connection, string path, CancellationToken ct)
    {
        using var check = connection.CreateCommand();
        check.CommandText = "SELECT 1 FROM sqlite_master WHERE type='table' AND name='graph_index_formats';";
        if (await check.ExecuteScalarAsync(ct) is not null) return;
        await using var backup = new SqliteConnection(new SqliteConnectionStringBuilder {
            DataSource = path + ".bak-graph-format-" + DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmssfff"), Pooling = false }.ToString());
        await backup.OpenAsync(ct);
        connection.BackupDatabase(backup);
    }
}
