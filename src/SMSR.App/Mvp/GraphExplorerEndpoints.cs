using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace SMSR.App.Mvp;

internal static class GraphExplorerEndpoints
{
    internal static void Map(WebApplication app)
    {
        GraphDeepEndpoints.Map(app);
        GraphKnowledgeEndpoints.Map(app);
        GraphRoleJobEndpoints.Map(app);
        app.MapGet("/api/graph/files", (string projectId, string? q, string? kind, int? offset,
            int? limit, GraphQueryService query, CancellationToken ct)
            => Reply(() => query.FilesAsync(projectId, q ?? "", kind, offset ?? 0, limit ?? 60, ct)));
        app.MapGet("/api/graph/symbols", (string projectId, string nodeId, int? offset, int? revision,
            GraphQueryService query, CancellationToken ct)
            => Reply(() => query.SymbolsAsync(projectId, nodeId, offset ?? 0, revision, ct)));
        app.MapGet("/api/graph/role-context", (string projectId, string nodeId, GraphRoleService service, CancellationToken ct)
            => Reply(() => service.ContextAsync(projectId, nodeId, ct)));
        app.MapGet("/api/graph/role", (string projectId, string nodeId, GraphRoleService service, CancellationToken ct)
            => Reply(() => service.ReadAsync(projectId, nodeId, ct)));
        app.MapPost("/api/graph/role", (GraphRoleRequest request, HttpRequest http, GraphRoleService service, CancellationToken ct)
            => !LocalServerEndpoints.SameOrigin(http) ? Task.FromResult<IResult>(Results.Unauthorized())
                : Reply(() => service.SubmitAsync(request, ct)));
        app.MapGet("/api/graph/visual",(string projectId,int? groupId,int? revision,GraphOverviewService service,CancellationToken ct)
            =>Reply(()=>service.VisualAsync(projectId,groupId,revision,ct)));
        app.MapGet("/api/graph/structure",(string projectId,string? path,int? revision,GraphOverviewService service,CancellationToken ct)
            =>Reply(()=>service.StructureAsync(projectId,path,revision,ct)));
        app.MapGet("/api/graph/relations", (string projectId, string nodeId, string? direction,
            string? relation, int? offset, int? limit, int? revision, GraphQueryService query, CancellationToken ct)
            => Reply(() => query.RelationsAsync(projectId, nodeId, direction ?? "both", relation,
                offset ?? 0, limit ?? 20, revision, ct)));
        app.MapGet("/api/graph/trace", (string projectId, string fromId, string toId,
            string? relation, bool? includeInferred, int? depth, GraphQueryService query, CancellationToken ct)
            => Reply(() => query.TraceAsync(projectId, fromId, toId, relation, includeInferred ?? false, depth ?? 5, ct)));
        app.MapGet("/api/graph/affected", (string projectId, string nodeId,
            string? relation, bool? includeInferred, int? depth, GraphQueryService query, CancellationToken ct)
            => Reply(() => query.ImpactAsync(projectId, nodeId, depth ?? 3, 100, ct, relation, includeInferred ?? false)));
        app.MapGet("/api/graph/explain", (string projectId, string nodeId, GraphQueryService query, CancellationToken ct)
            => Reply(() => query.ExplainAsync(projectId, nodeId, ct)));
        app.MapGet("/api/graph/issues", (string projectId, string? ownerPath, int? offset, EventStore store, CancellationToken ct)
            => Reply(() => store.GraphIssuesAsync(projectId, ownerPath, offset ?? 0, ct)));
    }

    internal static async Task<IResult> Reply<T>(Func<Task<T>> action)
    {
        try { return Results.Ok(await action()); }
        catch (ArgumentException e) { return Results.BadRequest(new { error = e.Message }); }
        catch (KeyNotFoundException e) { return Results.NotFound(new { error = e.Message }); }
        catch (InvalidOperationException e) { return Results.Conflict(new { error = e.Message }); }
    }
}
