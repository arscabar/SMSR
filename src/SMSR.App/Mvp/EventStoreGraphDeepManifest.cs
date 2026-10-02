using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace SMSR.App.Mvp;

public sealed partial class EventStore
{
    private static async Task SaveDeepManifestsAsync(SqliteConnection connection, SqliteTransaction transaction,
        string projectId, int revision, GraphDeepFact[] facts, CancellationToken ct)
    {
        foreach (var evidence in facts.Select(f => f.Evidence).DistinctBy(e => (e.Analyzer, e.AnalysisKey)))
        {
            using var query = GraphSql.Create(connection, transaction,
                "SELECT payload FROM graph_derived WHERE project_id=$p0 AND kind=$p1 AND item_key=$p2;",
                projectId, evidence.Analyzer, evidence.AnalysisKey);
            var payload = await query.ExecuteScalarAsync(ct) as string ?? throw new InvalidOperationException("심층 근거가 변경됐습니다.");
            var manifest = GraphDeepManifest.Read(evidence.Analyzer, evidence.AnalysisKey, JsonSerializer.Deserialize<JsonElement>(payload));
            if (manifest.Hash != evidence.ManifestHash) throw new InvalidOperationException("심층 근거 지문이 변경됐습니다.");
            await GraphSql.ExecuteAsync(connection, transaction, """
                INSERT INTO graph_deep_manifests VALUES ($p0,$p1,$p2,$p3,$p4);
                """, ct, projectId, revision, evidence.Analyzer, evidence.AnalysisKey,
                JsonSerializer.Serialize(manifest, GraphWorker.Json));
        }
    }

    public async Task<GraphDeepManifest?> GetGraphDeepManifestAsync(string projectId, int revision, string analyzer,
        string analysisKey, CancellationToken ct = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(ct);
        using var query = GraphSql.Create(connection, null, """
            SELECT manifest_json FROM graph_deep_manifests WHERE project_id=$p0 AND revision=$p1 AND kind=$p2 AND item_key=$p3;
            """, projectId, revision, analyzer, analysisKey);
        var payload = await query.ExecuteScalarAsync(ct) as string;
        return payload is null ? null : JsonSerializer.Deserialize<GraphDeepManifest>(payload, GraphWorker.Json);
    }
}
