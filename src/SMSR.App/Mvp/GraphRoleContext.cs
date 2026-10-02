using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SMSR.App.Mvp;

public sealed partial class GraphRoleService(EventStore store)
{
    public async Task<GraphRoleContext> ContextAsync(string projectId, string nodeId,
        CancellationToken ct = default)
    {
        var graph = await new GraphQueryService(store).ContextAsync(projectId, nodeId, 20, ct);
        var edges = await store.GraphRoleEdgesAsync(projectId, graph.Node, graph.Revision, ct);
        var nodes = (await store.GetGraphNodesAsync(projectId,
            edges.SelectMany(e => new[] { e.SourceId, e.TargetId }).Distinct().ToArray(), ct, graph.Revision)).ToDictionary(n => n.NodeId);
        var links = edges.Take(20).Select(e => new GraphRoleLink(e, nodes[e.SourceId], nodes[e.TargetId])).ToArray();
        var collector = new GraphRoleEvidenceCollector(store, projectId, graph.Revision);
        await collector.AddAsync(graph.Node.SourcePath, graph.Node.Line, 80, null, ct);
        await GraphRolePartialContext.AddAsync(store, projectId, graph.Revision, graph.Node, collector, ct);
        foreach (var link in links.Take(6))
        {
            var edge = link.Edge with { Evidence = link.Edge.Evidence is null ? null : link.Edge.Evidence with { CapturedAt = null } };
            await collector.AddAsync(edge.OwnerPath, edge.SourceLine, 16, edge, ct);
            foreach (var node in new[] { link.From, link.To }.Where(n => n.SourcePath != graph.Node.SourcePath))
                await collector.AddAsync(node.SourcePath, node.Line, Math.Clamp((node.Details?.EndLine ?? node.Line + 40) - node.Line + 7, 8, 80), null, ct);
        }
        var evidence = collector.Evidence; var unavailable = collector.Unavailable;
        if (!evidence.Any(e => e.Edge is null && e.Path == graph.Node.SourcePath))
            throw new InvalidOperationException("현재 원문 근거를 읽을 수 없습니다. 색인을 확인하세요.");
        if ((await store.GetGraphInfoAsync(projectId, ct))?.Revision != graph.Revision)
            throw new InvalidOperationException("색인이 변경됐습니다. 다시 실행하세요.");
        var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
            JsonSerializer.Serialize(new { graph.Node, evidence, unavailable,
                links = links.Select(l => l with { Edge = l.Edge with { Evidence = l.Edge.Evidence is null ? null : l.Edge.Evidence with { CapturedAt = null } } }) }, GraphWorker.Json))));
        return new(graph.Node, graph.Revision, fingerprint, evidence, links, unavailable,
            collector.Truncated || edges.Length > 6);
    }
}
