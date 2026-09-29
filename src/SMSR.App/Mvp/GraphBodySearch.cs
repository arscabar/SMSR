namespace SMSR.App.Mvp;

public sealed record GraphBodyHit(string Path, int Line, int EndLine, double Score, string SourceUrl);
public sealed record GraphBodyResult(int Revision, string Model, string Scope, int Files, int Chunks,
    int ExcludedFiles, IReadOnlyList<GraphBodyHit> Hits);

public sealed partial class GraphAdvancedService
{
    public async Task<GraphBodyResult> BodySemanticAsync(string projectId, string query,
        string scope = "", CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length > 2000)
            throw new ArgumentException("검색어는 1~2000자여야 합니다.");
        if (scope is null || scope.Length > 1024 || scope.Contains('\\') || scope.StartsWith('/')
            || scope.Split('/').Any(part => part is "." or "..") || scope.Contains(':'))
            throw new ArgumentException("검색 범위는 저장소 상대 파일 또는 폴더 경로여야 합니다.");
        var info = await RequireAsync(projectId, ct);
        var indexed = await store.GetGraphFilesAsync(projectId, ct, info.Revision);
        var paths = indexed.Keys.Where(path => scope.Length == 0 || path == scope
            || path.StartsWith(scope.TrimEnd('/') + "/", StringComparison.Ordinal)).Order(StringComparer.Ordinal).ToArray();
        if (paths.Length == 0) throw new KeyNotFoundException("검색 범위에 색인된 파일이 없습니다.");
        var chunks = new List<GraphBodyChunk>();
        var excluded = 0;
        foreach (var path in paths)
        {
            var source = await new GraphSourceService(store).ReadAsync(projectId, path, ct);
            if (source.Revision != info.Revision) throw new InvalidOperationException("색인이 변경됐습니다. 다시 검색하세요.");
            if (GraphBodyChunks.Excluded(source.Text)) { excluded++; continue; }
            chunks.AddRange(GraphBodyChunks.Split(path, source.Text));
            if (chunks.Count > 10000) throw new InvalidOperationException("본문 1만 조각 한도를 초과했습니다. 파일/폴더 범위를 지정하세요.");
        }
        var texts = chunks.Select(chunk => chunk.Text).ToArray();
        var vectors = await EmbedCachedAsync(projectId, "body-vectors", scope, info.Revision, texts, true, ct);
        var target = (await EmbedAsync([query], true, ct))[0];
        if ((await RequireAsync(projectId, ct)).Revision != info.Revision)
            throw new InvalidOperationException("색인이 변경됐습니다. 다시 검색하세요.");
        var hits = chunks.Select((chunk, i) => new GraphBodyHit(chunk.Path, chunk.Line, chunk.EndLine,
            Cosine(target, vectors[i]), System.Net.WebUtility.HtmlDecode(GraphPage.SourceUrl(projectId, chunk.Path, chunk.Line))))
            .OrderByDescending(hit => hit.Score).ThenBy(hit => hit.Path, StringComparer.Ordinal).ThenBy(hit => hit.Line).Take(20).ToArray();
        foreach (var path in hits.Select(hit => hit.Path).Distinct())
        {
            var current = await new GraphSourceService(store).ReadAsync(projectId, path, ct);
            if (current.Revision != info.Revision) throw new InvalidOperationException("색인이 변경됐습니다. 다시 검색하세요.");
        }
        return new(info.Revision, VectorModel, scope, paths.Length, chunks.Count, excluded, hits);
    }
}
