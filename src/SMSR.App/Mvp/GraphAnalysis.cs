using System.Text.Json;

namespace SMSR.App.Mvp;

public sealed partial class GraphAdvancedService
{
    private const int CodeAnalysisVersion = 10;
    public async Task<JsonElement> AnalyzeAsync(string projectId, string path, CancellationToken ct = default)
    {
        await RequireAsync(projectId, ct);
        var source = await new GraphSourceService(store).ReadAsync(projectId, path, ct);
        var result = await worker.RunAsync(new { operation = "analyze", path, source = source.Text }, ct);
        var current = await new GraphSourceService(store).ReadAsync(projectId, path, ct);
        if (current.Hash != source.Hash || current.Revision != source.Revision)
            throw new InvalidOperationException("분석 도중 원문 또는 색인이 변경됐습니다.");
        var payload = JsonSerializer.Serialize(new { analysisVersion = CodeAnalysisVersion, revision = source.Revision, hash = source.Hash,
            analyzedAt = DateTimeOffset.UtcNow, result }, GraphWorker.Json);
        await store.SaveGraphDerivedAsync(projectId, "analysis", path, source.Revision, payload, ct);
        return JsonSerializer.Deserialize<JsonElement>((await store.GetGraphDerivedAsync(projectId, "analysis", path, ct))!);
    }

    public async Task<object> AnalysisAsync(string projectId, string path, CancellationToken ct = default)
    {
        var info = await RequireAsync(projectId, ct);
        if (string.IsNullOrWhiteSpace(path) || path.Length > 1024) throw new ArgumentException("파일 경로를 지정하세요.");
        var saved = await store.GetGraphDerivedAsync(projectId, "analysis", path, ct)
            ?? throw new KeyNotFoundException("이 파일의 저장된 분석 결과가 없습니다.");
        var report = JsonSerializer.Deserialize<JsonElement>(saved);
        var files = await store.GetGraphFilesAsync(projectId, ct);
        var stale = !await store.IsGraphDeepReportCurrentAsync(projectId, "analysis", path, report, ct);
        if (!stale)
            try { await new GraphSourceService(store).ReadAsync(projectId, path, ct); }
            catch (Exception error) when (error is InvalidOperationException or KeyNotFoundException or System.IO.IOException)
            { stale = true; }
        return new { report, currentRevision = info.Revision, stale };
    }
}
