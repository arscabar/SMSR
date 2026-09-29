namespace SMSR.CSharpAnalysis;

internal static class ReturnSummaries
{
    internal static ReturnSummary[] Read(FunctionFlow[] functions, CallConnection[] connections, int budget = 2_000_000)
    {
        var contexts = functions.Select(f => new ReturnContext(f)).ToArray();
        ReturnSites.Bind(contexts, connections);
        var changed = true;
        // ponytail: bounded bundle-wide fixed points; use a dependency worklist if profiling requires it.
        while (changed)
        {
            changed = false;
            foreach (var c in contexts)
            {
                ReturnTraversal.Take(ref budget);
                var reach = ReturnTraversal.Read(c, c.Values.Ports.Where(p => p.Kind == "RETURN").Select(p => p.Value), ref budget);
                foreach (var parameter in reach.Parameters) changed |= c.Parameters.Add(parameter);
            }
        }
        var reaches = contexts.Select(c => ReturnTraversal.Read(c,
            c.Values.Ports.Where(p => p.Kind == "RETURN").Select(p => p.Value), ref budget)).ToArray();
        // Resolve uncertainty only after input dependencies stabilize. Recursive value cycles stay unknown.
        changed = true;
        while (changed)
        {
            changed = false;
            for (var i = 0; i < contexts.Length; i++)
            {
                ReturnTraversal.Take(ref budget);
                foreach (var call in reaches[i].Calls) ReturnTraversal.Take(ref budget);
                if (contexts[i].Available && contexts[i].Uncertain && !reaches[i].Uncertain)
                { contexts[i].Uncertain = false; changed = true; }
            }
        }
        var bindings = connections.ToLookup(c => c.CallerId);
        var evidenceBudget = 20_000;
        return contexts.Select(c => ReturnOutput.Read(c, bindings[c.Function.SymbolId], ref budget, ref evidenceBudget)).ToArray();
    }
}
