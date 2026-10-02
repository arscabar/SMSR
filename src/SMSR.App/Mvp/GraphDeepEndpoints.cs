using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace SMSR.App.Mvp;

internal static class GraphDeepEndpoints
{
    internal static void Map(WebApplication app)
    {
        app.MapGet("/api/graph/deep-status", (string projectId, EventStore store, CancellationToken ct)
            => Reply(async () => {
                await RequireAsync(store, projectId, ct);
                return await store.GetGraphDeepStatusAsync(projectId, ct);
            }));
        app.MapGet("/api/graph/deep-evidence", (string projectId, int revision, string analyzer,
            string analysisKey, EventStore store, CancellationToken ct) => Reply(async () => {
                var info = await RequireAsync(store, projectId, ct);
                if (revision < 1 || revision > info.Revision || !GraphDeepValidity.Supported(analyzer)
                    || string.IsNullOrWhiteSpace(analysisKey) || analysisKey.Length > 1024 || analysisKey.Any(char.IsControl))
                    throw new ArgumentException("분석 근거의 리비전·분석기·분석 키가 올바르지 않습니다.");
                return await store.GetGraphDeepManifestAsync(projectId, revision, analyzer, analysisKey, ct)
                    ?? throw new KeyNotFoundException("해당 리비전의 심층 분석 근거가 없습니다.");
            }));
    }
    private static async Task<GraphInfo> RequireAsync(EventStore store, string projectId, CancellationToken ct)
    {
        if (EventValidation.ValidateWorkflowIds(projectId, "graph") is { } error) throw new ArgumentException(error);
        return await store.GetGraphInfoAsync(projectId, ct) ?? throw new KeyNotFoundException("프로젝트 관계 색인이 없습니다.");
    }
    private static async Task<IResult> Reply<T>(Func<Task<T>> action)
    {
        try { return Results.Ok(await action()); }
        catch (ArgumentException e) { return Results.BadRequest(new { error = e.Message }); }
        catch (KeyNotFoundException e) { return Results.NotFound(new { error = e.Message }); }
        catch (InvalidOperationException e) { return Results.Conflict(new { error = e.Message }); }
    }
}
