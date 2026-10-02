using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Data.Sqlite;

namespace SMSR.App.Mvp;

public sealed partial class EventStore
{
    private static string StampDeepContext(string payload, string kind, string key, JsonElement context)
    {
        var report = JsonNode.Parse(payload)!.AsObject();
        var metadata = JsonNode.Parse(context.GetRawText())!.AsObject();
        metadata["kind"] = kind;
        metadata["analysisKey"] = key;
        report["graphContext"] = metadata;
        return report.ToJsonString();
    }
    private static async Task<(Dictionary<string, string> Files, JsonElement Context)> ReadDeepInputsAsync(
        SqliteConnection connection, SqliteTransaction? transaction, string projectId, CancellationToken ct)
    {
        var files = new Dictionary<string, string>(StringComparer.Ordinal);
        using (var query = GraphSql.Create(connection, transaction,
            "SELECT path,content_hash FROM graph_files WHERE project_id=$p0 ORDER BY path;", projectId))
        await using (var rows = await query.ExecuteReaderAsync(ct))
            while (await rows.ReadAsync(ct)) files.Add(rows.GetString(0), rows.GetString(1));
        string root = "", scope = "";
        using (var query = GraphSql.Create(connection, transaction,
            "SELECT root_path,scope_json FROM graph_projects WHERE project_id=$p0;", projectId))
        await using (var rows = await query.ExecuteReaderAsync(ct))
            if (await rows.ReadAsync(ct)) { root = rows.GetString(0); scope = rows.GetString(1); }
        var configuration = files.Where(p => GraphCodeIndexInput.IsConfiguration(p.Key)).ToDictionary(p => p.Key, p => p.Value);
        return (files, JsonSerializer.SerializeToElement(new { root, scope, configuration }));
    }

    internal async Task<bool> IsGraphDeepReportCurrentAsync(string projectId, string kind, string key,
        JsonElement report, CancellationToken ct)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(ct);
        using var transaction = connection.BeginTransaction(deferred: true);
        var info = await ReadGraphInfoAsync(connection, transaction, projectId, ct);
        var inputs = await ReadDeepInputsAsync(connection, transaction, projectId, ct);
        return info is not null && GraphDeepValidity.Invalid(kind, key, report,
            info.Revision, inputs.Files, inputs.Context) is null;
    }
}
