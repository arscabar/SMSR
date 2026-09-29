using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis;

namespace SMSR.CSharpAnalysis;

internal static class ValueFacts
{
    internal static ValueFlow Read(ControlFlowGraph cfg,FlowBlock[] blocks,VariableSite[] sites,DefinitionLink[] definitions,
        Dictionary<(IOperation,bool),int> bindings,ref int budget)
    {
        var graph=new ValueBuilder(bindings,sites);
        foreach(var link in definitions) graph.Link(link.Definition,link.Read,"REACHING_DEFINITION");
        foreach(var block in cfg.Blocks.Where(b=>b.IsReachable))
        {
            foreach(var root in block.Operations) ValueOperations.Read(root,block.Ordinal,graph);
            if(block.BranchValue is not { } branch) continue;
            ValueOperations.Read(branch,block.Ordinal,graph);
            var kind=block.ConditionKind!=ControlFlowConditionKind.None?"CONDITION":
                block.FallThroughSuccessor?.Semantics==ControlFlowBranchSemantics.Return?"RETURN":"BRANCH_VALUE";
            graph.Port(new(kind,graph.Results[branch],block.Ordinal,SymbolFacts.Source(branch.Syntax)));
        }
        if(graph.Captures.Count>0)
            foreach(var link in ReachingDefinitions.Solve(blocks,graph.Captures.ToArray(),ref budget))
                graph.Link(graph.CaptureValues[link.Definition],graph.CaptureValues[link.Read],"CAPTURE_DEFINITION");
        return new(graph.Nodes.Any(n=>n.Status!="LOCAL_VALUE")?"PARTIAL_VALUE_DEPENDENCE":"LOCAL_VALUE_DEPENDENCE",
            graph.Nodes.ToArray(),graph.Links.ToArray(),graph.Ports.ToArray(),
            ["MAY_VALUE_NOT_EXECUTION", "OPAQUE_BOUNDARIES_NOT_PROPAGATED", "HEAP_AND_PATH_CONDITIONS_UNMODELED", "NOT_TAINT"]);
    }
}
