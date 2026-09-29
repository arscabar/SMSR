namespace SMSR.CSharpAnalysis;

internal static class ControlFacts
{
    internal static ControlDependence Read(FlowBlock[] blocks, FlowRegion[] regions,
        bool valid, string returnKind, ref int budget)
    {
        if (!valid) return ControlDependence.Unavailable("COMPILATION_ERRORS");
        if (blocks.Length == 0) return ControlDependence.Unavailable("NO_CFG");
        if (returnKind is "ASYNC_RESULT" or "ITERATOR_RESULT")
            return ControlDependence.Unavailable("SUSPENSION");
        if (regions.Any(r => r.Kind is not ("Root" or "LocalLifetime")))
            return ControlDependence.Unavailable("EXCEPTION_OR_SPECIAL_REGION");
        var (edges, error) = ControlGraph.Read(blocks);
        if (error is not null) return ControlDependence.Unavailable(error);
        var ids = blocks.Where(b => b.Reachable).Select(b => b.Ordinal).ToArray();
        var sets = ControlPostdominators.Solve(ids, edges, ref budget);
        var immediate = new List<Postdominator>();
        foreach (var id in ids)
        {
            ControlPostdominators.Take(ref budget, sets[id].Count);
            immediate.Add(new(id, sets[id].Where(n => n != id).MaxBy(n => sets[n].Count)));
        }
        var links = new List<ControlLink>();
        foreach (var edge in edges)
        {
            ControlPostdominators.Take(ref budget, sets[edge.Target].Count);
            foreach (var dependent in sets[edge.Target].Order())
                if (dependent != -1 && (dependent == edge.Source || !sets[edge.Source].Contains(dependent)))
                {
                    links.Add(new(edge.Source, edge.Target, dependent, edge.Outcome));
                    if (links.Count > 20_000) throw new ArgumentException("Control dependence link limit exceeded");
                }
        }
        return new("NORMAL_CFG_CONTROL_DEPENDENCE", -1, immediate.ToArray(), links.ToArray(),
            ["TERMINATING_PATH_POSTDOMINANCE", "IMPLICIT_EXCEPTIONS_UNMODELED", "PATH_CONDITIONS_UNMODELED", "NOT_TAINT"]);
    }
}
