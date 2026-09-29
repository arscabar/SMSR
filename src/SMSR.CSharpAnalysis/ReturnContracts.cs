namespace SMSR.CSharpAnalysis;

internal sealed record ReturnSummary(string SymbolId, Span Source, string Status,
    int[] ParameterOrdinals, bool Uncertain, ReturnCall[] Calls, string[] Limitations, ArgumentFlow Arguments);
internal sealed record ReturnCall(Span Source, string? TargetId, int ResultValue, string Status,
    bool Uncertain, ValueLink[] Links);

internal sealed class ReturnContext(FunctionFlow function)
{
    internal readonly FunctionFlow Function = function;
    internal readonly ValueFlow Values = function.Definitions.Values;
    internal readonly Dictionary<int, ReturnSite> Calls = [];
    internal readonly HashSet<int> Parameters = [];
    internal bool Uncertain = true;
    internal bool Available => Values.Status != "UNAVAILABLE" &&
        Function.Boundary.ReturnKind == "VALUE" && Values.Ports.Any(p => p.Kind == "RETURN");
    internal readonly Dictionary<int, ValueLink[]> Incoming = function.Definitions.Values.Links
        .GroupBy(l => l.Target).ToDictionary(g => g.Key, g => g.ToArray());
    internal readonly Dictionary<int, int> Entries = function.Definitions.Values.Nodes
        .Where(n => n.Kind == "ENTRY").ToDictionary(n => n.Id, n =>
            function.Boundary.Parameters.Single(p => p.Id == n.SymbolId).Ordinal);
}
internal sealed record ArgumentFlow(string Status, ArgumentOrigin[] Origins, string[] Limitations);
internal sealed record ArgumentOrigin(Span Source, Span CallSource, string? TargetId, int? ParameterOrdinal,
    int Value, string CallStatus, bool Uncertain, ArgumentDependency[] Dependencies);
internal sealed record ArgumentDependency(int ParameterOrdinal, int EntryValue, ValueLink[] Path);
internal sealed record ReturnSite(ValuePort Port, ReturnContext? Target, string Status,
    Dictionary<int, int> Arguments);
internal sealed record ReturnReach(HashSet<int> Parameters, HashSet<ReturnSite> Calls, bool Opaque)
{
    internal bool Uncertain => Opaque || Calls.Any(c => c.Target is null || c.Target.Uncertain);
}
