namespace SMSR.CSharpAnalysis;

internal static class ReturnSites
{
    internal static void Bind(ReturnContext[] contexts, CallConnection[] connections)
    {
        var bodies = contexts.ToLookup(c => c.Function.SymbolId);
        var bindings = connections.ToLookup(c => (c.CallerId, c.Source));
        foreach (var context in contexts)
        {
            var arguments = context.Values.Ports.Where(p => p.Kind == "CALL_ARGUMENT")
                .ToLookup(p => (p.CallSource, p.TargetId));
            foreach (var output in context.Values.Ports.Where(p => p.Kind == "CALL_RESULT"))
            {
                var matches = bindings[(context.Function.SymbolId, output.Source)].ToArray();
                var binding = matches.Length == 1 ? matches[0] : null;
                var targets = output.TargetId is null ? [] : bodies[output.TargetId].ToArray();
                var target = targets.Length == 1 ? targets[0] : null;
                var status = binding?.Status ?? "NO_EXACT_CALL_BINDING";
                if (status == "BOUNDARY_LINKED")
                {
                    status = binding!.TargetId != output.TargetId ? "TARGET_MISMATCH" :
                        target?.Available != true ? "UNAVAILABLE_RETURN_BODY" :
                        binding.Inputs.Any(i => i.Relation != "ARGUMENT_PARAMETER" ||
                            i.Argument.InConversion.MethodId is not null) ? "UNSUPPORTED_ARGUMENT" : "RETURN_LINKED";
                }
                var slots = arguments[(output.Source, output.TargetId)].ToArray();
                var map = new Dictionary<int, int>();
                foreach (var slot in slots)
                    if (slot.ParameterOrdinal is not { } ordinal || !map.TryAdd(ordinal, slot.Value))
                        status = "ARGUMENT_PORT_MISMATCH";
                if (status == "RETURN_LINKED" && (target!.Function.Boundary.Parameters.Length != map.Count ||
                    target.Function.Boundary.Parameters.Any(p => !map.ContainsKey(p.Ordinal))))
                    status = "ARGUMENT_PORT_MISMATCH";
                context.Calls.Add(output.Value, new(output, status == "RETURN_LINKED" ? target : null, status, map));
            }
        }
    }
}
