namespace SMSR.App.Mvp;

public sealed record GraphVectorHit(GraphNode Node, double Score);
public sealed record GraphVectorResult(int Revision, string Model, IReadOnlyList<GraphVectorHit> Hits);
internal sealed record GraphVectorCache(string Model, Dictionary<string, float[]> Vectors);

public sealed partial class GraphAdvancedService
{
    private const string VectorModel = "sentence-transformers/paraphrase-multilingual-MiniLM-L12-v2";
    public async Task<GraphVectorResult> SemanticAsync(string projectId, string query, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length > 2000) throw new ArgumentException("검색어는 1~2000자여야 합니다.");
        var info = await RequireAsync(projectId, ct);
        var nodes = (await store.GetGraphRevisionNodesAsync(projectId, info.Revision, ct)).OrderBy(n => n.NodeId, StringComparer.Ordinal).ToArray();
        if (nodes.Length > 10000) throw new InvalidOperationException("벡터 색인은 1만 노드 이하를 지원합니다.");
        if (nodes.Length == 0) return new(info.Revision, VectorModel, []);
        var texts = nodes.Select(n => string.Join(" ", n.Kind, n.Label, n.SourcePath)).ToArray();
        var vectors = await EmbedCachedAsync(projectId, "vectors", VectorModel, info.Revision, texts, false, ct);
        var target = (await EmbedAsync([query], false, ct))[0];
        var hits = nodes.Select((node, i) => new GraphVectorHit(node, Cosine(target, vectors[i])))
            .OrderByDescending(hit => hit.Score).ThenBy(hit => hit.Node.NodeId, StringComparer.Ordinal).Take(20).ToArray();
        return new(info.Revision, VectorModel, hits);
    }

    private static double Cosine(float[] a, float[] b)
    {
        if (a.Length != b.Length) throw new InvalidOperationException("벡터 모델 차원이 일치하지 않습니다.");
        double dot = 0, aa = 0, bb = 0;
        for (var i = 0; i < a.Length; i++) { dot += a[i] * b[i]; aa += a[i] * a[i]; bb += b[i] * b[i]; }
        return aa == 0 || bb == 0 ? 0 : dot / Math.Sqrt(aa * bb);
    }
}
