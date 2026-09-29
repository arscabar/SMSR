namespace SMSR.App.Mvp;

public sealed partial class GraphQueryService
{
    public async Task<GraphCrossRepoMap> CrossRepoAsync(string projectId, CancellationToken ct = default)
    {
        var info = await RequireInfoAsync(projectId, ct);
        var edges = await store.GetCrossRepoEdgesAsync(projectId, ct);
        return new(info.Revision, edges.Take(500).ToArray(), edges.Count > 500,
            !await store.IsCrossRepoCurrentAsync(projectId, info.Revision, ct));
    }
}
