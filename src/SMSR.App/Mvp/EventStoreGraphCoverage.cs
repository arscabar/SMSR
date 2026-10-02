using Microsoft.Data.Sqlite;

namespace SMSR.App.Mvp;

public sealed partial class EventStore
{
    internal async Task<GraphCoverage> GraphCoverageAsync(string projectId, int revision,
        int offset, int limit, CancellationToken ct)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(ct);
        using var totals = GraphSql.Create(connection, null, """
            SELECT COUNT(CASE WHEN node_id LIKE 'file:%' THEN 1 END),
              COUNT(CASE WHEN kind='symbol' AND details_json IS NOT NULL AND json_extract(details_json,'$.entityKind')<>'unknown' THEN 1 END),
              COUNT(CASE WHEN kind='symbol' AND (details_json IS NULL OR json_extract(details_json,'$.entityKind')='unknown') THEN 1 END),
              (SELECT COUNT(*) FROM graph_effective_edges WHERE project_id=$p0 AND revision=$p1
                AND evidence_json IS NULL AND provenance_json IS NULL)
            FROM graph_revision_nodes WHERE project_id=$p0 AND revision=$p1;
            """, projectId, revision);
        await using var row = await totals.ExecuteReaderAsync(ct);
        await row.ReadAsync(ct);
        var files = row.GetInt32(0); var detailed = row.GetInt32(1);
        var unknown = row.GetInt32(2); var noEvidence = row.GetInt32(3);
        await row.DisposeAsync();
        using var query = GraphSql.Create(connection, null, """
            WITH files AS (SELECT * FROM graph_revision_nodes WHERE project_id=$p0 AND revision=$p1
              AND node_id LIKE 'file:%' ORDER BY source_path LIMIT $p2 OFFSET $p3)
            SELECT f.source_path,f.kind,COUNT(s.node_id),
              COUNT(CASE WHEN json_extract(s.details_json,'$.entityKind')<>'unknown' THEN 1 END)
            FROM files f LEFT JOIN graph_revision_nodes s ON s.project_id=f.project_id
              AND s.revision=f.revision AND s.owner_path=f.owner_path AND s.kind='symbol'
            GROUP BY f.node_id ORDER BY f.source_path;
            """, projectId, revision, limit + 1, offset);
        var items = new List<GraphCoverageFile>();
        await using var rows = await query.ExecuteReaderAsync(ct);
        while (await rows.ReadAsync(ct))
        {
            var path = rows.GetString(0); var kind = rows.GetString(1);
            var symbols = rows.GetInt32(2); var details = rows.GetInt32(3);
            var status = details > 0 ? "DETAILS_AVAILABLE" : kind != "code" ? "FILE_ONLY"
                : GraphCodeIndexInput.Supported(path) ? "NO_SYMBOLS_OR_LEGACY" : "STRUCTURE_UNSUPPORTED";
            items.Add(new(path, kind, symbols, details, status));
        }
        return new(revision, files, detailed, unknown, noEvidence,
            !await IsGraphFormatCurrentAsync(projectId, ct), items.Take(limit).ToArray(), items.Count > limit);
    }
}
