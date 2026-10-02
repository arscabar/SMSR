namespace SMSR.App.Mvp;

internal static class GraphStructureLinks
{
    internal static (GraphStructureLink[] Items,int Contained,int Omitted) Build(
        GraphEdge[] edges,Dictionary<string,GraphStructureItem> buckets)
    {
        if(edges.Any(e=>!buckets.ContainsKey(e.SourceId)||!buckets.ContainsKey(e.TargetId)))
            throw new InvalidOperationException("끝점 없는 관계는 구조도로 집계할 수 없습니다.");
        var contained=edges.Count(e=>buckets[e.SourceId].Id==buckets[e.TargetId].Id);
        var groups=edges.Where(e=>buckets[e.SourceId].Id!=buckets[e.TargetId].Id)
            .GroupBy(e=>(Source:buckets[e.SourceId].Id,Target:buckets[e.TargetId].Id,e.Relation,e.Confidence,e.Resolution))
            .Select(g=>new GraphStructureLink(g.Key.Source,g.Key.Target,g.Key.Relation,g.Key.Confidence,g.Key.Resolution,
                g.Count(),g.Take(3).ToArray())).OrderByDescending(g=>g.Count)
            .ThenBy(g=>g.SourceId,StringComparer.Ordinal).ThenBy(g=>g.TargetId,StringComparer.Ordinal)
            .ThenBy(g=>g.Relation,StringComparer.Ordinal).ToArray();
        // ponytail: bound the visual payload; original relations remain in the existing query API.
        return (groups.Take(2000).ToArray(),contained,Math.Max(0,groups.Length-2000));
    }
}
