using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using ModelContextProtocol.Server;

namespace SMSR.App.Mvp;

internal static class LocalServerEndpoints
{
    public static void Map(WebApplication app, string token)
    {
        app.Use(async (context, next) =>
        {
            if (!string.Equals(context.Request.Host.Host, "127.0.0.1", StringComparison.Ordinal))
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }
            if (context.Request.Path.StartsWithSegments("/mcp") && !IsAuthorized(context.Request, token))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }
            await next();
        });
        app.MapGet("/api/state", (string? projectId, string? workflowId, EventStore events, CancellationToken ct) => GetStateAsync(projectId, workflowId, events, ct));
        app.MapGet("/api/plan", (string? projectId, string? workflowId, EventStore events, CancellationToken ct) => GetPlanAsync(projectId, workflowId, events, ct));
        app.MapGet("/api/summary", (string? projectId, string? workflowId, EventStore events, CancellationToken ct) => GetSummaryAsync(projectId, workflowId, events, ct));
        app.MapGet("/api/events/stream", async (string? projectId, string? workflowId, HttpResponse response, WorkflowEventNotifier notifier, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(projectId) || string.IsNullOrWhiteSpace(workflowId))
            {
                response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }
            await StreamAsync(projectId, workflowId, response, notifier, ct);
        });
        app.MapGet("/dashboard", async (string? projectId, string? workflowId, EventStore events, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(projectId) || string.IsNullOrWhiteSpace(workflowId))
                return Results.BadRequest(new { error = "projectId와 workflowId가 필요합니다." });
            var state = await events.GetStateAsync(projectId, workflowId, ct);
            var plan = await events.GetPlanAsync(projectId, workflowId, ct);
            return Results.Content(DashboardPage.Render(state, plan, await events.GetRecentEventsAsync(projectId, workflowId, ct)), "text/html; charset=utf-8");
        });
        app.MapMcp("/mcp");
    }

    internal static bool IsAuthorized(HttpRequest request, string token)
    {
        var value = request.Headers.Authorization.ToString();
        return value.StartsWith("Bearer ", StringComparison.Ordinal) &&
            CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(value[7..]), Encoding.UTF8.GetBytes(token));
    }

    private static async Task<IResult> GetStateAsync(string? projectId, string? workflowId, EventStore events, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(projectId) || string.IsNullOrWhiteSpace(workflowId))
            return Results.BadRequest(new { error = "projectId와 workflowId가 필요합니다." });
        return Results.Ok(await events.GetStateAsync(projectId, workflowId, ct));
    }

    private static async Task<IResult> GetPlanAsync(string? projectId, string? workflowId, EventStore events, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(projectId) || string.IsNullOrWhiteSpace(workflowId))
            return Results.BadRequest(new { error = "projectId와 workflowId가 필요합니다." });
        return Results.Ok(await events.GetPlanAsync(projectId, workflowId, ct));
    }

    private static async Task<IResult> GetSummaryAsync(string? projectId, string? workflowId, EventStore events, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(projectId) || string.IsNullOrWhiteSpace(workflowId))
            return Results.BadRequest(new { error = "projectId와 workflowId가 필요합니다." });
        var summary = await events.GetLatestSummaryAsync(projectId, workflowId, ct);
        return summary is null ? Results.NotFound() : Results.Ok(summary);
    }

    private static async Task StreamAsync(string projectId, string workflowId, HttpResponse response, WorkflowEventNotifier notifier, CancellationToken cancellationToken)
    {
        response.Headers.CacheControl = "no-cache";
        response.ContentType = "text/event-stream";
        var version = notifier.Version(projectId, workflowId);
        while (!cancellationToken.IsCancellationRequested)
        {
            await response.WriteAsync("event: state\ndata: changed\n\n", cancellationToken);
            await response.Body.FlushAsync(cancellationToken);
            await notifier.WaitForChangeAsync(projectId, workflowId, version, cancellationToken);
            version = notifier.Version(projectId, workflowId);
        }
    }
}
