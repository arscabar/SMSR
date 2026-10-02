namespace SMSR.App.Mvp;

public sealed partial class GraphSemanticService
{
    public async Task<GraphSemanticReport> SubmitAsync(GraphSemanticRequest request, CancellationToken ct = default)
    {
        var doc = await documents.ReadAsync(request.ProjectId, request.Path, ct);
        GraphSemanticValidation.Request(request, doc);
        var dependencies = new Dictionary<string, string> { [request.Path] = doc.SourceHash };
        var ids = request.Nodes.ToDictionary(n => n.Id, n => GraphSemanticCache.Identity(request.Path, n.Id));
        var nodes = request.Nodes.Select(n => new GraphNode(ids[n.Id], request.Path, n.Kind, n.Label, request.Path,
            GraphSemanticValidation.Evidence(doc, n.Location, n.Quote).Line, doc.SourceHash,
            new(n.Kind, GraphMediaTypes.TryGet(request.Path,out var mediaType)?mediaType.Kind:"document", request.Model, request.ContractVersion, "file:" + request.Path, Rationale: n.Quote,SourceLocation:n.Location))).ToArray();
        var edges = await GraphSemanticEdges.BuildAsync(store, request, doc, ids, dependencies, ct);
        var report = new GraphSemanticReport(request.Path, doc.SourceHash, request.Model, request.ContractVersion,
            doc.Revision, DateTimeOffset.UtcNow, request.Nodes, request.Edges, dependencies, request.Groups,doc.ExtractionFingerprint);
        var groups = GraphSemanticGroups.Build(request, doc, ids);
        foreach (var p in dependencies.Where(p => p.Key != request.Path))
            if (GraphDocumentService.Binary(p.Key)) await documents.ReadAsync(request.ProjectId, p.Key, ct);
            else await new GraphSourceService(store).ReadAsync(request.ProjectId, p.Key, ct);
        var current = await documents.ReadAsync(request.ProjectId, request.Path, ct);
        if (current.SourceHash != doc.SourceHash || current.Revision != doc.Revision || current.ExtractionFingerprint!=doc.ExtractionFingerprint) throw new InvalidOperationException("의미 제출 중 원문·추출 환경이 변경되었습니다.");
        var cached = await GraphSemanticCache.FindAsync(store, request, dependencies, doc.ExtractionFingerprint, ct);
        if (cached is not null && (await store.GetGraphInfoAsync(request.ProjectId, ct))?.Revision != request.ExpectedRevision)
            throw new InvalidOperationException("캐시 확인 중 색인이 변경되었습니다.");
        return cached ?? await store.SaveGraphSemanticAsync(request.ProjectId, report, nodes, edges, groups, ct);
    }
}
