using System.Text.Json;

namespace SMSR.App.Mvp;

public sealed partial class GraphOverviewService(EventStore store, GraphWorker worker)
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    internal async Task<GraphOverview> SummaryAsync(string projectId, CancellationToken ct)
    {
        if (EventValidation.ValidateWorkflowIds(projectId, "graph") is { } error) throw new ArgumentException(error);
        await _gate.WaitAsync(ct);
        try
        {
            var info = await store.GetGraphInfoAsync(projectId, ct) ?? throw new KeyNotFoundException("색인이 없습니다.");
            if (info.NodeCount > 50000 || info.EdgeCount > 200000) throw new InvalidOperationException("그룹 분석 한도는 5만 노드·20만 관계입니다.");
            var raw = await store.GetGraphDerivedAsync(projectId, "overview-v1", "", ct);
            if (raw is not null && JsonSerializer.Deserialize<GraphOverview>(raw, GraphWorker.Json) is { } saved && saved.Revision == info.Revision) return saved;
            var edges = await store.GetGraphRevisionEdgesAsync(projectId, info.Revision, ct);
            var data = await worker.RunOverviewAsync(new { operation = "overview", revision = info.Revision,
                nodes = (await store.GetGraphRevisionNodesAsync(projectId, info.Revision, ct)).Select(n => new { n.NodeId, n.Label, n.Kind, n.SourcePath }),
                edges = edges.Select(e => new { e.SourceId, e.TargetId,
                    e.Relation, e.Confidence, e.Resolution, e.OwnerPath, e.SourceLine }) }, ct);
            var result = data.Deserialize<GraphOverview>(GraphWorker.Json) ?? throw new InvalidOperationException("그룹 결과를 읽지 못했습니다.");
            result = RestoreEvidence(result, edges);
            await store.SaveGraphDerivedAsync(projectId, "overview-v1", "", info.Revision, JsonSerializer.Serialize(result, GraphWorker.Json), ct);
            return result;
        }
        finally { _gate.Release(); }
    }
    internal static void Bounds(int offset, int limit)
    { if (offset is < 0 or > 50000 || limit is < 1 or > 100) throw new ArgumentException("그룹 조회 범위가 올바르지 않습니다."); }
}
