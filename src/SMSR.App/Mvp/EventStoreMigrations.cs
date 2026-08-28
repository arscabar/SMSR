using Microsoft.Data.Sqlite;

namespace SMSR.App.Mvp;

internal static class EventStoreMigrations
{
    public static async Task EnsureMetadataColumnsAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        await EnsureColumnAsync(connection, "plan_nodes", "metadata_json", cancellationToken);
        await EnsureColumnAsync(connection, "current_state", "metadata_json", cancellationToken);
    }

    private static async Task EnsureColumnAsync(SqliteConnection connection, string table, string column, CancellationToken cancellationToken)
    {
        var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA table_info({table});";
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
            while (await reader.ReadAsync(cancellationToken))
                if (string.Equals(reader.GetString(1), column, StringComparison.OrdinalIgnoreCase)) return;
        command.CommandText = $"ALTER TABLE {table} ADD COLUMN {column} TEXT NOT NULL DEFAULT '{{}}';";
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
