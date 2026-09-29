namespace SMSR.CSharpAnalysis;

internal static class ReachingDefinitions
{
    internal static DefinitionLink[] Solve(FlowBlock[] blocks, VariableSite[] sites, ref int budget)
    {
        var live = blocks.Where(b => b.Reachable).ToArray();
        var byBlock = sites.GroupBy(s => s.Block).ToDictionary(g => g.Key, g => g.ToArray());
        var outputs = live.ToDictionary(b => b.Ordinal, _ => new HashSet<int>());
        var parents = live.ToDictionary(b => b.Ordinal, _ => new List<int>());
        foreach (var b in live)
            foreach (var target in new[] { b.FallThrough, b.Conditional }.Where(e => e is { ConstantExcluded: false })
                .Select(e => e!.Destination).Distinct())
                if (target is { } id && parents.TryGetValue(id, out var list)) list.Add(b.Ordinal);
        var entries = sites.Where(s => s.Kind == "ENTRY").Select(s => s.Id).ToArray();
        var links = new HashSet<DefinitionLink>();
        // ponytail: bounded fixed-point scan; use a worklist if large CFG measurements require it.
        bool changed;
        do
        {
            changed = false;
            foreach (var b in live)
            {
                Take(ref budget, 1);
                var state = new HashSet<int>();
                if (b.Kind == "Entry") state.UnionWith(entries);
                foreach (var parent in parents[b.Ordinal])
                {
                    Take(ref budget, outputs[parent].Count);
                    state.UnionWith(outputs[parent]);
                }
                foreach (var site in byBlock.GetValueOrDefault(b.Ordinal) ?? [])
                {
                    Take(ref budget, state.Count + 1);
                    var matching = state.Where(id => sites[id].SymbolId == site.SymbolId).ToArray();
                    if (site.Kind == "READ")
                        foreach (var id in matching) links.Add(new(id, site.Id));
                    else { state.ExceptWith(matching); state.Add(site.Id); }
                    if (links.Count > 20_000) throw new ArgumentException("Definition link limit exceeded");
                }
                if (!state.SetEquals(outputs[b.Ordinal])) { outputs[b.Ordinal] = state; changed = true; }
            }
        } while (changed);
        return links.OrderBy(l => l.Read).ThenBy(l => l.Definition).ToArray();
    }

    private static void Take(ref int budget, int count)
    {
        budget -= count;
        if (budget < 0) throw new ArgumentException("Definition work limit exceeded");
    }
}
