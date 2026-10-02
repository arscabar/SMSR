namespace SMSR.App.Mvp;

internal static class GraphKnowledgeFixture
{
    internal static readonly string Hash = new('A', 64);
    internal static readonly GraphFile[] Files = [new("A.cs", Hash, "code"), new("B.cs", Hash, "code")];
    internal static readonly GraphNode[] Nodes = [
        new("file:A.cs", "A.cs", "code", "A.cs", "A.cs", 1, Hash),
        new("file:B.cs", "B.cs", "code", "B.cs", "B.cs", 1, Hash),
        new("class", "A.cs", "symbol", "Client", "A.cs", 2, Hash,
            new("class", "code", "Graphify", "0.9.70", "file:A.cs", 9)),
        new("method", "A.cs", "symbol", "Run()", "A.cs", 3, Hash,
            new("method", "code", "Graphify", "0.9.70", "class", 7)),
        new("function", "B.cs", "symbol", "Process()", "B.cs", 1, Hash,
            new("function", "code", "Graphify", "0.9.70", "file:B.cs", 3))];
    internal static readonly GraphEdge Edge = new("method", "function", "CALLS", "A.cs", 4,
        "RESOLVED", "EXTRACTED", Evidence: new("Graphify", "0.9.70", Hash, 1));
    internal static readonly GraphHyperedge Group = new("auth", "인증 처리", "PARTICIPATE_IN", "A.cs", 4,
        "INFERRED", [new("class", "controller"), new("method", "entry"), new("function", "processor")],
        new("Graphify", "0.9.70", Hash, .75));
    internal static GraphScan Scan(string root, GraphHyperedge? group = null) => new(root, Files,
        Nodes, [Edge], [], ["A.cs", "B.cs"], [], [], Hyperedges: [group ?? Group]);
}
