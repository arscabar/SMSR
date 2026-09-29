using System.IO;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace SMSR.App.Mvp;

internal sealed record GraphIndexRequest(string ProjectId, string? RootPath = null, bool AllowLargeReduction = false,
    string[]? Folders = null);

internal static class GraphEndpoints
{
    public static void Map(WebApplication app, Func<string>? theme)
    {
        GraphAdvancedEndpoints.Map(app, theme);
        app.MapGet("/graph/explore", (string projectId, string? workflowId, string? view) =>
            view is "relations" or "flow"
                ? Results.Redirect("/graph/explore?projectId=" + Uri.EscapeDataString(projectId)
                    + (workflowId is null ? "" : "&workflowId=" + Uri.EscapeDataString(workflowId)))
                : Results.Content(GraphExplorerPage.Render(projectId, workflowId, view, theme?.Invoke()), "text/html; charset=utf-8"));
        app.MapPost("/api/graph/index", (GraphIndexRequest request, HttpRequest http,
            GraphIndexService index, EventStore store, CancellationToken ct) =>
            !LocalServerEndpoints.SameOrigin(http) ? Task.FromResult<IResult>(Results.Unauthorized())
                : RunAsync(async () =>
                {
                    var root = request.RootPath;
                    if (string.IsNullOrWhiteSpace(root))
                        root = (await GraphIndexOptionsService.GetAsync(store, request.ProjectId, ct))?.RootPath
                            ?? throw new ArgumentException("프로젝트 폴더를 찾지 못했습니다.");
                    return await index.IndexAsync(request.ProjectId, root, request.Folders,
                        request.AllowLargeReduction, ct);
                }));
        app.MapGet("/api/graph/index-options", (string projectId, EventStore store, CancellationToken ct)
            => RunAsync(() => GraphIndexOptionsService.GetAsync(store, projectId, ct)));
        app.MapGet("/api/graph/health", (string projectId, EventStore store, CancellationToken ct)
            => RunAsync(() => store.GetGraphHealthAsync(projectId, ct)));
        app.MapGet("/api/graph/freshness", (string projectId, GraphIndexService index, CancellationToken ct)
            => RunAsync(() => index.CheckFreshnessAsync(projectId, ct)));
        app.MapGet("/api/graph/search", (string projectId, string? q, int? limit, string? kind, int? offset,
            GraphQueryService query, CancellationToken ct)
            => RunAsync(() => query.SearchAsync(projectId, q ?? "", limit ?? 50, ct, kind, offset ?? 0)));
        app.MapGet("/api/graph/context", (string projectId, string nodeId, int? limit,
            GraphQueryService query, CancellationToken ct)
            => RunAsync(() => query.ContextAsync(projectId, nodeId, limit ?? 30, ct)));
        app.MapGet("/api/graph/path", (string projectId, string fromId, string toId, int? maxDepth,
            GraphQueryService query, CancellationToken ct)
            => RunAsync(() => query.PathAsync(projectId, fromId, toId, maxDepth ?? 5, 1000, ct)));
        app.MapGet("/api/graph/impact", (string projectId, string nodeId, int? maxDepth,
            GraphQueryService query, CancellationToken ct)
            => RunAsync(() => query.ImpactAsync(projectId, nodeId, maxDepth ?? 3, 100, ct)));
        app.MapGet("/api/graph/cycles", (string projectId, GraphQueryService query, CancellationToken ct)
            => RunAsync(() => query.CyclesAsync(projectId, ct)));
        app.MapGet("/api/graph/diff", (string projectId, int fromRevision, int toRevision,
            GraphQueryService query, CancellationToken ct)
            => RunAsync(() => query.DiffAsync(projectId, fromRevision, toRevision, ct)));
        app.MapPost("/api/graph/feedback", (GraphFeedbackRequest request, HttpRequest http,
            GraphFeedbackService feedback, CancellationToken ct) =>
            !LocalServerEndpoints.SameOrigin(http) ? Task.FromResult<IResult>(Results.Unauthorized())
                : RunAsync(() => feedback.RecordAsync(request, ct)));
        app.MapGet("/api/graph/feedback", (string projectId, string sourceId,
            GraphFeedbackService feedback, CancellationToken ct)
            => RunAsync(() => feedback.GetAsync(projectId, sourceId, ct)));
        app.MapGet("/api/graph/routes", (string projectId, GraphQueryService query, CancellationToken ct)
            => RunAsync(() => query.RoutesAsync(projectId, ct)));
        app.MapGet("/api/graph/validation", (string projectId, string workflowId,
            GraphValidationService validation, CancellationToken ct)
            => RunAsync(() => validation.GetAsync(projectId, workflowId, ct)));
        app.MapGet("/api/graph/scope", (string projectId, string nodeId, int? depth, string? direction,
            GraphQueryService query, CancellationToken ct)
            => RunAsync(() => query.ExportScopeAsync(projectId, nodeId, depth ?? 2, direction ?? "both", ct)));
        app.MapGet("/api/graph/communities", (string projectId, GraphQueryService query, CancellationToken ct)
            => RunAsync(() => query.CommunitiesAsync(projectId, ct)));
        app.MapGet("/api/graph/cross-repo", (string projectId, GraphQueryService query, CancellationToken ct)
            => RunAsync(() => query.CrossRepoAsync(projectId, ct)));
        app.MapGet("/api/graph/export", async (string projectId, string nodeId, int? depth, string? direction,
            GraphQueryService query, CancellationToken ct) =>
        {
            try
            {
                var scope = await query.ExportScopeAsync(projectId, nodeId, depth ?? 2, direction ?? "both", ct);
                return Results.File(JsonSerializer.SerializeToUtf8Bytes(scope), "application/json", "smsr-graph-scope.json");
            }
            catch (KeyNotFoundException error) { return Results.NotFound(new { error = error.Message }); }
            catch (ArgumentException error) { return Results.BadRequest(new { error = error.Message }); }
        });
        app.MapGet("/api/graph/evidence", (string projectId, string workflowId,
            GraphEvidenceService evidence, CancellationToken ct)
            => RunAsync(() => evidence.GetAsync(projectId, workflowId, ct)));
        app.MapGet("/graph/media", async (string projectId, string path, EventStore store,
            HttpContext http, CancellationToken ct) =>
        {
            try
            {
                var media = await new GraphMediaService(store).OpenAsync(projectId, path, ct);
                http.Response.Headers.XContentTypeOptions = "nosniff";
                http.Response.Headers.CacheControl = "no-store";
                http.Response.Headers.ContentSecurityPolicy = "default-src 'none'; sandbox";
                return Results.File(media.Stream, media.Mime, enableRangeProcessing: true);
            }
            catch (KeyNotFoundException) { return Results.NotFound(); }
            catch (ArgumentException error) { return Results.BadRequest(new { error = error.Message }); }
            catch (Exception error) when (error is InvalidOperationException or IOException or UnauthorizedAccessException)
            { return Results.Conflict(new { error = error.Message }); }
        });
        app.MapGet("/graph/source", async (string projectId, string path, int? line, string? workflowId,
            GraphSourceService source, HttpContext http, CancellationToken ct) =>
        {
            try
            {
                var preview = await source.GetAsync(projectId, path, line ?? 1, ct);
                http.Response.Headers.ContentSecurityPolicy = "default-src 'none'; style-src 'unsafe-inline'; base-uri 'none'; form-action 'none'; frame-ancestors 'none'";
                http.Response.Headers.XContentTypeOptions = "nosniff";
                http.Response.Headers.CacheControl = "no-store";
                return Results.Content(GraphSourcePage.Render(projectId, preview, theme?.Invoke(), workflowId), "text/html; charset=utf-8");
            }
            catch (KeyNotFoundException error) { return Results.NotFound(new { error = error.Message }); }
            catch (ArgumentException error) { return Results.BadRequest(new { error = error.Message }); }
            catch (Exception error) when (error is InvalidOperationException or IOException or UnauthorizedAccessException)
            { return Results.Conflict(new { error = error.Message }); }
        });
        app.MapGet("/graph", async (string projectId, string? workflowId, string? q, string? nodeId,
            bool? cycles, bool? routes, bool? communities, bool? crossRepo,
            int? fromRevision, int? toRevision,
            string? toId, GraphQueryService query, GraphEvidenceService evidence, GraphFeedbackService feedback,
            GraphValidationService validation,
            EventStore store, CancellationToken ct) =>
        {
            try
            {
                var info = await store.GetGraphHealthAsync(projectId, ct);
                var search = info is null ? null : await query.SearchAsync(projectId, q ?? "", 60, ct);
                var context = nodeId is null || info is null ? null : await query.ContextAsync(projectId, nodeId, 40, ct);
                var path = nodeId is null || toId is null || info is null ? null
                    : await query.PathAsync(projectId, nodeId, toId, 5, 1000, ct);
                var impact = nodeId is null || info is null ? null : await query.ImpactAsync(projectId, nodeId, 2, 40, ct);
                var reviews = nodeId is null || info is null ? null : await feedback.GetAsync(projectId, nodeId, ct);
                var links = workflowId is null || info is null ? null : await evidence.GetAsync(projectId, workflowId, ct);
                var gaps = workflowId is null || info is null ? null : await validation.GetAsync(projectId, workflowId, ct);
                var cycleReport = cycles == true && info is not null ? await query.CyclesAsync(projectId, ct) : null;
                var routeMap = routes == true && info is not null ? await query.RoutesAsync(projectId, ct) : null;
                var communityMap = communities == true && info is not null ? await query.CommunitiesAsync(projectId, ct) : null;
                var crossRepoMap = crossRepo == true && info is not null ? await query.CrossRepoAsync(projectId, ct) : null;
                var diff = fromRevision.HasValue && toRevision.HasValue && info is not null
                    ? await query.DiffAsync(projectId, fromRevision.Value, toRevision.Value, ct) : null;
                return Results.Content(GraphPage.Render(projectId, workflowId, q ?? "", info, search, context, path, impact, links, theme?.Invoke(), cycleReport, diff, reviews, routeMap, gaps, communityMap, crossRepoMap),
                    "text/html; charset=utf-8");
            }
            catch (Exception error) when (error is ArgumentException or KeyNotFoundException)
            { return Results.BadRequest(new { error = error.Message }); }
        });
    }

    private static async Task<IResult> RunAsync<T>(Func<Task<T>> action)
    {
        try
        {
            var result = await action();
            return result is null ? Results.NotFound() : Results.Ok(result);
        }
        catch (KeyNotFoundException error) { return Results.NotFound(new { error = error.Message }); }
        catch (ArgumentException error) { return Results.BadRequest(new { error = error.Message }); }
        catch (Exception error) when (error is InvalidOperationException or IOException or UnauthorizedAccessException)
        { return Results.Conflict(new { error = error.Message }); }
    }
}
