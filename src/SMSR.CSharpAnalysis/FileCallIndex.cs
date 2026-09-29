using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SMSR.CSharpAnalysis;

internal sealed record FileCall(string Source, string Target, int Line);

internal static class FileCallIndex
{
    internal static FileCall[] Run(AnalysisInput input)
    {
        var compilation = CompilationInput.Create(input, 1000);
        var calls = new Dictionary<(string Source, string Target), FileCall>();
        foreach (var tree in compilation.SyntaxTrees)
        {
            var model = compilation.GetSemanticModel(tree);
            foreach (var node in tree.GetRoot().DescendantNodes().Where(node =>
                node is InvocationExpressionSyntax or ObjectCreationExpressionSyntax or ImplicitObjectCreationExpressionSyntax))
            {
                var symbol = model.GetSymbolInfo(node).Symbol;
                var target = symbol?.Locations.FirstOrDefault(location => location.IsInSource)?.SourceTree?.FilePath;
                if (target is null || target == tree.FilePath) continue;
                var line = node.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                calls.TryAdd((tree.FilePath, target), new(tree.FilePath, target, line));
            }
        }
        return calls.Values.OrderBy(call => call.Source, StringComparer.Ordinal)
            .ThenBy(call => call.Target, StringComparer.Ordinal).ToArray();
    }
}
