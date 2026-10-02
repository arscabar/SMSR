namespace SMSR.App.Mvp;
internal static class GraphVisualLinks
{
    internal static GraphVisualEdge[] Build(IEnumerable<GraphEdge> edges,Func<string,string> map)
        =>edges.GroupBy(e=>(From:map(e.SourceId),To:map(e.TargetId),e.Relation,e.Confidence,e.Resolution))
            .OrderByDescending(g=>g.Count()).ThenBy(g=>g.Key.From,StringComparer.Ordinal).ThenBy(g=>g.Key.To,StringComparer.Ordinal)
            .ThenBy(g=>g.Key.Relation,StringComparer.Ordinal).ThenBy(g=>g.Key.Confidence,StringComparer.Ordinal).ThenBy(g=>g.Key.Resolution,StringComparer.Ordinal)
            .Select((g,i)=>new GraphVisualEdge("edge:"+i,g.Key.From,g.Key.To,g.Key.Relation,g.Key.Confidence,g.Key.Resolution,g.Count(),
                g.Take(3).ToArray())).ToArray();
}
