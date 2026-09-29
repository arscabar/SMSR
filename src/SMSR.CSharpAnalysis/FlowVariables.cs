using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SMSR.CSharpAnalysis;

internal static class FlowVariables
{
    internal static VariableFlow Read(SemanticModel model, SyntaxNode node, bool valid)
    {
        SyntaxNode? body = node switch {
            BaseMethodDeclarationSyntax m => (SyntaxNode?)m.Body ?? m.ExpressionBody?.Expression,
            AccessorDeclarationSyntax a => (SyntaxNode?)a.Body ?? a.ExpressionBody?.Expression,
            LocalFunctionStatementSyntax l => (SyntaxNode?)l.Body ?? l.ExpressionBody?.Expression,
            AnonymousFunctionExpressionSyntax a => a.Body,
            PropertyDeclarationSyntax p => p.ExpressionBody?.Expression,
            IndexerDeclarationSyntax i => i.ExpressionBody?.Expression,
            _ => null
        };
        DataFlowAnalysis? data = null;
        try { if (body is not null) data = model.AnalyzeDataFlow(body); }
        catch (ArgumentException) { } // Unsupported regions are not empty, successful analyses.
        if (data?.Succeeded != true)
            return new("UNAVAILABLE", body is null ? null : SymbolFacts.Source(body), [], [], [], [], [], [], []);
        return new(valid ? "COMPILER_REGION_SUMMARY" : "COMPILER_CANDIDATE", SymbolFacts.Source(body!),
            Ids(data.VariablesDeclared), Ids(data.ReadInside), Ids(data.WrittenInside),
            Ids(data.DataFlowsIn), Ids(data.DataFlowsOut), Ids(data.AlwaysAssigned), Ids(data.CapturedInside));
    }

    internal static string[] Ids(IEnumerable<ISymbol> symbols) => symbols.Select(SymbolFacts.Id)
        .Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal).ToArray();
}
