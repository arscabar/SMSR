namespace SMSR.App.Mvp;

public sealed partial class GraphQueryService
{
    public async Task<GraphDiff> DiffAsync(string projectId, int fromRevision, int toRevision, CancellationToken ct = default)
    {
        var info = await RequireInfoAsync(projectId, ct);
        if (fromRevision < 1 || toRevision <= fromRevision || toRevision > info.Revision)
            throw new ArgumentException("비교할 리비전 범위가 올바르지 않습니다.");
        var beforeNodes = (await store.GetGraphRevisionNodesAsync(projectId, fromRevision, ct))
            .ToDictionary(node => node.NodeId, StringComparer.Ordinal);
        var afterNodes = (await store.GetGraphRevisionNodesAsync(projectId, toRevision, ct))
            .ToDictionary(node => node.NodeId, StringComparer.Ordinal);
        if (beforeNodes.Count == 0 || afterNodes.Count == 0)
            throw new KeyNotFoundException("비교할 리비전의 스냅샷이 없습니다. 이전 색인은 소급 복원할 수 없습니다.");
        var nodeChanges = new List<GraphNodeChange>();
        foreach (var id in beforeNodes.Keys.Union(afterNodes.Keys, StringComparer.Ordinal))
        {
            beforeNodes.TryGetValue(id, out var before);
            afterNodes.TryGetValue(id, out var after);
            if (before != after) nodeChanges.Add(new(before is null ? "ADDED" : after is null ? "REMOVED" : "CHANGED", before, after));
        }
        var beforeEdges = (await store.GetGraphRevisionEdgesAsync(projectId, fromRevision, ct)).ToDictionary(EdgeKey);
        var afterEdges = (await store.GetGraphRevisionEdgesAsync(projectId, toRevision, ct)).ToDictionary(EdgeKey);
        var edgeChanges = new List<GraphEdgeChange>();
        foreach (var key in beforeEdges.Keys.Union(afterEdges.Keys))
        {
            beforeEdges.TryGetValue(key, out var before);
            afterEdges.TryGetValue(key, out var after);
            if (before != after) edgeChanges.Add(new(before is null ? "ADDED" : after is null ? "REMOVED" : "CHANGED", before, after));
        }
        return new(fromRevision, toRevision, nodeChanges.Take(100).ToArray(), edgeChanges.Take(100).ToArray(),
            nodeChanges.Count, edgeChanges.Count, nodeChanges.Count > 100 || edgeChanges.Count > 100);
    }

    private static (string, string, string, string, int) EdgeKey(GraphEdge edge)
        => (edge.SourceId, edge.TargetId, edge.Relation, edge.OwnerPath, edge.SourceLine);
}
