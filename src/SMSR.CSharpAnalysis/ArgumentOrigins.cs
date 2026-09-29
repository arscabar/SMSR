namespace SMSR.CSharpAnalysis;

internal static class ArgumentOrigins
{
    internal static ArgumentFlow Read(ReturnContext context, IEnumerable<CallConnection> bindings,
        ref int budget, ref int evidenceBudget)
    {
        if (context.Values.Status == "UNAVAILABLE") return new("UNAVAILABLE", [], context.Values.Limitations);
        var calls = bindings.ToLookup(c => c.Source);
        var origins = new List<ArgumentOrigin>();
        foreach (var port in context.Values.Ports.Where(p => p.Kind == "CALL_ARGUMENT"))
        {
            Take(ref evidenceBudget);
            var steps = new Dictionary<int, ValueLink>();
            var reach = ReturnTraversal.Read(context, [port.Value], ref budget, steps);
            var dependencies = new List<ArgumentDependency>();
            foreach (var entry in context.Entries.Where(e => reach.Parameters.Contains(e.Value)).OrderBy(e => e.Value))
            {
                Take(ref evidenceBudget);
                var path = new List<ValueLink>(); var id = entry.Key;
                while (id != port.Value)
                {
                    ReturnTraversal.Take(ref budget); Take(ref evidenceBudget);
                    var edge = steps[id]; path.Add(edge); id = edge.Target;
                }
                dependencies.Add(new(entry.Value, entry.Key, path.ToArray()));
            }
            var source = port.CallSource ?? throw new InvalidOperationException("Missing argument call source");
            var candidates = calls[source].Where(c => c.TargetId == port.TargetId).ToArray();
            origins.Add(new(port.Source, source, port.TargetId, port.ParameterOrdinal, port.Value,
                candidates.Length == 1 ? candidates[0].Status : "NO_EXACT_CALL_BINDING",
                reach.Uncertain, dependencies.ToArray()));
        }
        return new("MODELED_ARGUMENT_ORIGINS", origins.ToArray(),
            ["ONE_WITNESS_PER_INPUT", "CALLER_VALUE_IDS", "RETURN_EDGES_USE_CALLEE_SUMMARIES",
             "DATA_NOT_CONTROL_OR_HEAP", "NO_SINK_OR_SANITIZER_CLASSIFICATION", "NOT_EXECUTION_OR_SAFETY_PROOF"]);
    }

    private static void Take(ref int budget)
    {
        if (--budget < 0) throw new ArgumentException("Argument evidence limit exceeded");
    }
}
