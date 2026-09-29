namespace SMSR.App.Mvp;

public sealed partial class GraphQueryService
{
    public async Task<GraphRouteMap> RoutesAsync(string projectId, CancellationToken ct = default)
    {
        var info = await RequireInfoAsync(projectId, ct);
        var routes = await store.GetGraphRoutesAsync(projectId, 201, ct, info.Revision);
        return new(info.Revision, routes.Take(200).ToArray(), routes.Count > 200);
    }
}
