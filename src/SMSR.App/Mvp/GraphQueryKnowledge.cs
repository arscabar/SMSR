namespace SMSR.App.Mvp;

public sealed partial class GraphQueryService
{
    public async Task<GraphHyperedgePage> HyperedgesAsync(string projectId, string? nodeId = null,
        int offset = 0, int limit = 20, int? expectedRevision = null, CancellationToken ct = default)
    {
        var info = await RequireInfoAsync(projectId, ct);
        if (offset is < 0 or > 1_000_000 || limit is < 1 or > 100)
            throw new ArgumentException("offset은 0~1,000,000, limit은 1~100이어야 합니다.");
        if (expectedRevision is { } requested && (requested < 1 || requested > info.Revision))
            throw new ArgumentException("존재하는 리비전을 지정하세요.");
        var revision = expectedRevision ?? info.Revision;
        if (nodeId is not null)
        {
            ValidateNodeId(nodeId);
            if (await store.GetGraphNodeAsync(projectId, nodeId, ct, revision) is null)
                throw new KeyNotFoundException("색인에 해당 노드가 없습니다.");
        }
        var items = await store.HyperedgeSliceAsync(projectId, revision, nodeId, offset, limit + 1, ct);
        return new(revision, items.Take(limit).ToArray(), items.Length > limit);
    }

    public async Task<GraphCoverage> CoverageAsync(string projectId, int offset = 0,
        int limit = 50, CancellationToken ct = default)
    {
        var info = await RequireInfoAsync(projectId, ct);
        if (offset is < 0 or > 1_000_000 || limit is < 1 or > 100)
            throw new ArgumentException("offset은 0~1,000,000, limit은 1~100이어야 합니다.");
        return await store.GraphCoverageAsync(projectId, info.Revision, offset, limit, ct);
    }
}
