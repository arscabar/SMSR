namespace SMSR.App.Mvp;

public sealed record GraphRoleCoverageFile(GraphNode Node, string Status, string? JobStatus);
public sealed record GraphRoleCoverage(int Revision, int TotalFiles, int Current, int Missing, int Stale,
    int Queued, int Running, int Failed, IReadOnlyList<GraphRoleCoverageFile> Files, bool Truncated);

public sealed class GraphRoleCoverageService(EventStore store, GraphRoleService roles)
{
    public async Task<GraphRoleCoverage> ReadAsync(string projectId, string? status = null,
        int offset = 0, CancellationToken ct = default)
    {
        if (EventValidation.ValidateWorkflowIds(projectId, "role") is { } error
            || status is not (null or "CURRENT" or "MISSING" or "STALE") || offset is < 0 or > 1_000_000)
            throw new ArgumentException("설명 현황 조회 조건이 올바르지 않습니다.");
        var info = await store.GetGraphInfoAsync(projectId, ct) ?? throw new KeyNotFoundException("관계 색인이 없습니다.");
        var jobs = (await store.GraphRoleJobsAsync(projectId, ct)).ToDictionary(j => j.NodeId);
        var files = new List<GraphRoleCoverageFile>();
        // ponytail: validate saved explanations only; cache validation after measuring a large explained corpus.
        foreach (var (node, saved) in await store.GraphRoleFilesAsync(projectId, info.Revision, ct))
        {
            var state = "MISSING";
            if (saved) try { state = (await roles.ReadAsync(projectId, node.NodeId, ct)).Status; }
                catch (Exception failure) when (failure is ArgumentException or InvalidOperationException or KeyNotFoundException or System.IO.IOException)
                { state = "STALE"; }
            files.Add(new(node, state, jobs.GetValueOrDefault(node.NodeId)?.Status));
        }
        if ((await store.GetGraphInfoAsync(projectId, ct))?.Revision != info.Revision)
            throw new InvalidOperationException("색인이 변경됐습니다. 다시 확인하세요.");
        var selected = files.Where(f => status is null || f.Status == status).Skip(offset).Take(51).ToArray();
        return new(info.Revision, files.Count, files.Count(f => f.Status == "CURRENT"), files.Count(f => f.Status == "MISSING"),
            files.Count(f => f.Status == "STALE"), jobs.Values.Count(j => j.Status == "QUEUED"),
            jobs.Values.Count(j => j.Status == "RUNNING"), jobs.Values.Count(j => j.Status == "FAILED"),
            selected.Take(50).ToArray(), selected.Length > 50);
    }
}
