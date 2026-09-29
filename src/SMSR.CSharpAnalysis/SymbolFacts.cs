using Microsoft.CodeAnalysis;

namespace SMSR.CSharpAnalysis;

internal static class SymbolFacts
{
    private static readonly SymbolDisplayFormat Display = new(
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
        memberOptions: SymbolDisplayMemberOptions.IncludeContainingType | SymbolDisplayMemberOptions.IncludeParameters,
        parameterOptions: SymbolDisplayParameterOptions.IncludeType | SymbolDisplayParameterOptions.IncludeParamsRefOut,
        miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

    internal static string Signature(ISymbol symbol) => symbol.ToDisplayString(Display);
    internal static Span? Source(Location location)
    {
        if (!location.IsInSource) return null;
        var line = location.GetLineSpan(); // Physical path/line: never trust #line redirects.
        return new(line.Path, new(line.StartLinePosition.Line, line.StartLinePosition.Character),
            new(line.EndLinePosition.Line, line.EndLinePosition.Character));
    }
    internal static Span Source(SyntaxNode node) => Source(node.GetLocation())!;

    internal static string Id(ISymbol symbol)
    {
        if (symbol is IMethodSymbol { ReducedFrom: { } original }) symbol = original;
        symbol = symbol.OriginalDefinition;
        var source = symbol.Locations.FirstOrDefault(l => l.IsInSource);
        // Documentation IDs omit lexical owners for local/anonymous methods.
        var documentation = symbol is IMethodSymbol { MethodKind: MethodKind.LocalFunction or MethodKind.AnonymousFunction }
            ? null : symbol.GetDocumentationCommentId();
        if (source is not null)
            return documentation is not null ? "source:" + documentation :
                $"source:{source.SourceTree!.FilePath}:{source.SourceSpan.Start}:{symbol.Kind}";
        return $"metadata:{symbol.ContainingAssembly?.Identity.Name}:{documentation ?? Signature(symbol)}";
    }

    internal static string[] Candidates(SymbolInfo info) => info.CandidateSymbols.Select(Id)
        .Distinct(StringComparer.Ordinal).OrderBy(id => id, StringComparer.Ordinal).ToArray();
}
