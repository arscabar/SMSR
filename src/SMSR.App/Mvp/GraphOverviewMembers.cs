namespace SMSR.App.Mvp;

public sealed partial class GraphOverviewService
{
    private static GraphOverview RestoreEvidence(GraphOverview result,IReadOnlyList<GraphEdge> edges)
    {
        var original=edges.ToDictionary(e=>(e.SourceId,e.TargetId,e.Relation,e.OwnerPath,e.SourceLine));
        return result with {Surprises=result.Surprises.Select(s=>s with {Edge=original.TryGetValue(
            (s.Edge.SourceId,s.Edge.TargetId,s.Edge.Relation,s.Edge.OwnerPath,s.Edge.SourceLine),out var edge)?edge:
            throw new InvalidOperationException("요약이 존재하지 않는 관계를 반환했습니다.")}).ToArray()};
    }
    public async Task<GraphOverviewMembers> MembersAsync(string projectId, int groupId, int expectedRevision,
        int offset = 0, int limit = 50, CancellationToken ct = default)
    {
        Bounds(offset, limit); var summary = await SummaryAsync(projectId, ct);
        if (expectedRevision != summary.Revision) throw new InvalidOperationException("그룹 리비전이 변경되었습니다. 다시 조회하세요.");
        var group = summary.Groups.FirstOrDefault(g => g.Id == groupId) ?? throw new KeyNotFoundException("그룹이 없습니다.");
        var nodes = await store.GetGraphNodesAsync(projectId, group.MemberIds.Skip(offset).Take(limit).ToArray(), ct, summary.Revision);
        return new(summary.Revision, groupId, group.NodeCount, nodes.ToArray(), offset + limit < group.NodeCount);
    }
}
