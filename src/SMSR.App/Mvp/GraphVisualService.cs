namespace SMSR.App.Mvp;
public sealed partial class GraphOverviewService
{
    public async Task<GraphVisualSnapshot> VisualAsync(string projectId,int? groupId=null,
        int? expectedRevision=null,CancellationToken ct=default)
    {
        var summary=await SummaryAsync(projectId,ct);
        if(expectedRevision is{}rev&&rev!=summary.Revision)throw new InvalidOperationException("리비전이 변경되었습니다. 전체 구조를 다시 확인하세요.");
        var nodes=await store.GetGraphRevisionNodesAsync(projectId,summary.Revision,ct);
        var edges=await store.GetGraphRevisionEdgesAsync(projectId,summary.Revision,ct);
        var groups=summary.Groups.ToDictionary(g=>g.Id);
        var membership=summary.Groups.SelectMany(g=>g.MemberIds.Select(id=>(id,g.Id))).ToDictionary(p=>p.id,p=>p.Id);
        if(groupId is{}gid&&gid!=-1&&!groups.ContainsKey(gid))throw new KeyNotFoundException("그룹이 없습니다.");
        var meta=groupId is null&&nodes.Count>5000;
        var selected=groupId is{}id?nodes.Where(n=>membership.GetValueOrDefault(n.NodeId,-1)==id).ToArray():nodes.ToArray();
        var projection=GraphVisualProjection.Build(summary,selected,edges,membership,groups,meta);
        var hyper=await store.HyperedgeSliceAsync(projectId,summary.Revision,null,0,1001,ct);
        string? Map(string id)=>meta?membership.TryGetValue(id,out var g)?"group:"+g:"group:-1":id;
        var shown=projection.Nodes.Select(n=>n.Id).ToHashSet();
        var regions=hyper.Take(1000).Select(h=>new GraphVisualHyperedge(h.HyperedgeId,h.Label,
            h.Members.Select(m=>Map(m.NodeId)).OfType<string>().Where(shown.Contains).Distinct().ToArray()))
            .Where(h=>h.Nodes.Length>=2).ToArray();
        if((await store.GetGraphInfoAsync(projectId,ct))?.Revision!=summary.Revision)throw new InvalidOperationException("조회 중 색인이 변경되었습니다. 다시 확인하세요.");
        return projection with{Hyperedges=regions,OmittedHyperedges=Math.Max(0,hyper.Length-1000),
            Core=summary.Core,Surprises=summary.Surprises,Questions=summary.Questions,
            Limitation=summary.Limitation+(groupId is null?"":" 선택 그룹 내부 관계만 표시하며 그룹 밖 연결은 전체 구조에서 확인합니다.")};
    }
}
