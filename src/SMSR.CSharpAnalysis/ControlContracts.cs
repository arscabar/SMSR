namespace SMSR.CSharpAnalysis;

internal sealed record Postdominator(int Block, int Immediate);
internal sealed record ControlLink(int Controller, int Successor, int Dependent, string Outcome);
internal sealed record ControlDependence(string Status, int ExitNode, Postdominator[] Postdominators,
    ControlLink[] Links, string[] Limitations)
{
    internal static ControlDependence Unavailable(string reason) => new("UNAVAILABLE", -1, [], [], [reason]);
}
internal sealed record ControlEdge(int Source, int Target, string Outcome);
