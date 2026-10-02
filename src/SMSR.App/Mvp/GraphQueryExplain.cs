using System.IO;

namespace SMSR.App.Mvp;

public sealed record GraphExplanation(GraphNode Node, int Revision, int Incoming, int Outgoing,
    GraphIssuePage Diagnostics, string SourceStatus);

public sealed partial class GraphQueryService
{
    public async Task<GraphExplanation> ExplainAsync(string projectId, string nodeId, CancellationToken ct = default)
    {
        ValidateNodeId(nodeId);
        var info = await RequireInfoAsync(projectId, ct);
        var node = await store.GetGraphNodeAsync(projectId, nodeId, ct, info.Revision)
            ?? throw new KeyNotFoundException("색인에 해당 노드가 없습니다.");
        var incoming = await store.GraphRelationSliceAsync(projectId, nodeId, "incoming", null, 0, 1, info.Revision, ct);
        var outgoing = await store.GraphRelationSliceAsync(projectId, nodeId, "outgoing", null, 0, 1, info.Revision, ct);
        var issues = await store.GraphIssuesAsync(projectId, node.OwnerPath, 0, ct);
        if (issues.Revision != info.Revision) throw new InvalidOperationException("색인이 변경됐습니다. 다시 선택하세요.");
        var status = "NOT_CHECKED";
        if (GraphCodeIndexInput.Supported(node.SourcePath) || node.Kind is "document" or "heading")
        {
            try { await new GraphSourceService(store).GetAsync(projectId, node.SourcePath, node.Line, ct); status = "CURRENT"; }
            catch (Exception e) when (e is IOException or InvalidOperationException or KeyNotFoundException or ArgumentException)
            { status = "STALE_OR_UNAVAILABLE"; }
        }
        return new(node, info.Revision, incoming.Total, outgoing.Total, issues, status);
    }
}
