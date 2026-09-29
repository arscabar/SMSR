namespace SMSR.CSharpAnalysis;

internal sealed record ValueNode(int Id, int Block, string Kind, Span Source, string Status, string? SymbolId = null);
internal sealed record ValueLink(int Source, int Target, string Relation);
internal sealed record ValuePort(string Kind, int Value, int Block, Span Source,
    string? TargetId = null, int? ParameterOrdinal = null, Span? CallSource = null);
internal sealed record ValueFlow(string Status, ValueNode[] Nodes, ValueLink[] Links, ValuePort[] Ports, string[] Limitations)
{
    internal static ValueFlow Unavailable(string reason) => new("UNAVAILABLE", [], [], [], [reason]);
}
