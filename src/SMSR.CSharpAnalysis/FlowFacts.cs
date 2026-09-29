using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FlowAnalysis;

namespace SMSR.CSharpAnalysis;

internal static class FlowFacts
{
    internal static FunctionFlow[] Read(CSharpCompilation compilation, bool valid)
    {
        var results = new List<FunctionFlow>();
        var budget = 40_000;
        var definitionBudget = 250_000;
        var controlBudget = 4_000_000;
        foreach (var tree in compilation.SyntaxTrees)
        {
            var model = compilation.GetSemanticModel(tree);
            foreach (var node in tree.GetRoot().DescendantNodes().Where(n => n is
                BaseMethodDeclarationSyntax or AccessorDeclarationSyntax ||
                n is PropertyDeclarationSyntax { ExpressionBody: not null } or
                IndexerDeclarationSyntax { ExpressionBody: not null }))
            {
                var symbol = model.GetDeclaredSymbol(node);
                if (symbol is IPropertySymbol p) symbol = p.GetMethod;
                if (symbol is null) continue;
                ControlFlowGraph? graph = null;
                try { graph = FlowRoots.Read(node, model); }
                catch (ArgumentException) { }
                if (graph is null)
                {
                    Take(ref budget, 1);
                    results.Add(new(SymbolFacts.Id(symbol), SymbolFacts.Source(node), "NO_CFG", [], [],
                        FlowVariables.Read(model, node, valid), BoundaryFacts.Read(symbol, ref budget), DefinitionFlow.Unavailable("NO_CFG"),
                        ControlDependence.Unavailable("NO_CFG")));
                    continue;
                }
                var queue = new Queue<(ControlFlowGraph Graph, ISymbol Symbol, SyntaxNode Node)>();
                queue.Enqueue((graph, symbol, node));
                while (queue.TryDequeue(out var item))
                {
                    Take(ref budget, 1);
                    var nested = new List<IFlowAnonymousFunctionOperation>();
                    var (blocks, regions) = FlowBlocks.Read(item.Graph, nested, ref budget);
                    var variables = FlowVariables.Read(model, item.Node, valid);
                    Take(ref budget, variables.Declared.Length + variables.Read.Length + variables.Written.Length +
                        variables.FlowsIn.Length + variables.FlowsOut.Length + variables.AlwaysAssigned.Length + variables.Captured.Length);
                    results.Add(new(SymbolFacts.Id(item.Symbol), SymbolFacts.Source(item.Node),
                        valid ? "COMPILER_CFG" : "COMPILER_CANDIDATE", blocks, regions, variables, BoundaryFacts.Read(item.Symbol, ref budget),
                        DefinitionFacts.Read(item.Graph, item.Symbol, valid, blocks, regions, variables, ref definitionBudget),
                        ControlFacts.Read(blocks, regions, valid, BoundaryFacts.ReturnKind(item.Symbol as IMethodSymbol), ref controlBudget)));
                    foreach (var local in item.Graph.LocalFunctions)
                        queue.Enqueue((item.Graph.GetLocalFunctionControlFlowGraph(local), local,
                            local.DeclaringSyntaxReferences.Single().GetSyntax()));
                    foreach (var anon in nested)
                        queue.Enqueue((item.Graph.GetAnonymousFunctionControlFlowGraph(anon), anon.Symbol, anon.Syntax));
                }
            }
        }
        return results.ToArray();
    }

    internal static void Take(ref int budget, int count)
    {
        budget -= count;
        if (budget < 0) throw new ArgumentException("Flow node limit exceeded");
    }
}
