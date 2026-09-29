using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace SMSR.App.Mvp;

internal sealed record GraphAdvancedRequest(string ProjectId, string Input, string Scope = "");
internal static class GraphAdvancedEndpoints
{
    public static void Map(WebApplication app, Func<string>? theme)
    {
        app.MapGet("/graph/advanced", (string projectId, string? workflowId) => Results.Content(
            GraphAdvancedPage.Render(projectId, theme?.Invoke(), workflowId), "text/html; charset=utf-8"));
        app.MapPost("/api/graph/cypher", (GraphAdvancedRequest request, HttpRequest http,
            GraphAdvancedService service, CancellationToken ct) => Run(http, () => service.CypherAsync(request.ProjectId, request.Input, ct)));
        app.MapPost("/api/graph/semantic", (GraphAdvancedRequest request, HttpRequest http,
            GraphAdvancedService service, CancellationToken ct) => Run(http, () => service.SemanticAsync(request.ProjectId, request.Input, ct)));
        app.MapPost("/api/graph/body-semantic", (GraphAdvancedRequest request, HttpRequest http,
            GraphAdvancedService service, CancellationToken ct) => Run(http, () => service.BodySemanticAsync(request.ProjectId, request.Input, request.Scope, ct)));
        app.MapPost("/api/graph/analyze", (GraphAdvancedRequest request, HttpRequest http,
            GraphAdvancedService service, CancellationToken ct) => Run(http, () => service.AnalyzeAsync(request.ProjectId, request.Input, ct)));
        app.MapPost("/api/graph/analysis", (GraphAdvancedRequest request, HttpRequest http,
            GraphAdvancedService service, CancellationToken ct) => Run(http, () => service.AnalysisAsync(request.ProjectId, request.Input, ct)));
        app.MapPost("/api/graph/csharp", (GraphCSharpRequest request, HttpRequest http,
            GraphAdvancedService service, CancellationToken ct) => Run(http, () => service.AnalyzeCSharpAsync(request, ct)));
        app.MapPost("/api/graph/csharp-result", (GraphCSharpRequest request, HttpRequest http,
            GraphAdvancedService service, CancellationToken ct) => Run(http, () => service.CSharpAnalysisAsync(request, ct)));
        app.MapPost("/api/graph/java", (GraphJavaRequest request, HttpRequest http,
            GraphAdvancedService service, CancellationToken ct) => Run(http, () => service.AnalyzeJavaAsync(request, ct)));
        app.MapPost("/api/graph/java-result", (GraphJavaRequest request, HttpRequest http,
            GraphAdvancedService service, CancellationToken ct) => Run(http, () => service.JavaAnalysisAsync(request, ct)));
        app.MapPost("/api/graph/jdt", (GraphJdtRequest request, HttpRequest http,
            GraphAdvancedService service, CancellationToken ct) => Run(http, () => service.QueryJdtAsync(request, ct)));
        app.MapPost("/api/graph/jdt-result", (GraphJdtRequest request, HttpRequest http,
            GraphAdvancedService service, CancellationToken ct) => Run(http, () => service.JdtResultAsync(request, ct)));
        app.MapPost("/api/graph/typescript", (GraphTypeScriptRequest request, HttpRequest http,
            GraphAdvancedService service, CancellationToken ct) => Run(http, () => service.AnalyzeTypeScriptAsync(request, ct)));
        app.MapPost("/api/graph/typescript-result", (GraphTypeScriptRequest request, HttpRequest http,
            GraphAdvancedService service, CancellationToken ct) => Run(http, () => service.TypeScriptAnalysisAsync(request, ct)));
    }

    private static async Task<IResult> Run<T>(HttpRequest http, Func<Task<T>> action)
    {
        if (!LocalServerEndpoints.SameOrigin(http)) return Results.Unauthorized();
        http.HttpContext.Response.Headers.CacheControl = "no-store";
        try { return Results.Ok(await action()); }
        catch (ArgumentException error) { return Results.BadRequest(new { error = error.Message }); }
        catch (KeyNotFoundException error) { return Results.NotFound(new { error = error.Message }); }
        catch (Exception error) when (error is InvalidOperationException or IOException or UnauthorizedAccessException)
        { return Results.Conflict(new { error = error.Message }); }
    }
}
