using System.Text;
using System.Text.Json;

namespace SMSR.App.Mvp;

public sealed partial class GraphAdvancedService
{
    private const int CSharpAnalysisVersion = 9;
    internal async Task<JsonElement> AnalyzeCSharpAsync(GraphCSharpRequest request, CancellationToken ct = default)
    {
        request = request.Normalize();
        var info = await RequireAsync(request.ProjectId, ct);
        var sources = new List<object>();
        var hashes = new Dictionary<string, string>();
        var reader = new GraphSourceService(store);
        long size = 0;
        foreach (var path in request.Paths)
        {
            var file = await reader.ReadAsync(request.ProjectId, path, ct);
            if (file.Revision != info.Revision) throw new InvalidOperationException("분석 도중 색인이 변경됐습니다.");
            size += Encoding.UTF8.GetByteCount(file.Text);
            if (size > 16 * 1024 * 1024) throw new ArgumentException("C# 묶음은 전체 16 MiB 이하여야 합니다.");
            sources.Add(new { path, text = file.Text });
            hashes.Add(path, file.Hash);
        }
        var result = await worker.RunCSharpAsync(new { files = sources, request.LanguageVersion, request.Defines }, ct);
        foreach (var pair in hashes)
            if ((await reader.ReadAsync(request.ProjectId, pair.Key, ct)).Hash != pair.Value)
                throw new InvalidOperationException("분석 도중 원문이 변경됐습니다.");
        var payload = JsonSerializer.Serialize(new { analysisVersion = CSharpAnalysisVersion, revision = info.Revision, analyzedAt = DateTimeOffset.UtcNow,
            inputHashes = hashes, result }, GraphWorker.Json);
        await store.SaveGraphDerivedAsync(request.ProjectId, "csharp", request.Key, info.Revision, payload, ct);
        return JsonSerializer.Deserialize<JsonElement>((await store.GetGraphDerivedAsync(request.ProjectId, "csharp", request.Key, ct))!);
    }

    internal async Task<object> CSharpAnalysisAsync(GraphCSharpRequest request, CancellationToken ct = default)
    {
        request = request.Normalize();
        var info = await RequireAsync(request.ProjectId, ct);
        var saved = await store.GetGraphDerivedAsync(request.ProjectId, "csharp", request.Key, ct)
            ?? throw new KeyNotFoundException("같은 파일·버전·조건부 심벌 묶음의 분석 결과가 없습니다.");
        var report = JsonSerializer.Deserialize<JsonElement>(saved);
        var stale = !await store.IsGraphDeepReportCurrentAsync(request.ProjectId, "csharp", request.Key, report, ct);
        foreach (var pair in report.GetProperty("inputHashes").EnumerateObject())
            try { stale |= (await new GraphSourceService(store).ReadAsync(request.ProjectId, pair.Name, ct)).Hash != pair.Value.GetString(); }
            catch (Exception e) when (e is InvalidOperationException or KeyNotFoundException or System.IO.IOException or UnauthorizedAccessException) { stale = true; }
        return new { report, currentRevision = info.Revision, stale };
    }
}
