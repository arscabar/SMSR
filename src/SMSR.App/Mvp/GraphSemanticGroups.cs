namespace SMSR.App.Mvp;

internal static class GraphSemanticGroups
{
    internal static GraphHyperedge[] Build(GraphSemanticRequest r, GraphDocument doc, Dictionary<string, string> ids)
    {
        var seen = new HashSet<string>();
        return (r.Groups ?? []).Select(g =>
        {
            if (g is null || !seen.Add(g.Id)) throw new ArgumentException("의미 그룹이 비어 있거나 중복됩니다.");
            GraphSemanticValidation.Text(g.Id, 64); GraphSemanticValidation.Text(g.Label, 256);
            var block = GraphSemanticValidation.Evidence(doc, g.Location, g.Quote);
            if(GraphSemanticValidation.Visual(block)&&!g.Inferred)throw new ArgumentException("시각 그룹 해석은 inferred 후보여야 합니다.");
            var members = (g.Members ?? []).Select(m => m is not null && ids.TryGetValue(m.NodeId, out var id)
                ? m with { NodeId = id } : throw new ArgumentException("그룹 참여자는 이 문서의 의미 노드여야 합니다.")).ToArray();
            foreach (var m in members) GraphSemanticValidation.Text(m.Role, 64);
            var item = new GraphHyperedge("semantic:" + r.Path + ":" + g.Id, g.Label, "PARTICIPATES_IN",
                r.Path, block.Line, g.Inferred ? "INFERRED" : "EXTRACTED", members,
                new(r.Model, r.ContractVersion, doc.SourceHash));
            GraphHyperedgeValidation.Validate(item); return item;
        }).ToArray();
    }
}
