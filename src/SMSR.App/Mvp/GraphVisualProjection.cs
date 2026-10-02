namespace SMSR.App.Mvp;
internal static class GraphVisualProjection
{
    internal static GraphVisualSnapshot Build(GraphOverview s,GraphNode[] selected,IReadOnlyList<GraphEdge> edges,
        Dictionary<string,int> membership,Dictionary<int,GraphOverviewGroup> groups,bool meta)
    {
        var degree=new Dictionary<string,int>();
        foreach(var e in edges){degree[e.SourceId]=degree.GetValueOrDefault(e.SourceId)+1;degree[e.TargetId]=degree.GetValueOrDefault(e.TargetId)+1;}
        GraphVisualNode Node(GraphNode n){var id=membership.GetValueOrDefault(n.NodeId,-1);var g=groups.GetValueOrDefault(id);
            return new(n.NodeId,n.Label,id,g?.Name??"연결 없는 항목",1,degree.GetValueOrDefault(n.NodeId),g?.Cohesion,n);}
        var candidates=meta?groups.Values.Select(g=>new GraphVisualNode("group:"+g.Id,g.Name,g.Id,g.Name,g.NodeCount,0,g.Cohesion,null)).ToArray():selected.Select(Node).ToArray();
        if(meta&&s.IsolatedNodes>0)candidates=[..candidates,new("group:-1","연결 없는 항목",-1,"연결 없는 항목",s.IsolatedNodes,0,null,null)];
        // ponytail: same 5,000-node ceiling as upstream HTML; omitted nodes remain searchable in the project index.
        var nodes=candidates.OrderByDescending(n=>meta?n.Members:n.Degree).ThenBy(n=>n.Id,StringComparer.Ordinal).Take(5000).ToArray();
        var ids=nodes.Select(n=>n.Id).ToHashSet();
        string Map(string id)=>meta?"group:"+membership.GetValueOrDefault(id,-1):id;
        var relevant=edges.Where(e=>ids.Contains(Map(e.SourceId))&&ids.Contains(Map(e.TargetId))&&(!meta||Map(e.SourceId)!=Map(e.TargetId))).ToArray();
        var links=GraphVisualLinks.Build(relevant,Map);
        if(meta){var counts=links.SelectMany(e=>new[]{e.SourceId,e.TargetId}).GroupBy(id=>id).ToDictionary(g=>g.Key,g=>g.Count());
            nodes=nodes.Select(n=>n with{Degree=counts.GetValueOrDefault(n.Id)}).ToArray();}
        return new(s.Revision,meta?"communities":"nodes",nodes,links.Take(20000).ToArray(),[],s.ScannedNodes,s.ScannedEdges,s.IsolatedNodes,
            candidates.Length-nodes.Length,Math.Max(0,links.Length-20000),0,s.Limitation);
    }
}
