namespace SMSR.App.Mvp;

public sealed record GraphPathStep(GraphNode Source, GraphNode Target, GraphEdge Edge);
public sealed record GraphTrace(GraphNode Start, GraphNode Target, GraphPathStep[] Steps,
    bool Found, bool Truncated, int Revision, bool IncludesInferred);

public sealed partial class GraphQueryService
{
    public async Task<GraphTrace> TraceAsync(string projectId, string fromId, string toId,
        string? relation = null, bool includeInferred = false, int depth = 5, CancellationToken ct = default)
    {
        var path = await PathAsync(projectId, fromId, toId, depth, 1000, ct, relation, includeInferred);
        var ids = path.Edges.SelectMany(e => new[] { e.SourceId, e.TargetId }).Append(fromId).Append(toId).Distinct().ToArray();
        var nodes = (await store.GetGraphNodesAsync(projectId, ids, ct, path.Revision)).ToDictionary(n => n.NodeId);
        return new(nodes[fromId], nodes[toId], path.Edges.Select(e => new GraphPathStep(nodes[e.SourceId], nodes[e.TargetId], e)).ToArray(),
            path.Found, path.Truncated, path.Revision, path.Edges.Any(e => e.Confidence == "INFERRED" || e.Resolution == "INFERRED"));
    }
}
