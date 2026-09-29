using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace SMSR.App.Mvp;

public sealed partial class EventStore
{
    public async Task<IReadOnlyList<GraphEdge>> GetGraphAdjacentAsync(string projectId,
        IReadOnlyCollection<string> ids, bool incoming, int limit = 5001, CancellationToken ct = default, int? revision = null)
    {
        if (ids.Count == 0) return [];
        if (ids.Count > 500) throw new ArgumentOutOfRangeException(nameof(ids));
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(ct);
        var key = incoming ? "target_id" : "source_id";
        var placeholders = string.Join(",", Enumerable.Range(1, ids.Count).Select(index => "$p" + index));
        var table = revision.HasValue ? "graph_revision_edges" : "graph_edges";
        var version = revision.HasValue ? "AND revision=$revision" : "";
        using var command = GraphSql.Create(connection, null, $"""
            SELECT source_id,target_id,relation,owner_path,source_line,resolution,confidence
            FROM {table} WHERE project_id=$p0 {version} AND {key} IN ({placeholders}) LIMIT $p{ids.Count + 1};
            """, [projectId, .. ids.Cast<object>(), limit]);
        if (revision.HasValue) command.Parameters.AddWithValue("$revision", revision.Value);
        var edges = new List<GraphEdge>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct)) edges.Add(ReadEdge(reader));
        return edges;
    }

    public async Task<GraphHealth?> GetGraphHealthAsync(string projectId, CancellationToken ct = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(ct);
        using var transaction = connection.BeginTransaction(deferred: true);
        var info = await ReadGraphInfoAsync(connection, transaction, projectId, ct);
        if (info is null) return null;
        using var command = GraphSql.Create(connection, transaction, """
            SELECT (SELECT COUNT(*) FROM graph_edges e LEFT JOIN graph_nodes s ON s.project_id=e.project_id AND s.node_id=e.source_id
                    LEFT JOIN graph_nodes t ON t.project_id=e.project_id AND t.node_id=e.target_id
                    WHERE e.project_id=$p0 AND (s.node_id IS NULL OR t.node_id IS NULL)),
                   (SELECT COUNT(*) FROM graph_edges WHERE project_id=$p0 AND source_id=target_id)
                     + (SELECT COUNT(*) FROM graph_issues WHERE project_id=$p0 AND reason='자기 파일 참조'),
                   (SELECT COUNT(*) FROM graph_issues WHERE project_id=$p0 AND reason='동일 위치 중복 참조'),
                   (SELECT COUNT(*) FROM graph_issues WHERE project_id=$p0);
            """, projectId);
        await using var reader = await command.ExecuteReaderAsync(ct);
        await reader.ReadAsync(ct);
        var dangling = reader.GetInt32(0);
        var selfLoops = reader.GetInt32(1);
        var duplicates = reader.GetInt32(2);
        var issueCount = reader.GetInt32(3);
        await reader.DisposeAsync();
        var issues = new List<GraphIssue>();
        using var list = GraphSql.Create(connection, transaction, """
            SELECT owner_path,source_line,relation,reason,candidates_json FROM graph_issues
            WHERE project_id=$p0 ORDER BY owner_path,source_line LIMIT 100;
            """, projectId);
        await using var rows = await list.ExecuteReaderAsync(ct);
        while (await rows.ReadAsync(ct))
            issues.Add(new(rows.GetString(0), rows.GetInt32(1), rows.GetString(2), rows.GetString(3),
                JsonSerializer.Deserialize<string[]>(rows.GetString(4)) ?? []));
        return new(info, dangling, selfLoops, duplicates, issueCount, issues);
    }

    private static GraphEdge ReadEdge(SqliteDataReader reader)
        => new(reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3),
            reader.GetInt32(4), reader.GetString(5), reader.GetString(6));
}
