using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using ModelContextProtocol.Server;

namespace SMSR.App.Mvp;

internal static class LocalServerEndpoints
{
    public static void Map(WebApplication app, LocalOAuthStore oauth, McpBridgeToken bridgeToken, OAuthFlowStore flows,
        OAuthAuditLog audit, McpConnectionTracker connections, WorkflowEventNotifier notifier, ActivityJsonlStore activity,
        ActivityHookToken activityToken, Func<string>? dashboardTheme)
    {
        app.Use(async (context, next) =>
        {
            if (!string.Equals(context.Request.Host.Host, "127.0.0.1", StringComparison.Ordinal))
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }
            if (context.Request.Path.StartsWithSegments("/mcp") && !IsAuthorized(context.Request, oauth, bridgeToken))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.Headers.WWWAuthenticate = $"Bearer resource_metadata=\"{OAuthUris.Metadata(context.Request)}\", scope=\"{OAuthUris.Scope}\"";
                return;
            }
            if (context.Request.Path.StartsWithSegments("/mcp")) connections.MarkActivity();
            await next();
        });
        OAuthEndpoints.Map(app, oauth, flows, audit);
        ActivityEndpoints.Map(app, activity, activityToken, notifier);
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
        app.MapGet("/dashboard", async (string? projectId, string? workflowId, string? parentNodeId, string? selectedNodeId, EventStore events, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(projectId) || string.IsNullOrWhiteSpace(workflowId))
                return Results.BadRequest(new { error = "projectId와 workflowId가 필요합니다." });
            var state = await events.GetStateAsync(projectId, workflowId, ct);
            var plan = await events.GetPlanAsync(projectId, workflowId, ct);
            var recent = await events.GetRecentEventsAsync(projectId, workflowId, ct);
            return Results.Content(DashboardPage.Render(state, plan, recent, dashboardTheme?.Invoke(), parentNodeId,
                selectedNodeId, activity.ReadLatest(projectId, workflowId)), "text/html; charset=utf-8");
        });
        app.MapMcp("/mcp");
    }

    private static bool IsAuthorized(HttpRequest request, LocalOAuthStore oauth, McpBridgeToken bridgeToken)
    {
        var value = request.Headers.Authorization.ToString();
        if (!value.StartsWith("Bearer ", StringComparison.Ordinal)) return false;
        var token = value[7..];
        return bridgeToken.Validate(token) || oauth.ValidateAccess(token, OAuthUris.Resource(request));
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
