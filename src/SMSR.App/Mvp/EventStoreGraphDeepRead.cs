using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace SMSR.App.Mvp;

public sealed record GraphDeepStatus(string Analyzer, string AnalysisKey, string Status, int Applied, int Excluded);

public sealed partial class EventStore
{
    private static async Task<GraphDeepFact[]> ReadDeepProjectionAsync(SqliteConnection connection,
        SqliteTransaction transaction, string projectId, int revision,
        Dictionary<string, string> files, JsonElement context, CancellationToken ct)
    {
        var nodes = new List<GraphNode>();
        using (var query = GraphSql.Create(connection, transaction, """
            SELECT node_id,owner_path,kind,label,source_path,source_line,content_hash,details_json
            FROM graph_nodes WHERE project_id=$p0 AND kind='symbol';
            """, projectId))
        await using (var rows = await query.ExecuteReaderAsync(ct))
            while (await rows.ReadAsync(ct)) nodes.Add(ReadNode(rows));
        var reports = new List<(string Kind, string Key, string Payload)>();
        using (var query = GraphSql.Create(connection, transaction, """
            SELECT kind,item_key,payload FROM graph_derived WHERE project_id=$p0
            AND kind IN ('analysis','csharp','java','typescript','jdt') ORDER BY kind,item_key;
            """, projectId))
        await using (var rows = await query.ExecuteReaderAsync(ct))
            while (await rows.ReadAsync(ct)) reports.Add((rows.GetString(0), rows.GetString(1), rows.GetString(2)));
        var map = new GraphDeepMapping(nodes);
        var facts = new List<GraphDeepFact>();
        await GraphSql.ExecuteAsync(connection, transaction, "DELETE FROM graph_deep_status WHERE project_id=$p0;", ct, projectId);
        foreach (var report in reports)
        {
            ct.ThrowIfCancellationRequested();
            facts.AddRange(await ProjectDeepReportAsync(connection, transaction, projectId, revision,
                files, context, map, report.Kind, report.Key, report.Payload, ct));
        }
        return facts.ToArray();
    }

    public async Task<IReadOnlyList<GraphDeepStatus>> GetGraphDeepStatusAsync(string projectId, CancellationToken ct = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(ct);
        using var query = GraphSql.Create(connection, null,
            "SELECT kind,item_key,status,applied,excluded FROM graph_deep_status WHERE project_id=$p0 ORDER BY kind,item_key;", projectId);
        await using var rows = await query.ExecuteReaderAsync(ct);
        var results = new List<GraphDeepStatus>();
        while (await rows.ReadAsync(ct)) results.Add(new(rows.GetString(0), rows.GetString(1), rows.GetString(2), rows.GetInt32(3), rows.GetInt32(4)));
        return results;
    }
}
