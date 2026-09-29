namespace SMSR.App.Mvp;

public sealed partial class GraphQueryService
{
    public async Task<GraphCycles> CyclesAsync(string projectId, CancellationToken ct = default)
    {
        var info = await RequireInfoAsync(projectId, ct);
        const int edgeLimit = 300_000;
        if (info.NodeCount > 100_000 || info.EdgeCount > edgeLimit)
            return new(info.Revision, [], 0, 0, 0, true);
        var edges = await store.GetGraphEdgePairsAsync(projectId, edgeLimit + 1, ct, info.Revision);
        return edges.Count > edgeLimit
            ? new(info.Revision, [], 0, 0, edges.Count, true)
            : GraphCycleDetector.Find(edges, info.Revision);
    }
}
