namespace SMSR.App.Mvp;
public sealed partial class GraphOverviewService
{
    public async Task<GraphOverviewPage> PageAsync(string projectId,int offset=0,int limit=12,
        CancellationToken ct=default,string? q=null)
    {
        Bounds(offset,limit);var s=await SummaryAsync(projectId,ct);
        var nodes=(await store.GetGraphRevisionNodesAsync(projectId,s.Revision,ct)).ToDictionary(n=>n.NodeId);
        q=(q??"").Trim();if(q.Length>200)throw new ArgumentException("그룹 검색어는200자 이하로 입력하세요.");
        var groups=s.Groups.Where(g=>q.Length==0||g.MemberIds.Any(id=>nodes.TryGetValue(id,out var n)
            &&(n.Label.Contains(q,StringComparison.OrdinalIgnoreCase)||n.SourcePath.Contains(q,StringComparison.OrdinalIgnoreCase)))).ToArray();
        return new(s.Revision,s.Algorithm,groups.Skip(offset).Take(limit).Select(g=>new GraphOverviewGroupPage(g.Id,g.Name,g.NodeCount,g.Cohesion,g.Representatives,
            g.MemberIds.Select(id=>nodes.GetValueOrDefault(id)?.SourcePath).Where(p=>!string.IsNullOrEmpty(p)).Select(p=>p!).Distinct().Order(StringComparer.Ordinal).Take(3).ToArray())).ToArray(),
            groups.Length,s.Links,s.Core,s.Surprises,s.Questions,s.IsolatedNodes,s.ScannedNodes,s.ScannedEdges,offset+limit<groups.Length||s.LinksTruncated,s.Limitation);
    }
}
