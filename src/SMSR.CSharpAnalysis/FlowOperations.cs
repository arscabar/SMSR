using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace SMSR.CSharpAnalysis;

internal static class FlowOperations
{
    internal static FlowOperation[] Read(IEnumerable<IOperation> roots,
        List<IFlowAnonymousFunctionOperation> nested, ref int budget)
    {
        var result = new List<FlowOperation>();
        var stack = new Stack<(IOperation Op, int? Parent, int Index)>();
        foreach (var root in roots.Reverse()) stack.Push((root, null, 0));
        while (stack.TryPop(out var item))
        {
            FlowFacts.Take(ref budget, 1);
            var (op, parent, index) = item;
            var id = result.Count;
            ISymbol? symbol = op switch {
                ILocalReferenceOperation l => l.Local,
                IParameterReferenceOperation p => p.Parameter,
                IMemberReferenceOperation m => m.Member,
                IInvocationOperation i => i.TargetMethod,
                IObjectCreationOperation c => c.Constructor,
                IVariableDeclaratorOperation v => v.Symbol,
                IFlowAnonymousFunctionOperation a => a.Symbol,
                _ => null
            };
            var parameter = (op as IArgumentOperation)?.Parameter;
            result.Add(new(id, parent, index, op.Kind.ToString(), SymbolFacts.Source(op.Syntax),
                op.IsImplicit, symbol is null ? null : SymbolFacts.Id(symbol),
                parameter is null ? null : SymbolFacts.Id(parameter)));
            if (op is IFlowAnonymousFunctionOperation anonymous) nested.Add(anonymous);
            var children = op.ChildOperations.ToArray();
            for (var i = children.Length - 1; i >= 0; i--) stack.Push((children[i], id, i));
        }
        return result.ToArray();
    }
}
