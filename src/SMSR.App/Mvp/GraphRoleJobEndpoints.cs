using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace SMSR.App.Mvp;

internal static class GraphRoleJobEndpoints
{
    internal static void Map(WebApplication app)
    {
        GraphRoleBatchEndpoints.Map(app);
        GraphVaultEndpoints.Map(app);
        app.MapGet("/api/graph/role-coverage", (string projectId, string? status, int? offset,
            GraphRoleCoverageService service, CancellationToken ct)
            => GraphExplorerEndpoints.Reply(() => service.ReadAsync(projectId, status, offset ?? 0, ct)));
        app.MapGet("/api/graph/role-job", (string projectId, string nodeId, GraphRoleJobs jobs, CancellationToken ct)
            => GraphExplorerEndpoints.Reply(async () => await jobs.ReadAsync(projectId, nodeId, ct)
                ?? new GraphRoleJob("", nodeId, "", 0, "NOT_REQUESTED", 0, DateTimeOffset.UnixEpoch)));
        app.MapPost("/api/graph/role-job", (GraphRoleJobRequest request, HttpRequest http,
            GraphRoleJobs jobs, CancellationToken ct) => !LocalServerEndpoints.SameOrigin(http)
                ? Task.FromResult<IResult>(Results.Unauthorized())
                : GraphExplorerEndpoints.Reply(() => jobs.RequestAsync(request.ProjectId, request.NodeId, request.Retry, ct)));
    }
}
