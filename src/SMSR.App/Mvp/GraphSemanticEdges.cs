namespace SMSR.App.Mvp;

internal static class GraphSemanticEdges
{
    internal static async Task<GraphEdge[]> BuildAsync(EventStore store, GraphSemanticRequest r, GraphDocument doc,
        Dictionary<string, string> ids, Dictionary<string, string> dependencies, CancellationToken ct)
    {
        var evidence = new GraphSourceEvidence(r.Model, r.ContractVersion, doc.SourceHash);
        var edges = r.Nodes.Select(n => new GraphEdge("file:" + r.Path, ids[n.Id], "DESCRIBES", r.Path,
            GraphSemanticValidation.Evidence(doc, n.Location, n.Quote).Line, "RESOLVED", n.Inferred ? "INFERRED" : "EXTRACTED", Evidence: evidence)).ToList();
        async Task<string> Resolve(string id, GraphSemanticEdge edge)
        {
            if (ids.TryGetValue(id, out var local)) return local;
            var target = await store.GetGraphNodeAsync(r.ProjectId, id, ct, doc.Revision)
                ?? throw new ArgumentException("존재하지 않는 의미 관계 대상입니다.");
            if (target.Kind is "concept" or "requirement" or "rationale")
                throw new ArgumentException("다른 문서의 의미 노드는 명시적인 문서 참조로 연결하세요.");
            if (!edge.Inferred && target.Kind == "symbol" && !edge.Quote.Contains(target.SourcePath, StringComparison.Ordinal))
            {
                var matches = await store.SearchGraphNodesAsync(r.ProjectId, target.Label, 201, ct, doc.Revision, "code");
                if (matches.Count > 200 || matches.Count(n => n.Label == target.Label && n.Kind == "symbol") != 1)
                    throw new ArgumentException("동명 심벌은 파일 경로를 인용하거나 추론 후보로 제출하세요.");
            }
            if (!edge.Inferred && !edge.Quote.Contains(target.SourcePath, StringComparison.Ordinal)
                && (target.Kind != "symbol" || !edge.Quote.Contains(target.Label, StringComparison.Ordinal)))
                throw new ArgumentException("명시적 코드 관계에 대상 경로·심벌 인용이 없습니다.");
            dependencies[target.SourcePath] = target.Hash;
            return id;
        }
        foreach (var e in r.Edges)
        {
            if (!ids.ContainsKey(e.SourceId) && !ids.ContainsKey(e.TargetId)) throw new ArgumentException("의미 관계에는 제출한 의미 노드가 참여해야 합니다.");
            if (e.Relation == "SEMANTIC_CANDIDATE" && !e.Inferred) throw new ArgumentException("의미 유사성은 후보로 구분하세요.");
            var line = GraphSemanticValidation.Evidence(doc, e.Location, e.Quote).Line;
            edges.Add(new(await Resolve(e.SourceId, e), await Resolve(e.TargetId, e),
                e.Inferred ? "SEMANTIC_CANDIDATE" : e.Relation, r.Path, line,
                e.Inferred ? "CANDIDATE" : "RESOLVED", e.Inferred ? "INFERRED" : "EXTRACTED", Evidence: evidence));
        }
        return edges.Distinct().ToArray();
    }
}
