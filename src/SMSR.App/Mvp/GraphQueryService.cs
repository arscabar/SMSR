namespace SMSR.App.Mvp;

public sealed partial class GraphQueryService(EventStore store)
{
    public async Task<GraphSearch> SearchAsync(string projectId, string query,
        int limit = 50, CancellationToken ct = default, string? kind = null, int offset = 0)
    {
        var info = await RequireInfoAsync(projectId, ct);
        if (query is null || query.Length > 128) throw new ArgumentException("검색어는 128자 이하여야 합니다.", nameof(query));
        if (offset is < 0 or > 1_000_000) throw new ArgumentException("offset은 0~1,000,000이어야 합니다.", nameof(offset));
        limit = Math.Clamp(limit, 1, 100);
        var nodes = await store.SearchGraphNodesAsync(projectId, query, limit + 1, ct, info.Revision, kind, offset);
        return new(nodes.Take(limit).ToArray(), nodes.Count > limit, info.Revision);
    }

    public async Task<GraphContext> ContextAsync(string projectId, string nodeId,
        int limit = 30, CancellationToken ct = default)
    {
        var info = await RequireInfoAsync(projectId, ct);
        ValidateNodeId(nodeId);
        var node = await store.GetGraphNodeAsync(projectId, nodeId, ct, info.Revision)
            ?? throw new KeyNotFoundException("색인에 해당 노드가 없습니다.");
        limit = Math.Clamp(limit, 1, 100);
        var outgoing = await store.GetGraphAdjacentAsync(projectId, [nodeId], false, limit + 1, ct, info.Revision);
        var incoming = await store.GetGraphAdjacentAsync(projectId, [nodeId], true, limit + 1, ct, info.Revision);
        var ids = outgoing.Take(limit).Select(edge => edge.TargetId)
            .Concat(incoming.Take(limit).Select(edge => edge.SourceId)).Distinct().ToArray();
        var nodes = (await store.GetGraphNodesAsync(projectId, ids, ct, info.Revision)).ToDictionary(item => item.NodeId);
        return new(node,
            outgoing.Take(limit).Where(edge => nodes.ContainsKey(edge.TargetId)).Select(edge => new GraphNeighbor(edge, nodes[edge.TargetId])).ToArray(),
            incoming.Take(limit).Where(edge => nodes.ContainsKey(edge.SourceId)).Select(edge => new GraphNeighbor(edge, nodes[edge.SourceId])).ToArray(),
            outgoing.Count > limit || incoming.Count > limit, info.Revision);
    }

    private async Task<GraphInfo> RequireInfoAsync(string projectId, CancellationToken ct)
    {
        if (EventValidation.ValidateWorkflowIds(projectId, "graph") is { } error)
            throw new ArgumentException(error, nameof(projectId));
        return await store.GetGraphInfoAsync(projectId, ct)
            ?? throw new KeyNotFoundException("이 프로젝트의 관계 색인이 없습니다.");
    }

    private static void ValidateNodeId(string nodeId)
    {
        if (string.IsNullOrWhiteSpace(nodeId) || nodeId.Length > 1024)
            throw new ArgumentException("nodeId는 1~1,024자여야 합니다.", nameof(nodeId));
    }

    private static void ValidateBounds(int maxDepth, int maxNodes)
    {
        if (maxDepth is < 1 or > 8 || maxNodes is < 1 or > 2000)
            throw new ArgumentException("maxDepth는 1~8, maxNodes는 1~2,000이어야 합니다.");
    }
}
