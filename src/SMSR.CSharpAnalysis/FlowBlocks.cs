using Microsoft.CodeAnalysis.FlowAnalysis;

namespace SMSR.CSharpAnalysis;

internal static class FlowBlocks
{
    internal static (FlowBlock[], FlowRegion[]) Read(ControlFlowGraph graph,
        List<IFlowAnonymousFunctionOperation> nested, ref int budget)
    {
        var ids = new Dictionary<ControlFlowRegion, int>();
        var regions = new List<FlowRegion>();
        var stack = new Stack<(ControlFlowRegion Region, int? Parent)>();
        stack.Push((graph.Root, null));
        while (stack.TryPop(out var item))
        {
            var (r, parent) = item;
            FlowFacts.Take(ref budget, 1 + r.Locals.Length + r.LocalFunctions.Length);
            var id = ids.Count;
            ids.Add(r, id);
            regions.Add(new(id, parent, r.Kind.ToString(), r.FirstBlockOrdinal, r.LastBlockOrdinal,
                FlowVariables.Ids(r.Locals), FlowVariables.Ids(r.LocalFunctions)));
            foreach (var child in r.NestedRegions.Reverse()) stack.Push((child, id));
        }
        FlowBranch? Branch(ControlFlowBranch? b, bool excluded) => b is null ? null :
            new(b.Destination?.Ordinal, b.Semantics.ToString(), b.FinallyRegions.Select(r => ids[r]).ToArray(), excluded);
        var blocks = new List<FlowBlock>();
        foreach (var b in graph.Blocks)
        {
            FlowFacts.Take(ref budget, 1);
            bool? taken = b.ConditionKind != ControlFlowConditionKind.None &&
                b.BranchValue?.ConstantValue is { HasValue: true, Value: bool truth }
                ? truth == (b.ConditionKind == ControlFlowConditionKind.WhenTrue) : null;
            blocks.Add(new(b.Ordinal, b.Kind.ToString(), b.IsReachable, b.ConditionKind.ToString(),
                FlowOperations.Read(b.Operations, nested, ref budget),
                FlowOperations.Read(b.BranchValue is null ? [] : [b.BranchValue], nested, ref budget),
                Branch(b.FallThroughSuccessor, taken == true), Branch(b.ConditionalSuccessor, taken == false)));
        }
        return (blocks.ToArray(), regions.ToArray());
    }
}
