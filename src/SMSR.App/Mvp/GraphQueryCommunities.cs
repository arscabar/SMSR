namespace SMSR.App.Mvp;

public sealed partial class GraphQueryService
{
    public async Task<GraphCommunities> CommunitiesAsync(string projectId, CancellationToken ct = default)
    {
        var info = await RequireInfoAsync(projectId, ct);
        if (info.NodeCount > 100_000 || info.EdgeCount > 300_000)
            return new(info.Revision, [], 0, 0, 0, 0, true);
        var ids = await store.GetGraphNodeIdsAsync(projectId, 100_001, ct, info.Revision);
        var edges = await store.GetGraphEdgePairsAsync(projectId, 300_001, ct, info.Revision);
        return ids.Count > 100_000 || edges.Count > 300_000
            ? new(info.Revision, [], 0, 0, ids.Count, edges.Count, true)
            : GraphCommunityDetector.Find(ids, edges, info.Revision);
    }
}
