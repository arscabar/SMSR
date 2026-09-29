using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SMSR.CSharpAnalysis;

internal static class Analyzer
{
    internal static AnalysisOutput Run(AnalysisInput input)
    {
        var compilation = CompilationInput.Create(input);
        var diagnostics = compilation.GetDiagnostics();
        var valid = !diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error);
        var declarations = new Dictionary<string, Declaration>();
        var calls = new List<CallBinding>();
        var references = new List<ReferenceBinding>();
        foreach (var tree in compilation.SyntaxTrees)
        {
            var model = compilation.GetSemanticModel(tree);
            foreach (var node in tree.GetRoot().DescendantNodes())
            {
                if (node is BaseTypeDeclarationSyntax or BaseMethodDeclarationSyntax or PropertyDeclarationSyntax or
                    VariableDeclaratorSyntax or ParameterSyntax or LocalFunctionStatementSyntax or DelegateDeclarationSyntax or AccessorDeclarationSyntax)
                {
                    var symbol = model.GetDeclaredSymbol(node);
                    if (symbol is not null)
                        declarations.TryAdd(SymbolFacts.Id(symbol), new(SymbolFacts.Id(symbol), symbol.Name,
                            symbol.Kind.ToString(), SymbolFacts.Source(symbol.Locations.First())));
                }
                if (node is InvocationExpressionSyntax or BaseObjectCreationExpressionSyntax)
                    calls.Add(CallFacts.Read(model, (ExpressionSyntax)node, valid));
                if (node is SimpleNameSyntax name)
                {
                    var info = model.GetSymbolInfo(name);
                    var alias = name is IdentifierNameSyntax identifier ? model.GetAliasInfo(identifier) : null;
                    var target = alias?.Target ?? info.Symbol;
                    references.Add(new(SymbolFacts.Source(name), target is null ? null : SymbolFacts.Id(target),
                        alias?.Name, target is null ? "UNRESOLVED" : valid ? "BOUND_IN_BUNDLE" : "COMPILER_CANDIDATE",
                        SymbolFacts.Candidates(info)));
                }
                if (declarations.Count + calls.Count + references.Count > 20_000)
                    throw new ArgumentException("Analysis node limit exceeded");
            }
        }
        var functions = FlowFacts.Read(compilation, valid);
        var connections = CallConnections.Read(calls, functions);
        return new(valid ? "BOUND_INPUT_BUNDLE" : "COMPILATION_ERRORS", "EXPLICIT_FILES_HOST_FRAMEWORK",
            input.LanguageVersion, input.Defines ?? [], Environment.Version.ToString(),
            typeof(CSharpCompilation).Assembly.GetName().Version!.ToString(), input.Files.Select(f => f.Path).ToArray(),
            declarations.Values.ToArray(), calls.ToArray(), references.ToArray(), diagnostics.Take(2000).Select(d =>
                new CompilerIssue(d.Id, d.Severity.ToString(), SymbolFacts.Source(d.Location))).ToArray(),
            diagnostics.Length, diagnostics.Length > 2000, functions, connections, ReturnSummaries.Read(functions, connections));
    }
}
