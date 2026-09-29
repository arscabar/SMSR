using Microsoft.CodeAnalysis;

namespace SMSR.CSharpAnalysis;

internal static class BoundaryFacts
{
    internal static MethodBoundary Read(ISymbol symbol, ref int budget)
    {
        if (symbol is not IMethodSymbol method) return new([], "UNKNOWN", null);
        FlowFacts.Take(ref budget, method.Parameters.Length + 1);
        return new(method.Parameters.Select(p => new ParameterSlot(SymbolFacts.Id(p), p.Ordinal,
            p.Name, SymbolFacts.Signature(p.Type), p.RefKind.ToString(), p.IsOptional, p.IsParams,
            p.Locations.Where(l => l.IsInSource).Select(SymbolFacts.Source).FirstOrDefault())).ToArray(),
            ReturnKind(method), method.IsStatic ? null : "receiver:" + SymbolFacts.Id(method));
    }

    internal static string ReturnKind(IMethodSymbol? method) => method switch {
        null => "UNKNOWN",
        { MethodKind: MethodKind.Constructor } => "CONSTRUCTED_INSTANCE",
        { IsAsync: true } => "ASYNC_RESULT",
        { IsIterator: true } => "ITERATOR_RESULT",
        { ReturnsByRefReadonly: true } => "REF_READONLY",
        { ReturnsByRef: true } => "REF",
        { ReturnsVoid: true } => "VOID",
        _ => "VALUE"
    };
}
