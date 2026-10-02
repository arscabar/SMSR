namespace SMSR.App.Mvp;

internal static class GraphSemanticValidation
{
    internal static void Text(string text, int max)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Length > max || text.Any(char.IsControl)
            || GraphDocumentRedaction.Excluded(text)) throw new ArgumentException("의미 분석 값이 비어 있거나 제한·비밀값 규칙을 위반합니다.");
    }
    internal static GraphDocumentBlock Evidence(GraphDocument document, string location, string quote)
    {
        Text(location, 256);
        if (string.IsNullOrWhiteSpace(quote) || quote.Length > 512 || GraphDocumentRedaction.Excluded(quote)
            || quote.Any(c => char.IsControl(c) && c is not ('\r' or '\n' or '\t')))
            throw new ArgumentException("원문 인용이 비어 있거나 비밀값·길이 규칙을 위반합니다.");
        return document.Blocks.FirstOrDefault(b => b.Location == location && b.Text.Contains(quote, StringComparison.Ordinal))
            ?? GraphMediaRegion.Evidence(document,location,quote)
            ?? throw new ArgumentException("의미 분석 인용이 현재 원문 구간에 없습니다.");
    }
    internal static void Request(GraphSemanticRequest r, GraphDocument document)
    {
        Text(r.Model, 64); Text(r.ContractVersion, 64);
        if (r.ContractVersion != "semantic-v1" || r.ExpectedRevision != document.Revision
            || r.SourceHash != document.SourceHash || r.Nodes is null || r.Edges is null
            || r.Nodes.Length is < 1 or > 200 || r.Edges.Length > 400 || r.Groups?.Length > 3)
            throw new ArgumentException("의미 분석 버전·리비전·지문·수량이 올바르지 않습니다.");
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var n in r.Nodes)
        {
            if (n is null) throw new ArgumentException("의미 노드가 비어 있습니다.");
            Text(n.Id, 64); Text(n.Label, 256);
            if (!ids.Add(n.Id) || n.Kind is not ("concept" or "requirement" or "rationale"))
                throw new ArgumentException("의미 노드 ID·종류가 올바르지 않습니다.");
            var block=Evidence(document, n.Location, n.Quote);
            if(Visual(block)&&!n.Inferred)
                throw new ArgumentException("프레임·이미지 해석은 검증된 텍스트가 아니므로 inferred 후보로 제출하세요.");
        }
        foreach (var e in r.Edges)
        {
            if (e is null) throw new ArgumentException("의미 관계가 비어 있습니다.");
            Text(e.SourceId, 1024); Text(e.TargetId, 1024);
            if (e.Relation is not ("REQUIRES" or "JUSTIFIES" or "DESCRIBES" or "REFERENCES" or "SEMANTIC_CANDIDATE"))
                throw new ArgumentException("의미 관계 종류가 올바르지 않습니다.");
            if(Visual(Evidence(document,e.Location,e.Quote))&&!e.Inferred)
                throw new ArgumentException("이미지·프레임 메타데이터만으로 관계를 확정할 수 없습니다.");
        }
    }
    internal static bool Visual(GraphDocumentBlock block)=>block.Location.Contains("image:0,0,",StringComparison.Ordinal)&&block.Text.StartsWith("Image ",StringComparison.Ordinal)
        ||block.Location.StartsWith("time:",StringComparison.Ordinal)&&(block.Text.StartsWith("Video frame at ",StringComparison.Ordinal)||block.Text.StartsWith("Audio duration ",StringComparison.Ordinal));
}
