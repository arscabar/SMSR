using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace SMSR.App.Mvp;

internal static class GraphHyperedgeIntegrity
{
    internal static async Task CheckAsync(SqliteConnection connection, SqliteTransaction transaction,
        string projectId, GraphScan scan, CancellationToken ct)
    {
        using (var owners = GraphSql.Create(connection, transaction, """
            SELECT 1 FROM graph_nodes n WHERE n.project_id=$p0 AND json_extract(n.details_json,'$.ownerNodeId') IS NOT NULL
              AND NOT EXISTS(SELECT 1 FROM graph_nodes p WHERE p.project_id=n.project_id
                AND p.node_id=json_extract(n.details_json,'$.ownerNodeId')) LIMIT 1;
            """, projectId))
            if (await owners.ExecuteScalarAsync(ct) is not null)
                throw new ArgumentException("심벌의 소유 노드가 색인에 없습니다.");
        using var query = GraphSql.Create(connection, transaction, """
            SELECT h.hyperedge_id FROM graph_hyperedges h WHERE h.project_id=$p0 AND (
              NOT EXISTS(SELECT 1 FROM graph_files f WHERE f.project_id=h.project_id AND f.path=h.owner_path
                AND upper(f.content_hash)=upper(json_extract(h.payload_json,'$.evidence.sourceHash')))
              OR EXISTS(SELECT 1 FROM json_each(h.payload_json,'$.members') m WHERE NOT EXISTS(
                SELECT 1 FROM graph_nodes n WHERE n.project_id=h.project_id AND n.node_id=json_extract(m.value,'$.nodeId'))));
            """, projectId);
        var missing = new List<string>();
        await using (var rows = await query.ExecuteReaderAsync(ct))
            while (await rows.ReadAsync(ct)) missing.Add(rows.GetString(0));
        if ((scan.Hyperedges ?? []).Any(h => missing.Contains(h.HyperedgeId)))
            throw new ArgumentException("다중 참여 관계에 색인되지 않은 참여자가 있습니다.");
        foreach (var id in missing)
            await GraphSql.ExecuteAsync(connection, transaction,
                "DELETE FROM graph_hyperedges WHERE project_id=$p0 AND hyperedge_id=$p1;", ct, projectId, id);
        // An existing member whose source changed invalidates the entire old group.
        foreach (var path in scan.ChangedPaths.Concat(scan.ReparsedDocs).Distinct())
            await GraphSql.ExecuteAsync(connection, transaction, """
                DELETE FROM graph_hyperedges WHERE project_id=$p0 AND (hyperedge_id NOT LIKE 'semantic:%' OR $p3) AND hyperedge_id NOT IN (
                  SELECT json_extract(value,'$.hyperedgeId') FROM json_each($p2)) AND EXISTS (
                  SELECT 1 FROM json_each(payload_json,'$.members') m JOIN graph_nodes n
                    ON n.project_id=$p0 AND n.node_id=json_extract(m.value,'$.nodeId') WHERE n.owner_path=$p1);
                """, ct, projectId, path, JsonSerializer.Serialize(scan.Hyperedges ?? [], GraphWorker.Json), scan.ChangedPaths.Contains(path));
    }
}
