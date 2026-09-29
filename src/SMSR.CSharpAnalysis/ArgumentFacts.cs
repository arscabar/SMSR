using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace SMSR.CSharpAnalysis;

internal static class ArgumentFacts
{
    internal static ArgumentBinding Read(IArgumentOperation argument)
    {
        var p = argument.Parameter!;
        var elements = (argument.Value as IArrayCreationOperation)?.Initializer?.ElementValues;
        return new(p.Ordinal, p.RefKind.ToString(), argument.ArgumentKind.ToString(),
            SymbolFacts.Source(argument.Syntax), SymbolFacts.Source(argument.Value.Syntax), SymbolFacts.Id(p),
            argument.IsImplicit, Convert(argument.InConversion), Convert(argument.OutConversion),
            elements?.Select(e => SymbolFacts.Source(e.Syntax)).ToArray() ?? []);
    }

    private static ConversionBinding Convert(CommonConversion conversion) => new(conversion.Exists,
        conversion.IsIdentity, conversion.MethodSymbol is { } m ? SymbolFacts.Id(m) : null);
}
