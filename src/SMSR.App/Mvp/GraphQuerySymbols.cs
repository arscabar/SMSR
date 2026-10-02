namespace SMSR.App.Mvp;

public sealed record GraphSymbols(IReadOnlyList<GraphNode> Nodes, bool Truncated, int Revision,
    IReadOnlyDictionary<string, string> Owners);

public sealed partial class GraphQueryService
{
    public async Task<GraphSymbols> SymbolsAsync(string projectId, string nodeId, int offset = 0,
        int? expectedRevision = null, CancellationToken ct = default)
    {
        ValidateNodeId(nodeId);
        if (offset is < 0 or > 1_000_000) throw new ArgumentException("심벌 페이지가 올바르지 않습니다.");
        var info = await RequireInfoAsync(projectId, ct);
        if (expectedRevision.HasValue && expectedRevision != info.Revision)
            throw new InvalidOperationException("색인이 변경되었습니다. 검색을 다시 실행하세요.");
        var parent = await store.GetGraphNodeAsync(projectId, nodeId, ct, info.Revision)
            ?? throw new KeyNotFoundException("선택 항목이 색인에 없습니다.");
        if (parent.Kind is not ("code" or "symbol")) throw new ArgumentException("코드 구성요소만 조회할 수 있습니다.");
        var nodes = await store.GraphChildrenAsync(projectId, parent, info.Revision, offset, 31, ct);
        var visible = nodes.Take(30).ToArray();
        var ids = visible.Select(n => n.Details?.OwnerNodeId).OfType<string>().Distinct().ToArray();
        var owners = ids.Length == 0 ? [] : await store.GetGraphNodesAsync(projectId, ids, ct, info.Revision);
        return new(visible, nodes.Count > 30, info.Revision, owners.ToDictionary(n => n.NodeId, n => n.Label));
    }
}
