using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
namespace SMSR.App.Mvp;

internal static class GraphRoleBatchEndpoints
{
    internal static void Map(WebApplication app)
    {
        app.MapPost("/api/graph/role-batch-preview", (GraphRoleBatchRequest r, HttpRequest h,
            GraphRoleBatchService service, CancellationToken ct) => !LocalServerEndpoints.SameOrigin(h)
                ? Task.FromResult<IResult>(Results.Unauthorized())
                : GraphExplorerEndpoints.Reply(() => service.PreviewAsync(r.ProjectId, r.Folders, ct)));
        app.MapPost("/api/graph/role-batch", (GraphRoleBatchRequest r, HttpRequest h,
            GraphRoleBatchService service, CancellationToken ct) => !LocalServerEndpoints.SameOrigin(h)
                ? Task.FromResult<IResult>(Results.Unauthorized())
                : GraphExplorerEndpoints.Reply(() => service.StartAsync(r, ct)));
        app.MapGet("/api/graph/role-batch", (string projectId, GraphRoleBatchService service, CancellationToken ct)
            => GraphExplorerEndpoints.Reply(() => service.StatusAsync(projectId, ct)));
        app.MapPost("/api/graph/role-batch-control", (GraphRoleBatchControl r, HttpRequest h,
            GraphRoleBatchService service, CancellationToken ct) => !LocalServerEndpoints.SameOrigin(h)
                ? Task.FromResult<IResult>(Results.Unauthorized())
                : GraphExplorerEndpoints.Reply(() => service.ControlAsync(r.ProjectId, r.Action, ct)));
    }
}
