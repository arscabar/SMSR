using Microsoft.Data.Sqlite;

namespace SMSR.App.Mvp;

public sealed partial class EventStore
{
    public async Task<AgentState> RecordHeartbeatAsync(AgentHeartbeatRequest request, CancellationToken cancellationToken = default)
    {
        await _writeGate.WaitAsync(cancellationToken);
        try
        {
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            var now = DateTimeOffset.UtcNow;
            await UpsertHeartbeatAsync(connection, null, request, now.ToString("O"), cancellationToken);
            return new(request.AgentId, request.AgentRole, request.Status, request.NodeId, request.Summary, request.RetryCount, now, false);
        }
        finally { _writeGate.Release(); }
    }

}
