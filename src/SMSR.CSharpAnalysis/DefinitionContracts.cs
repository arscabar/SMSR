namespace SMSR.CSharpAnalysis;

internal sealed record VariableSite(int Id, int Block, string SymbolId, string Kind, Span Source);
internal sealed record DefinitionLink(int Definition, int Read);
internal sealed record DefinitionFlow(string Status, VariableSite[] Sites, DefinitionLink[] Links, string[] Limitations, ValueFlow Values)
{
    internal static DefinitionFlow Unavailable(string reason) => new("UNAVAILABLE", [], [], [reason], ValueFlow.Unavailable(reason));
}
