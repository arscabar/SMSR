namespace SMSR.App.Mvp;

public sealed partial class GraphQueryService
{
    public async Task<GraphImpact> ImpactAsync(string projectId, string nodeId,
        int maxDepth = 3, int maxNodes = 100, CancellationToken ct = default,
        string? relation = null, bool includeInferred = true)
    {
        ValidateBounds(maxDepth, maxNodes);
        GraphEdgeSelection.Validate(relation);
        var info = await RequireInfoAsync(projectId, ct);
        ValidateNodeId(nodeId);
        if (await store.GetGraphNodeAsync(projectId, nodeId, ct, info.Revision) is null)
            throw new KeyNotFoundException("색인에 해당 노드가 없습니다.");
        var seen = new HashSet<string> { nodeId };
        var impacted = new List<string>();
        var steps = new List<GraphImpactStep>();
        var frontier = new List<string> { nodeId };
        var truncated = false;
        for (var depth = 0; depth < maxDepth && frontier.Count > 0; depth++)
        {
            var next = new List<string>();
            foreach (var chunk in frontier.Chunk(500))
            {
                var edges = await store.GetGraphAdjacentAsync(projectId, chunk, true, 5001, ct, info.Revision);
                if (edges.Count > 5000) { truncated = true; break; }
                foreach (var edge in edges)
                {
                    if (!GraphEdgeSelection.Matches(edge, relation, includeInferred)) continue;
                    if (!seen.Add(edge.SourceId)) continue;
                    if (impacted.Count >= maxNodes) { truncated = true; break; }
                    impacted.Add(edge.SourceId);
                    steps.Add(new(edge.SourceId, edge.TargetId, depth + 1, edge));
                    next.Add(edge.SourceId);
                }
                if (truncated) break;
            }
            if (truncated) break;
            frontier = next;
        }
        if (!truncated)
            foreach (var chunk in frontier.Chunk(500))
            {
                var remaining = await store.GetGraphAdjacentAsync(projectId, chunk, true, 5001, ct, info.Revision);
                if (remaining.Count > 5000 || remaining.Any(edge => GraphEdgeSelection.Matches(edge, relation, includeInferred) && !seen.Contains(edge.SourceId))) { truncated = true; break; }
            }
        return new(await store.GetGraphNodesAsync(projectId, impacted, ct, info.Revision), truncated, info.Revision, steps);
    }
}
