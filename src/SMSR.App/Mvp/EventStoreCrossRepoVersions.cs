using Microsoft.Data.Sqlite;

namespace SMSR.App.Mvp;

public sealed partial class EventStore
{
    private static async Task CheckCrossRepoVersionsAsync(SqliteConnection connection, SqliteTransaction transaction,
        IReadOnlyDictionary<string, int> expected, CancellationToken ct)
    {
        using var command = GraphSql.Create(connection, transaction, "SELECT project_id,revision FROM graph_projects;");
        await using var rows = await command.ExecuteReaderAsync(ct);
        var count = 0;
        while (await rows.ReadAsync(ct))
        {
            count++;
            if (!expected.TryGetValue(rows.GetString(0), out var revision) || rows.GetInt32(1) != revision)
                throw new InvalidOperationException("저장소 색인이 변경되어 연결 갱신을 취소했습니다. 다시 색인하세요.");
        }
        if (count != expected.Count) throw new InvalidOperationException("저장소 목록이 변경되어 연결 갱신을 취소했습니다.");
    }

    public async Task<bool> IsCrossRepoCurrentAsync(string project, int revision, CancellationToken ct = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(ct);
        using var command = GraphSql.Create(connection, null, """
            SELECT EXISTS(SELECT 1 FROM graph_derived d
            WHERE d.project_id=$p0 AND d.kind='crossrepo' AND d.item_key='' AND d.revision=$p1
              AND (SELECT count(*) FROM json_each(d.payload))=(SELECT count(*) FROM graph_projects)
              AND NOT EXISTS(SELECT 1 FROM json_each(d.payload) e LEFT JOIN graph_projects p ON p.project_id=e.key
                WHERE p.project_id IS NULL OR p.revision<>e.value));
            """, project, revision);
        return Convert.ToInt64(await command.ExecuteScalarAsync(ct)) == 1;
    }
}
