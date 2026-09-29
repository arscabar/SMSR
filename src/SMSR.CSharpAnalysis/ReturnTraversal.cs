namespace SMSR.CSharpAnalysis;

internal static class ReturnTraversal
{
    internal static ReturnReach Read(ReturnContext context, IEnumerable<int> roots, ref int budget,
        Dictionary<int, ValueLink>? steps = null)
    {
        var seen = roots.ToHashSet(); var queue = new Queue<int>(seen);
        var parameters = new HashSet<int>(); var calls = new HashSet<ReturnSite>();
        var opaque = context.Values.Status == "UNAVAILABLE";
        while (queue.TryDequeue(out var id))
        {
            Take(ref budget);
            if (context.Entries.TryGetValue(id, out var ordinal)) parameters.Add(ordinal);
            if (context.Calls.TryGetValue(id, out var call))
            {
                calls.Add(call);
                if (call.Target is not null)
                    foreach (var parameter in call.Target.Parameters)
                    {
                        Take(ref budget); var input = call.Arguments[parameter];
                        if (seen.Add(input))
                        {
                            queue.Enqueue(input);
                            steps?.Add(input, new(input, id, "ARGUMENT_RETURN_DEPENDENCE"));
                        }
                    }
            }
            else if (context.Values.Nodes[id].Status != "LOCAL_VALUE") opaque = true;
            foreach (var previous in context.Incoming.GetValueOrDefault(id) ?? [])
            {
                Take(ref budget);
                if (seen.Add(previous.Source))
                {
                    queue.Enqueue(previous.Source); steps?.Add(previous.Source, previous);
                }
            }
        }
        return new(parameters, calls, opaque);
    }

    internal static void Take(ref int budget)
    {
        if (--budget < 0) throw new ArgumentException("Return summary work limit exceeded");
    }
}
