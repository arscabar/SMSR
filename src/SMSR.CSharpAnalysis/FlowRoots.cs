using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace SMSR.CSharpAnalysis;

internal static class FlowRoots
{
    internal static ControlFlowGraph? Read(SyntaxNode node, SemanticModel model)
    {
        var expression = node switch {
            PropertyDeclarationSyntax p => p.ExpressionBody?.Expression,
            IndexerDeclarationSyntax i => i.ExpressionBody?.Expression,
            _ => null
        };
        if (expression is null) return ControlFlowGraph.Create(node, model);
        // A property declaration itself has no operation; its getter owns the body.
        var operation = model.GetOperation(expression);
        while (operation?.Parent is { } parent) operation = parent;
        return operation switch {
            IMethodBodyOperation method => ControlFlowGraph.Create(method),
            IBlockOperation block => ControlFlowGraph.Create(block),
            _ => null
        };
    }
}
