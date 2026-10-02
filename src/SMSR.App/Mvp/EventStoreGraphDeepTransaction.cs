using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace SMSR.App.Mvp;

public sealed partial class EventStore
{
    private static async Task<int> RefreshGraphDeepTransactionAsync(SqliteConnection connection,
        SqliteTransaction transaction, string projectId, int expectedRevision, CancellationToken ct)
    {
        var info = await ReadGraphInfoAsync(connection, transaction, projectId, ct)
            ?? throw new KeyNotFoundException("프로젝트 색인이 없습니다.");
        if (info.Revision != expectedRevision) throw new InvalidOperationException("심층 통합 중 색인이 변경됐습니다. 다시 색인하세요.");
        var inputs = await ReadDeepInputsAsync(connection, transaction, projectId, ct);
        var facts = await ReadDeepProjectionAsync(connection, transaction, projectId, info.Revision, inputs.Files, inputs.Context, ct);
        var rows = facts.GroupBy(f => (f.Edge.SourceId, f.Edge.TargetId, f.Edge.Relation, f.Edge.OwnerPath, f.Edge.SourceLine))
            .Select(g => (Edge: g.OrderBy(f => f.Edge.Resolution == "RESOLVED" ? 0 : 1).First().Edge,
                Provenance: JsonSerializer.Serialize(g.Select(f => f.Evidence).Distinct().OrderBy(e => e.Analyzer)
                    .ThenBy(e => e.AnalysisKey), GraphWorker.Json)))
            .OrderBy(r => r.Edge.SourceId).ThenBy(r => r.Edge.TargetId).ThenBy(r => r.Edge.OwnerPath).ThenBy(r => r.Edge.SourceLine).ToArray();
        var old = await ReadDeepRowsAsync(connection, transaction, projectId, info.Revision, ct);
        if (old.SequenceEqual(rows)) return info.Revision;
        var revision = info.Revision + 1;
        await SaveDeepRevisionAsync(connection, transaction, projectId, revision, rows, ct);
        await SaveDeepManifestsAsync(connection, transaction, projectId, revision, facts, ct);
        return revision;
    }
}
