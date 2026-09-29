using Microsoft.Data.Sqlite;

namespace SMSR.App.Mvp;

public sealed partial class EventStore
{
    public async Task<string?> GetGraphDerivedAsync(string projectId, string kind, string key, CancellationToken ct = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(ct);
        using var command = GraphSql.Create(connection, null,
            "SELECT payload FROM graph_derived WHERE project_id=$p0 AND kind=$p1 AND item_key=$p2;", projectId, kind, key);
        return await command.ExecuteScalarAsync(ct) as string;
    }

    public async Task SaveGraphDerivedAsync(string projectId, string kind, string key, int revision,
        string payload, CancellationToken ct = default)
    {
        await _writeGate.WaitAsync(ct);
        try
        {
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(ct);
            using var command = GraphSql.Create(connection, null, """
                INSERT INTO graph_derived(project_id,kind,item_key,revision,payload,created_at_utc)
                SELECT $p0,$p1,$p2,$p3,$p4,$p5 WHERE EXISTS
                  (SELECT 1 FROM graph_projects WHERE project_id=$p0 AND revision=$p3)
                ON CONFLICT(project_id,kind,item_key) DO UPDATE SET
                  revision=excluded.revision,payload=excluded.payload,created_at_utc=excluded.created_at_utc;
                """, projectId, kind, key, revision, payload, DateTimeOffset.UtcNow.ToString("O"));
            if (await command.ExecuteNonQueryAsync(ct) != 1)
                throw new InvalidOperationException("색인이 변경됐습니다. 다시 실행하세요.");
        }
        finally { _writeGate.Release(); }
    }
}
