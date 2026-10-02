using Microsoft.Data.Sqlite;

namespace SMSR.App.Mvp;

public sealed partial class EventStore
{
    public async Task<int> RefreshGraphDeepAsync(string projectId, int expectedRevision, CancellationToken ct = default)
    {
        await _writeGate.WaitAsync(ct);
        try
        {
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(ct);
            await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(ct);
            var revision = await RefreshGraphDeepTransactionAsync(connection, transaction, projectId, expectedRevision, ct);
            await transaction.CommitAsync(ct);
            return revision;
        }
        finally { _writeGate.Release(); }
    }
}
