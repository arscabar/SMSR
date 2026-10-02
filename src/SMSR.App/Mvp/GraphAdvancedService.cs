using System.Text.Json;

namespace SMSR.App.Mvp;

public sealed partial class GraphAdvancedService(EventStore store, GraphWorker worker)
{
    public async Task<JsonElement> CypherAsync(string projectId, string query,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length > 8000) throw new ArgumentException("Cypher는 1~8000자여야 합니다.");
        var info = await RequireAsync(projectId, ct);
        if (info.NodeCount > 50000 || info.EdgeCount > 200000)
            throw new InvalidOperationException("Cypher 투영 한도는 5만 노드·20만 관계입니다.");
        return await worker.RunAsync(new { operation = "cypher", query, revision = info.Revision,
            nodes = (await store.GetGraphRevisionNodesAsync(projectId, info.Revision, ct))
                .Select(n => new { n.NodeId, n.Label, n.Kind, n.SourcePath, n.Line }),
            edges = (await store.GetGraphRevisionEdgesAsync(projectId, info.Revision, ct))
                .Select(e => new { e.SourceId, e.TargetId, e.Relation, e.Confidence }) }, ct);
    }

    private async Task<GraphInfo> RequireAsync(string projectId, CancellationToken ct)
    {
        if (EventValidation.ValidateWorkflowIds(projectId, "graph") is { } error) throw new ArgumentException(error);
        return await store.GetGraphInfoAsync(projectId, ct)
            ?? throw new KeyNotFoundException("저장소를 먼저 색인하세요.");
    }
}
