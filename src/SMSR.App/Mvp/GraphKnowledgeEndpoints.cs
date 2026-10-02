using Microsoft.AspNetCore.Builder;

namespace SMSR.App.Mvp;

internal static class GraphKnowledgeEndpoints
{
    internal static void Map(WebApplication app)
    {
        app.MapGet("/api/graph/hyperedges", (string projectId, string? nodeId, int? offset,
            int? limit, int? revision, GraphQueryService query, CancellationToken ct)
            => GraphExplorerEndpoints.Reply(() => query.HyperedgesAsync(projectId, nodeId,
                offset ?? 0, limit ?? 20, revision, ct)));
        app.MapGet("/api/graph/coverage", (string projectId, int? offset, int? limit,
            GraphQueryService query, CancellationToken ct)
            => GraphExplorerEndpoints.Reply(() => query.CoverageAsync(projectId, offset ?? 0, limit ?? 50, ct)));
    }
}
