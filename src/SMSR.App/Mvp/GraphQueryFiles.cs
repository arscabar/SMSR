namespace SMSR.App.Mvp;

public sealed partial class GraphQueryService
{
    public async Task<GraphSearch> FilesAsync(string projectId, string query, string? kind = null,
        int offset = 0, int limit = 60, CancellationToken ct = default)
    {
        if (query is null || query.Length > 128 || offset is < 0 or > 1_000_000
            || kind is not (null or "code" or "document" or "image" or "video" or "audio"))
            throw new ArgumentException("파일 검색 조건이 올바르지 않습니다.");
        var info = await RequireInfoAsync(projectId, ct);
        limit = Math.Clamp(limit, 1, 100);
        var nodes = await store.SearchGraphFilesAsync(projectId, query, kind, info.Revision, offset, limit + 1, ct);
        return new(nodes.Take(limit).ToArray(), nodes.Count > limit, info.Revision);
    }
}
