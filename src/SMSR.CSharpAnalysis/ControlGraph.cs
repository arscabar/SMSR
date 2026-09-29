namespace SMSR.CSharpAnalysis;

internal static class ControlGraph
{
    internal static (ControlEdge[] Edges, string? Error) Read(FlowBlock[] blocks)
    {
        var ids = blocks.Where(b => b.Reachable).Select(b => b.Ordinal).ToHashSet();
        var edges = new List<ControlEdge>();
        foreach (var b in blocks.Where(b => b.Reachable))
        {
            if (b.Kind == "Exit") { edges.Add(new(b.Ordinal, -1, "EXIT")); continue; }
            foreach (var conditional in new[] { false, true })
            {
                var branch = conditional ? b.Conditional : b.FallThrough;
                if (branch is null || branch.ConstantExcluded) continue;
                if (branch.FinallyRegions.Length > 0) return ([], "FINALLY_ROUTING");
                var outcome = b.Condition switch {
                    "WhenTrue" => conditional ? "TRUE" : "FALSE",
                    "WhenFalse" => conditional ? "FALSE" : "TRUE",
                    "None" => "ALWAYS",
                    _ => "UNKNOWN"
                };
                if (outcome == "UNKNOWN") return ([], "UNKNOWN_CONDITION");
                if (branch.Semantics is "Throw" or "Rethrow" or "ProgramTermination")
                    edges.Add(new(b.Ordinal, -1, outcome));
                else if (branch.Semantics is "Regular" or "Return")
                {
                    if (branch.Destination is not { } target) return ([], "MISSING_DESTINATION");
                    if (ids.Contains(target)) edges.Add(new(b.Ordinal, target, outcome));
                }
                else return ([], "BRANCH_" + branch.Semantics);
            }
        }
        var parents = edges.GroupBy(e => e.Target).ToDictionary(g => g.Key, g => g.Select(e => e.Source).ToArray());
        var seen = new HashSet<int> { -1 };
        var queue = new Queue<int>(); queue.Enqueue(-1);
        while (queue.TryDequeue(out var node))
            foreach (var parent in parents.GetValueOrDefault(node) ?? [])
                if (seen.Add(parent)) queue.Enqueue(parent);
        return ids.All(seen.Contains) ? (edges.ToArray(), null) : ([], "NON_EXIT_REACHABLE_REGION");
    }
}
