using Microsoft.Data.Sqlite;

namespace SMSR.App.Mvp;

internal static class GraphSql
{
    public static async Task<int> ExecuteAsync(SqliteConnection connection, SqliteTransaction transaction,
        string sql, CancellationToken ct, params object?[] values)
    {
        using var command = Create(connection, transaction, sql, values);
        return await command.ExecuteNonQueryAsync(ct);
    }

    public static async Task<long> CountAsync(SqliteConnection connection, SqliteTransaction transaction,
        string sql, CancellationToken ct, params object?[] values)
    {
        using var command = Create(connection, transaction, sql, values);
        return Convert.ToInt64(await command.ExecuteScalarAsync(ct));
    }

    public static SqliteCommand Create(SqliteConnection connection, SqliteTransaction? transaction,
        string sql, params object?[] values)
    {
        var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        for (var index = 0; index < values.Length; index++)
            command.Parameters.AddWithValue("$p" + index, values[index] ?? DBNull.Value);
        return command;
    }
}
