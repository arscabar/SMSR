namespace SMSR.CSharpAnalysis;

internal static class ReturnOutput
{
    internal static ReturnSummary Read(ReturnContext context, IEnumerable<CallConnection> bindings,
        ref int budget, ref int evidenceBudget)
    {
        var calls = new List<ReturnCall>();
        foreach (var call in context.Calls.Values)
        {
            var reach = ReturnTraversal.Read(context, [call.Port.Value], ref budget);
            var links = new List<ValueLink>();
            foreach (var ordinal in (call.Target?.Parameters ?? []).Order())
            {
                ReturnTraversal.Take(ref budget);
                links.Add(new(call.Arguments[ordinal], call.Port.Value, "ARGUMENT_RETURN_DEPENDENCE"));
            }
            calls.Add(new(call.Port.Source, call.Port.TargetId, call.Port.Value, call.Status, reach.Uncertain, links.ToArray()));
        }
        return new(context.Function.SymbolId, context.Function.Source,
            !context.Available ? "UNAVAILABLE" : context.Uncertain ? "PARTIAL_RETURN_DEPENDENCE" : "MODELED_RETURN_DEPENDENCE",
            context.Parameters.Order().ToArray(), context.Uncertain, calls.ToArray(),
            ["CALL_SITE_SEPARATED_MAY_DATA", "CONTROL_HEAP_AND_PATH_CONDITIONS_UNMODELED",
             "OPAQUE_OR_RECURSIVE_VALUES_REMAIN_UNCERTAIN", "NOT_TERMINATION_OR_TAINT_PROOF"],
            ArgumentOrigins.Read(context, bindings, ref budget, ref evidenceBudget));
    }
}
