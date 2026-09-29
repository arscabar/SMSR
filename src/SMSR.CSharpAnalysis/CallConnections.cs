namespace SMSR.CSharpAnalysis;

internal static class CallConnections
{
    internal static CallConnection[] Read(IEnumerable<CallBinding> calls, FunctionFlow[] functions)
    {
        var bodies = functions.Where(f => f.Status == "COMPILER_CFG").ToLookup(f => f.SymbolId);
        var results = new List<CallConnection>();
        var budget = 20_000;
        foreach (var call in calls)
        {
            Take(ref budget);
            var candidates = call.TargetId is null ? [] : bodies[call.TargetId].ToArray();
            var status = call.Resolution != "BOUND_IN_BUNDLE" ? call.Resolution :
                call.Boundary.Conditional ? "CONDITIONAL_CALL" : candidates.Length == 1 ? "BOUNDARY_LINKED" :
                candidates.Length > 1 ? "AMBIGUOUS_BODY" : "NO_SOURCE_BODY";
            if (status != "BOUNDARY_LINKED")
            {
                results.Add(new(call.Source, call.CallerId, call.TargetId, status, [], [], null, ["NO_VALUE_PROPAGATION"]));
                continue;
            }
            var body = candidates[0];
            var inputs = new List<ParameterLink>();
            foreach (var argument in call.Arguments)
            {
                Take(ref budget);
                var parameter = body.Boundary.Parameters.SingleOrDefault(p => p.Ordinal == argument.Ordinal)
                    ?? throw new InvalidOperationException("Argument slot mismatch");
                inputs.Add(new(argument, parameter.Id, parameter.Source,
                    argument.RefKind == "None" ? "ARGUMENT_PARAMETER" : "REFERENCE_ALIAS_REQUIRED"));
            }
            var returns = new List<ReturnLink>();
            if (body.Boundary.ReturnKind == "VALUE")
                foreach (var block in body.Blocks.Where(b => b.Reachable &&
                    (b.FallThrough?.Semantics == "Return" || b.Conditional?.Semantics == "Return")))
                    foreach (var value in block.BranchValue.Where(o => o.Parent is null))
                    {
                        Take(ref budget);
                        returns.Add(new(block.Ordinal, value.Source, call.Source, "MAY_RETURN_TO_CALL"));
                    }
            var receiver = call.Boundary.ReceiverSource is { } source && body.Boundary.ReceiverId is { } id
                ? new ReceiverLink(source, id) : null;
            if (receiver is not null) Take(ref budget);
            results.Add(new(call.Source, call.CallerId, call.TargetId, status, inputs.ToArray(), returns.ToArray(), receiver,
                ["BINDINGS_NOT_TAINT", "HEAP_AND_PATH_CONDITIONS_UNMODELED", body.Boundary.ReturnKind]));
        }
        return results.ToArray();
    }

    private static void Take(ref int budget)
    {
        if (--budget < 0) throw new ArgumentException("Call connection limit exceeded");
    }
}
