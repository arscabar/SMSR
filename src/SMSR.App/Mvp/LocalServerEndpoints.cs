using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using ModelContextProtocol.Server;

namespace SMSR.App.Mvp;

internal static class LocalServerEndpoints
{
    public static void Map(WebApplication app, LocalOAuthStore oauth, McpBridgeToken bridgeToken, OAuthFlowStore flows,
        OAuthAuditLog audit, McpConnectionTracker connections, WorkflowEventNotifier notifier, ActivityJsonlStore activity,
        ActivityHookToken activityToken, OperatorInstructionQueue operatorInstructions, Func<string>? dashboardTheme)
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
            if (context.Request.Path.StartsWithSegments("/assets")) context.Response.Headers.CacheControl = "no-store";
            await next();
        });
        OAuthEndpoints.Map(app, oauth, flows, audit);
        ActivityEndpoints.Map(app, activity, activityToken, notifier);
        GraphEndpoints.Map(app, dashboardTheme);
        GraphQuestionEndpoints.Map(app);
        app.MapGet("/assets/vis-network-9.1.6.min.js",()=>Results.File(Path.Combine(AppContext.BaseDirectory,"WebAssets","vis-network-9.1.6.min.js"),"text/javascript"));
        app.MapGet("/assets/graph-explorer-knowledge.js",()=>Results.File(Path.Combine(AppContext.BaseDirectory,"WebAssets","graph-explorer-knowledge.js"),"text/javascript"));
        app.MapGet("/assets/graph-explorer-media.js",()=>Results.File(Path.Combine(AppContext.BaseDirectory,"WebAssets","graph-explorer-media.js"),"text/javascript"));
        app.MapGet("/assets/graph-explorer-export.js",()=>Results.File(Path.Combine(AppContext.BaseDirectory,"WebAssets","graph-explorer-export.js"),"text/javascript"));
        foreach (var module in new[] { "role-batch", "batch-preview", "sync-http", "vault", "role", "role-job", "role-coverage", "symbols", "related", "network", "svg", "render", "expand", "drag", "edges", "edge-layout", "edge-style", "viewport", "deep", "overview", "overview-svg", "overview-members", "semantic", "structure", "structure-list", "visual-network", "visual-data", "visual-regions", "visual-controls", "visual-info", "visual-analysis", "visual-expand" })
            app.MapGet("/assets/graph-explorer-" + module + ".js", () => Results.File(
                Path.Combine(AppContext.BaseDirectory, "WebAssets", "graph-explorer-" + module + ".js"), "text/javascript"));
        app.MapGet("/assets/thinking-orbs-engine.js", () => Results.File(
            Path.Combine(AppContext.BaseDirectory, "WebAssets", "thinking-orbs-engine.js"), "text/javascript"));
        app.MapGet("/assets/smsr-loading-orb.js", () => Results.File(
            Path.Combine(AppContext.BaseDirectory, "WebAssets", "smsr-loading-orb.js"), "text/javascript"));
        app.MapGet("/assets/smsr-dashboard-disclosures.js", () => Results.File(
            Path.Combine(AppContext.BaseDirectory, "WebAssets", "smsr-dashboard-disclosures.js"), "text/javascript"));
        app.MapGet("/assets/graph-explorer.js", () => Results.File(
            Path.Combine(AppContext.BaseDirectory, "WebAssets", "graph-explorer.js"), "text/javascript"));
        app.MapGet("/assets/graph-explorer-labels.js", () => Results.File(
            Path.Combine(AppContext.BaseDirectory, "WebAssets", "graph-explorer-labels.js"), "text/javascript"));
        app.MapGet("/assets/graph-explorer-evidence.js", () => Results.File(
            Path.Combine(AppContext.BaseDirectory, "WebAssets", "graph-explorer-evidence.js"), "text/javascript"));
        app.MapGet("/assets/graph-explorer-relations.js", () => Results.File(
            Path.Combine(AppContext.BaseDirectory, "WebAssets", "graph-explorer-relations.js"), "text/javascript"));
        app.MapGet("/assets/graph-explorer-trace.js", () => Results.File(
            Path.Combine(AppContext.BaseDirectory, "WebAssets", "graph-explorer-trace.js"), "text/javascript"));
        app.MapGet("/assets/graph-explorer-trace-result.js", () => Results.File(
            Path.Combine(AppContext.BaseDirectory, "WebAssets", "graph-explorer-trace-result.js"), "text/javascript"));
        app.MapGet("/assets/graph-explorer-impact.js", () => Results.File(
            Path.Combine(AppContext.BaseDirectory, "WebAssets", "graph-explorer-impact.js"), "text/javascript"));
        app.MapGet("/assets/graph-explorer-diagnostics.js", () => Results.File(
            Path.Combine(AppContext.BaseDirectory, "WebAssets", "graph-explorer-diagnostics.js"), "text/javascript"));
        app.MapGet("/assets/graph-explorer-flow.js", () => Results.File(
            Path.Combine(AppContext.BaseDirectory, "WebAssets", "graph-explorer-flow.js"), "text/javascript"));
        app.MapGet("/api/health", () => Results.Ok(new { service = "SMSR", status = "ready" }));
        app.MapPost("/api/mcp-bridge/connected", (HttpRequest request) =>
        {
            if (!bridgeToken.Validate(request.Headers["X-SMSR-Bridge-Token"].ToString()))
                return Results.Unauthorized();
            connections.MarkActivity();
            return Results.NoContent();
        });
        app.MapGet("/api/state", (string? projectId, string? workflowId, EventStore events, CancellationToken ct) => GetStateAsync(projectId, workflowId, events, ct));
        app.MapGet("/api/plan", (string? projectId, string? workflowId, EventStore events, CancellationToken ct) => GetPlanAsync(projectId, workflowId, events, ct));
        app.MapGet("/api/plan-revisions", (string? projectId, string? workflowId, EventStore events, CancellationToken ct) => GetPlanRevisionsAsync(projectId, workflowId, events, ct));
        app.MapGet("/api/timeline", (string? projectId, string? workflowId, EventStore events, CancellationToken ct) => GetTimelineAsync(projectId, workflowId, events, ct));
        app.MapGet("/api/context", (string? projectId, string? workflowId, EventStore events, CancellationToken ct) => GetContextAsync(projectId, workflowId, events, ct));
        app.MapPost("/api/context", async (WorkflowContextEditRequest request, HttpRequest http,
            EventStore events, WorkflowEventNotifier updates, CancellationToken ct) =>
        {
            if (!SameOrigin(http)) return Results.Unauthorized();
            var context = new WorkflowContext(request.ProjectId, request.WorkflowId,
                request.Reason?.Trim(), request.Approach?.Trim(), request.Result?.Trim(), DateTimeOffset.UtcNow);
            if (EventValidation.Validate(context) is { } error) return Results.BadRequest(new { error });
            if (string.IsNullOrWhiteSpace(context.Reason) && string.IsNullOrWhiteSpace(context.Approach)
                && string.IsNullOrWhiteSpace(context.Result)) return Results.BadRequest(new { error = "입력한 내용이 없습니다." });
            if ((await events.GetPlanAsync(request.ProjectId, request.WorkflowId, ct)).Nodes.Count == 0)
                return Results.NotFound(new { error = "작업 그래프가 없습니다." });
            await events.SaveWorkflowContextAsync(context, ct);
            updates.Publish(request.ProjectId, request.WorkflowId);
            return Results.Ok(await events.GetWorkflowContextAsync(request.ProjectId, request.WorkflowId, ct));
        });
        app.MapGet("/api/summary", (string? projectId, string? workflowId, EventStore events, CancellationToken ct) => GetSummaryAsync(projectId, workflowId, events, ct));
        app.MapPost("/api/operator-instruction", async (OperatorInstructionRequest request, HttpRequest http,
            EventStore events, CancellationToken ct) =>
        {
            if (!SameOrigin(http)) return Results.Unauthorized();
            if (EventValidation.ValidateWorkflowIds(request.ProjectId, request.WorkflowId) is { } idError)
                return Results.BadRequest(new { error = idError });
            if (string.IsNullOrWhiteSpace(request.NodeId) || request.NodeId.Length > 128)
                return Results.BadRequest(new { error = "nodeId는 1~128자여야 합니다." });
            if (request.Action is not ("resume" or "accelerate" or "redesign"))
                return Results.BadRequest(new { error = "지원하지 않는 작업 요청입니다." });
            var state = await events.GetStateAsync(request.ProjectId, request.WorkflowId, ct);
            var activeNode = state.Nodes.Any(node => node.NodeId == request.NodeId
                && node.Status is "IN_PROGRESS" or "VALIDATING" or "RETRYING");
            var activeAgent = (state.Agents ?? []).Any(agent => agent.NodeId == request.NodeId
                && agent.Status == "ACTIVE" && !agent.IsStale);
            if (!activeNode || !activeAgent) return Results.Conflict(new { error = "현재 Codex가 작업 중인 노드가 아닙니다." });
            var queued = operatorInstructions.Set(request.ProjectId, request.WorkflowId, request.NodeId, request.Action);
            return Results.Accepted(value: new { status = "queued", queued.Action });
        });
        app.MapGet("/api/daily-activities", async (DateTimeOffset? startUtc, DateTimeOffset? endUtc,
            EventStore events, CancellationToken ct) => startUtc is null || endUtc is null
                ? Results.BadRequest(new { error = "startUtc와 endUtc가 필요합니다." })
                : Results.Ok(await events.GetDailyActivitiesAsync(startUtc.Value, endUtc.Value, ct)));
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
            var context = await events.GetWorkflowContextAsync(projectId, workflowId, ct);
            var recent = await events.GetRecentEventsAsync(projectId, workflowId, selectedNodeId, ct);
            var revisions = await events.GetPlanRevisionsAsync(projectId, workflowId, ct);
            var timeline = await events.GetTimelineEventsAsync(projectId, workflowId, ct: ct);
            var evidence = await events.GetEvidenceAsync(projectId, workflowId, ct);
            var eventCount = await events.GetWorkflowEventCountAsync(projectId, workflowId, ct);
            return Results.Content(DashboardPage.Render(state, plan, recent, dashboardTheme?.Invoke(), parentNodeId,
                selectedNodeId, activity.ReadLatest(projectId, workflowId), activity.ReadTokenUsage(projectId, workflowId), context,
                revisions, timeline, evidence, eventCount),
                "text/html; charset=utf-8");
        });
        app.MapMcp("/mcp");
    }

    internal static bool SameOrigin(HttpRequest request)
        => Uri.TryCreate(request.Headers.Origin.ToString(), UriKind.Absolute, out var origin)
            && origin.Host == "127.0.0.1" && origin.Port == request.Host.Port;

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

    private static async Task<IResult> GetPlanRevisionsAsync(string? projectId, string? workflowId, EventStore events, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(projectId) || string.IsNullOrWhiteSpace(workflowId))
            return Results.BadRequest(new { error = "projectId와 workflowId가 필요합니다." });
        return Results.Ok(await events.GetPlanRevisionsAsync(projectId, workflowId, ct));
    }

    private static async Task<IResult> GetTimelineAsync(string? projectId, string? workflowId, EventStore events, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(projectId) || string.IsNullOrWhiteSpace(workflowId))
            return Results.BadRequest(new { error = "projectId와 workflowId가 필요합니다." });
        var plan = await events.GetPlanAsync(projectId, workflowId, ct);
        var revisions = await events.GetPlanRevisionsAsync(projectId, workflowId, ct);
        var entries = await events.GetTimelineEventsAsync(projectId, workflowId, 1000, ct);
        var count = await events.GetWorkflowEventCountAsync(projectId, workflowId, ct);
        var evidence = await events.GetEvidenceAsync(projectId, workflowId, ct);
        return Results.Ok(new { revisions, events = entries, eventCount = count, truncated = count > entries.Count,
            evidence, integrityIssues = WorkflowIntegrity.Find(plan) });
    }

    private static async Task<IResult> GetContextAsync(string? projectId, string? workflowId, EventStore events, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(projectId) || string.IsNullOrWhiteSpace(workflowId))
            return Results.BadRequest(new { error = "projectId와 workflowId가 필요합니다." });
        return Results.Ok(await events.GetWorkflowContextAsync(projectId, workflowId, ct));
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
