using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace SMSR.App.Mvp;

public sealed partial class EventStore
{
    private static async Task<GraphDeepFact[]> ProjectDeepReportAsync(SqliteConnection connection,
        SqliteTransaction transaction, string projectId, int revision, Dictionary<string,string> files,
        JsonElement context, GraphDeepMapping map, string kind, string key, string payload, CancellationToken ct)
    {
        JsonElement document = default;
        string? invalid;
        GraphDeepFact[] projected = [];
        try
        {
            document = JsonSerializer.Deserialize<JsonElement>(payload);
            invalid = GraphDeepValidity.Invalid(kind, key, document, revision, files, context);
            if (invalid is null)
            {
                if (GraphDeepMapping.Object(document, "graphContext").ValueKind != JsonValueKind.Object)
                {
                    var stamped = StampDeepContext(payload, kind, key, context);
                    await GraphSql.ExecuteAsync(connection, transaction,
                        "UPDATE graph_derived SET payload=$p3 WHERE project_id=$p0 AND kind=$p1 AND item_key=$p2;",
                        ct, projectId, kind, key, stamped);
                    document = JsonSerializer.Deserialize<JsonElement>(stamped);
                }
                projected = GraphDeepProjection.Read(kind, key, document, map);
            }
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or FormatException)
        { invalid = "INVALID_ANALYSIS_REPORT"; }
        var applied = kind == "jdt" ? projected.Length
            : projected.Count(f => !f.Edge.SourceId.StartsWith("file:", StringComparison.Ordinal));
        var total = kind == "jdt" ? GraphDeepMapping.Items(document, "candidates").Length
            : GraphDeepMapping.Items(GraphDeepMapping.Object(document, "result"), "calls").Length;
        await GraphSql.ExecuteAsync(connection, transaction, """
            INSERT INTO graph_deep_status VALUES ($p0,$p1,$p2,$p3,$p4,$p5);
            """, ct, projectId, kind, key, invalid ?? (applied > 0 ? "APPLIED" : "NO_UNIQUE_CALL_MAPPING"),
            applied, Math.Max(0, total - applied));
        return projected;
    }
}
