namespace SMSR.App.Mvp;

public sealed record GraphRelationItem(GraphEdge Edge, GraphNode Node, bool Incoming);
public sealed record GraphRelationPage(GraphNode Node, GraphRelationItem[] Items,
    int Total, int Offset, int Limit, int Revision, string Direction, string? Relation);

public sealed partial class GraphQueryService
{
    public async Task<GraphRelationPage> RelationsAsync(string projectId, string nodeId,
        string direction = "both", string? relation = null, int offset = 0, int limit = 20,
        int? expectedRevision = null, CancellationToken ct = default)
    {
        ValidateNodeId(nodeId);
        if (direction is not ("both" or "incoming" or "outgoing")
            || offset is < 0 or > 1_000_000 || limit is < 1 or > 100
            || relation is not null && (relation.Length is < 1 or > 48
                || relation.Any(c => !char.IsAsciiLetterUpper(c) && c != '_')))
            throw new ArgumentException("관계 방향·종류 또는 페이지 범위가 올바르지 않습니다.");
        var info = await RequireInfoAsync(projectId, ct);
        if (expectedRevision.HasValue && expectedRevision != info.Revision)
            throw new InvalidOperationException("색인이 변경됐습니다. 항목을 다시 선택하세요.");
        var node = await store.GetGraphNodeAsync(projectId, nodeId, ct, info.Revision)
            ?? throw new KeyNotFoundException("색인에 해당 노드가 없습니다.");
        var slice = await store.GraphRelationSliceAsync(projectId, nodeId, direction, relation, offset, limit, info.Revision, ct);
        var ids = slice.Edges.Select(e => e.TargetId == nodeId ? e.SourceId : e.TargetId).Distinct().ToArray();
        var nodes = (await store.GetGraphNodesAsync(projectId, ids, ct, info.Revision)).ToDictionary(n => n.NodeId);
        return new(node, slice.Edges.Select(e => new GraphRelationItem(e,
            nodes[e.TargetId == nodeId ? e.SourceId : e.TargetId], e.TargetId == nodeId)).ToArray(),
            slice.Total, offset, limit, info.Revision, direction, relation);
    }
}
