namespace SMSR.App.Mvp;

public sealed partial class GraphQueryService
{
    public async Task<GraphScopeExport> ExportScopeAsync(string projectId, string nodeId,
        int depth = 2, string direction = "both", CancellationToken ct = default)
    {
        var info = await RequireInfoAsync(projectId, ct);
        if (string.IsNullOrWhiteSpace(nodeId) || nodeId.Length > 1024 || depth is < 1 or > 3
            || direction is not ("out" or "in" or "both"))
            throw new ArgumentException("선택 범위는 노드 ID, 깊이 1~3, 방향 out/in/both를 지정해야 합니다.");
        if (await store.GetGraphNodeAsync(projectId, nodeId, ct, info.Revision) is null)
            throw new KeyNotFoundException("색인에 해당 노드가 없습니다.");
        var visited = new HashSet<string>(StringComparer.Ordinal) { nodeId };
        var edges = new HashSet<GraphEdge>();
        var frontier = new List<string> { nodeId };
        var truncated = false;
        for (var level = 0; level < depth && frontier.Count > 0 && !truncated; level++)
        {
            var next = new List<string>();
            var candidates = new List<GraphEdge>();
            if (direction is "out" or "both")
                candidates.AddRange(await store.GetGraphAdjacentAsync(projectId, frontier, false, 501, ct, info.Revision));
            if (direction is "in" or "both")
                candidates.AddRange(await store.GetGraphAdjacentAsync(projectId, frontier, true, 501, ct, info.Revision));
            foreach (var edge in candidates.Distinct())
            {
                if (edges.Contains(edge)) continue;
                if (edges.Count >= 500) { truncated = true; break; }
                var other = frontier.Contains(edge.SourceId) ? edge.TargetId : edge.SourceId;
                if (!visited.Contains(other) && visited.Count >= 200) { truncated = true; break; }
                edges.Add(edge);
                if (visited.Add(other)) next.Add(other);
            }
            if (candidates.Count >= 501) truncated = true;
            frontier = next;
        }
        var nodes = await store.GetGraphNodesAsync(projectId, visited.ToArray(), ct, info.Revision);
        return new(projectId, info.Revision, nodeId, direction, depth,
            nodes.OrderBy(node => node.NodeId, StringComparer.Ordinal).ToArray(),
            edges.OrderBy(edge => edge.OwnerPath, StringComparer.Ordinal).ThenBy(edge => edge.SourceLine).ToArray(), truncated);
    }
}
