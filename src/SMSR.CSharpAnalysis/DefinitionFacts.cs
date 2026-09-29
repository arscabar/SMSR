using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis;

namespace SMSR.CSharpAnalysis;

internal static class DefinitionFacts
{
    internal static DefinitionFlow Read(ControlFlowGraph graph, ISymbol symbol, bool valid,
        FlowBlock[] blocks, FlowRegion[] regions, VariableFlow variables, ref int budget)
    {
        if (!valid) return DefinitionFlow.Unavailable("COMPILATION_ERRORS");
        if (symbol is not IMethodSymbol method) return DefinitionFlow.Unavailable("UNKNOWN_METHOD");
        if (method.IsAsync || method.IsIterator || method.ReturnsByRef || method.ReturnsByRefReadonly ||
            method.Parameters.Any(p => p.RefKind != RefKind.None)) return DefinitionFlow.Unavailable("REFERENCE_OR_SUSPENSION");
        if (variables.Captured.Length > 0) return DefinitionFlow.Unavailable("CAPTURED_STORAGE");
        if (regions.Any(r => r.Kind is not ("Root" or "LocalLifetime")))
            return DefinitionFlow.Unavailable("EXCEPTION_OR_SPECIAL_REGION");
        var sites = new List<VariableSite>();
        var bindings = new Dictionary<(IOperation,bool),int>();
        foreach (var p in method.Parameters)
            sites.Add(new(sites.Count, -1, SymbolFacts.Id(p), "ENTRY",
                SymbolFacts.Source(p.Locations.First(l => l.IsInSource))!));
        foreach (var b in graph.Blocks.Where(b => b.IsReachable))
        {
            var reason = DefinitionOperations.Read(b.Operations.Concat(b.BranchValue is null ? [] : [b.BranchValue]), b.Ordinal, sites, bindings);
            if (reason is not null) return DefinitionFlow.Unavailable(reason);
        }
        var data = sites.ToArray();
        var links = ReachingDefinitions.Solve(blocks, data, ref budget);
        return new("MAY_REACHING_DEFINITIONS", data, links,
            ["INTRAPROCEDURAL_SCALAR_STORAGE", "NORMAL_CFG_PATHS", "PATH_CONDITIONS_UNMODELED", "NOT_TAINT"],
            ValueFacts.Read(graph, blocks, data, links, bindings, ref budget));
    }
}
