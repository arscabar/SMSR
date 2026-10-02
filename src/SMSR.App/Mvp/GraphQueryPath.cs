namespace SMSR.App.Mvp;

public sealed partial class GraphQueryService
{
    public async Task<GraphPath> PathAsync(string projectId, string fromId, string toId,
        int maxDepth = 5, int maxNodes = 1000, CancellationToken ct = default,
        string? relation = null, bool includeInferred = true)
    {
        ValidateBounds(maxDepth, maxNodes);
        GraphEdgeSelection.Validate(relation);
        var info = await RequireInfoAsync(projectId, ct);
        ValidateNodeId(fromId);
        ValidateNodeId(toId);
        if (await store.GetGraphNodeAsync(projectId, fromId, ct, info.Revision) is null
            || await store.GetGraphNodeAsync(projectId, toId, ct, info.Revision) is null)
            throw new KeyNotFoundException("출발 또는 도착 노드가 색인에 없습니다.");
        if (fromId == toId) return new([], true, false, 1, info.Revision);
        var parent = new Dictionary<string, GraphEdge>();
        var seen = new HashSet<string> { fromId };
        var frontier = new List<string> { fromId };
        for (var depth = 0; depth < maxDepth && frontier.Count > 0; depth++)
        {
            var next = new List<string>();
            foreach (var chunk in frontier.Chunk(500))
            {
                var edges = await store.GetGraphAdjacentAsync(projectId, chunk, false, 5001, ct, info.Revision);
                if (edges.Count > 5000) return new([], false, true, seen.Count, info.Revision);
                foreach (var edge in edges)
                {
                    if (!GraphEdgeSelection.Matches(edge, relation, includeInferred)) continue;
                    if (seen.Contains(edge.TargetId)) continue;
                    if (seen.Count >= maxNodes) return new([], false, true, seen.Count, info.Revision);
                    seen.Add(edge.TargetId);
                    parent[edge.TargetId] = edge;
                    if (edge.TargetId == toId)
                    {
                        var path = new List<GraphEdge>();
                        for (var current = toId; current != fromId; current = parent[current].SourceId)
                            path.Add(parent[current]);
                        path.Reverse();
                        return new(path, true, false, seen.Count, info.Revision);
                    }
                    next.Add(edge.TargetId);
                }
            }
            frontier = next;
        }
        foreach (var chunk in frontier.Chunk(500))
        {
            var remaining = await store.GetGraphAdjacentAsync(projectId, chunk, false, 5001, ct, info.Revision);
            if (remaining.Count > 5000 || remaining.Any(edge => GraphEdgeSelection.Matches(edge, relation, includeInferred) && !seen.Contains(edge.TargetId)))
                return new([], false, true, seen.Count, info.Revision);
        }
        return new([], false, false, seen.Count, info.Revision);
    }
}
