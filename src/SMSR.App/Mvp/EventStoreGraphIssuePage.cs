using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace SMSR.App.Mvp;

public sealed record GraphIssuePage(GraphIssue[] Items, int Total, int Offset, int Revision);

public sealed partial class EventStore
{
    public async Task<GraphIssuePage> GraphIssuesAsync(string projectId, string? ownerPath = null,
        int offset = 0, CancellationToken ct = default)
    {
        if (EventValidation.ValidateWorkflowIds(projectId, "graph") is { } error
            || ownerPath?.Length > 1024 || offset is < 0 or > 1_000_000)
            throw new ArgumentException("진단 조회 범위가 올바르지 않습니다.");
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(ct);
        using var transaction = connection.BeginTransaction(deferred: true);
        var info = await ReadGraphInfoAsync(connection, transaction, projectId, ct)
            ?? throw new KeyNotFoundException("관계 색인이 없습니다.");
        const string where = "project_id=$p0 AND ($p1 IS NULL OR owner_path=$p1)";
        using var count = GraphSql.Create(connection, transaction, "SELECT COUNT(*) FROM graph_issues WHERE " + where, projectId, ownerPath);
        var total = Convert.ToInt32(await count.ExecuteScalarAsync(ct));
        using var command = GraphSql.Create(connection, transaction, $"""
            SELECT owner_path,source_line,relation,reason,candidates_json FROM graph_issues
            WHERE {where} ORDER BY owner_path,source_line,relation,reason LIMIT 20 OFFSET $p2;
            """, projectId, ownerPath, offset);
        var items = new List<GraphIssue>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct)) items.Add(new(reader.GetString(0), reader.GetInt32(1), reader.GetString(2),
            reader.GetString(3), JsonSerializer.Deserialize<string[]>(reader.GetString(4)) ?? []));
        return new(items.ToArray(), total, offset, info.Revision);
    }
}
