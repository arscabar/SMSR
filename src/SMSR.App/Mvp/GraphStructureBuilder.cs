namespace SMSR.App.Mvp;

internal static class GraphStructureBuilder
{
    internal static GraphStructurePage Build(int revision,string path,GraphNode[] nodes,GraphEdge[] edges)
    {
        path=GraphStructurePaths.Normalize(path);
        var file=path.Length>0&&nodes.Any(n=>GraphStructurePaths.Normalize(n.SourcePath)==path);
        var within=nodes.Where(n=>GraphStructurePaths.Inside(GraphStructurePaths.Normalize(n.SourcePath),path,file)).ToArray();
        if(path.Length>0&&within.Length==0)throw new KeyNotFoundException("선택한 경로가 색인에 없습니다.");
        var inside=within.Select(n=>n.NodeId).ToHashSet(StringComparer.Ordinal);
        var files=nodes.Where(n=>n.NodeId.StartsWith("file:",StringComparison.Ordinal))
            .GroupBy(n=>GraphStructurePaths.Normalize(n.SourcePath)).ToDictionary(g=>g.Key,g=>g.First());
        var buckets=nodes.ToDictionary(n=>n.NodeId,n=>GraphStructurePaths.Bucket(n,path,file,inside.Contains(n.NodeId)));
        foreach(var id in buckets.Keys.ToArray())
            if(buckets[id].Id.StartsWith("file:",StringComparison.Ordinal)&&files.TryGetValue(buckets[id].Path,out var original))
                buckets[id]=buckets[id] with{Node=original,Kind=original.Kind};
        GraphStructureItem[] Items(IEnumerable<GraphNode> source)=>source.GroupBy(n=>buckets[n.NodeId].Id)
            .Select(g=>buckets[g.First().NodeId] with{Nodes=g.Count(),Files=g.Select(n=>GraphStructurePaths.Normalize(n.SourcePath)).Where(p=>p.Length>0).Distinct().Count(),
                Node=g.FirstOrDefault(n=>n.NodeId==g.Key)??buckets[g.First().NodeId].Node,
                Kind=g.FirstOrDefault(n=>n.NodeId==g.Key)?.Kind??buckets[g.First().NodeId].Kind})
            .OrderBy(g=>g.Kind=="folder"?0:1).ThenBy(g=>g.Label,StringComparer.Ordinal).ThenBy(g=>g.Id,StringComparer.Ordinal).ToArray();
        var connected=edges.Where(e=>inside.Contains(e.SourceId)||inside.Contains(e.TargetId)).ToArray();
        var links=GraphStructureLinks.Build(connected,buckets);
        var outsideIds=connected.SelectMany(e=>new[]{e.SourceId,e.TargetId}).Where(id=>!inside.Contains(id)).ToHashSet();
        return new(revision,path,file,Items(within),Items(nodes.Where(n=>outsideIds.Contains(n.NodeId))),links.Items,
            within.Length,within.Select(n=>GraphStructurePaths.Normalize(n.SourcePath)).Where(p=>p.Length>0).Distinct().Count(),links.Contained,
            connected.Count(e=>inside.Contains(e.SourceId)!=inside.Contains(e.TargetId)),links.Omitted);
    }
}
