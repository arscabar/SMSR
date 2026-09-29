using Microsoft.Data.Sqlite;

namespace SMSR.App.Mvp;

public sealed partial class EventStore
{
    public async Task<GraphFeedback> RecordGraphFeedbackAsync(GraphFeedbackRequest request, CancellationToken ct = default)
    {
        await _writeGate.WaitAsync(ct);
        try
        {
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(ct);
            using var edge = GraphSql.Create(connection, null, """
                SELECT f.content_hash FROM graph_edges e JOIN graph_files f
                  ON f.project_id=e.project_id AND f.path=e.owner_path
                WHERE e.project_id=$p0 AND e.source_id=$p1 AND e.target_id=$p2
                  AND e.relation=$p3 AND e.owner_path=$p4 AND e.source_line=$p5;
                """, request.ProjectId, request.SourceId, request.TargetId,
                request.Relation, request.OwnerPath, request.SourceLine);
            var hash = await edge.ExecuteScalarAsync(ct) as string
                ?? throw new KeyNotFoundException("색인된 관계와 근거 파일이 없습니다.");
            var id = Guid.NewGuid().ToString("N");
            var now = DateTimeOffset.UtcNow;
            using var insert = GraphSql.Create(connection, null, """
                INSERT INTO graph_feedback(feedback_id,project_id,source_id,target_id,relation,owner_path,source_line,verdict,source_hash,created_at_utc)
                VALUES ($p0,$p1,$p2,$p3,$p4,$p5,$p6,$p7,$p8,$p9);
                """, id, request.ProjectId, request.SourceId, request.TargetId, request.Relation,
                request.OwnerPath, request.SourceLine, request.Verdict, hash, now.ToString("O"));
            await insert.ExecuteNonQueryAsync(ct);
            return new(id, request.SourceId, request.TargetId, request.Relation,
                request.OwnerPath, request.SourceLine, request.Verdict, now, false);
        }
        finally { _writeGate.Release(); }
    }

    public async Task<IReadOnlyList<GraphFeedback>> GetGraphFeedbackAsync(string projectId, string sourceId,
        CancellationToken ct = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(ct);
        using var command = GraphSql.Create(connection, null, """
            SELECT b.feedback_id,b.source_id,b.target_id,b.relation,b.owner_path,b.source_line,
                   b.verdict,b.created_at_utc,
                   f.content_hash IS NULL OR f.content_hash<>b.source_hash OR e.source_id IS NULL
            FROM graph_feedback b LEFT JOIN graph_files f ON f.project_id=b.project_id AND f.path=b.owner_path
            LEFT JOIN graph_edges e ON e.project_id=b.project_id AND e.source_id=b.source_id
              AND e.target_id=b.target_id AND e.relation=b.relation AND e.owner_path=b.owner_path AND e.source_line=b.source_line
            WHERE b.project_id=$p0 AND b.source_id=$p1 ORDER BY b.created_at_utc DESC LIMIT 100;
            """, projectId, sourceId);
        var items = new List<GraphFeedback>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct)) items.Add(new(reader.GetString(0), reader.GetString(1),
            reader.GetString(2), reader.GetString(3), reader.GetString(4), reader.GetInt32(5),
            reader.GetString(6), DateTimeOffset.Parse(reader.GetString(7)), reader.GetBoolean(8)));
        return items;
    }
}
