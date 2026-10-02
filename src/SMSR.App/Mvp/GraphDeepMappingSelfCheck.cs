using System.Text.Json;

namespace SMSR.App.Mvp;

internal static class GraphDeepMappingSelfCheck
{
    internal static void Run()
    {
        GraphNode Node(string id, string path, int line, string label) => new(id, path, "symbol", label, path, line, "hash");
        var map = new GraphDeepMapping([Node("a", "A.cs", 2, "A.Run()"), Node("b", "B.cs", 2, "B.Run()"),
            Node("c", "A.cs", 4, "A.Pick(int)"), Node("d", "A.cs", 4, "A.Pick(string)"), Node("type", "A.cs", 1, "A")]);
        if (map.Callable("A.cs", 2)?.NodeId != "a" || map.Callable("B.cs", 2)?.NodeId != "b" ||
            map.Callable("A.cs", 4) is not null || map.Callable("A.cs", 1) is not null)
            throw new Exception("Physical declaration mapping merged ambiguous or cross-file symbols");
        var report = JsonSerializer.SerializeToElement(new { analysisVersion = 9, result = new {
            status = "BOUND_INPUT_BUNDLE", symbols = new[] {
                new { id = "caller", kind = "Method", source = new { path = "A.cs", start = new { line = 1 } } },
                new { id = "target", kind = "Method", source = new { path = "B.cs", start = new { line = 1 } } } },
            calls = new[] { new { callerId = "caller", targetId = "target", resolution = "STATIC_TARGET_ONLY",
                dispatch = "VIRTUAL", source = new { path = "A.cs", start = new { line = 2 } } },
                new { callerId = "caller", targetId = "target", resolution = "COMPILER_CANDIDATE",
                dispatch = "DYNAMIC", source = new { path = "A.cs", start = new { line = 2 } } } } } });
        var facts = GraphDeepProjection.Read("csharp", "key", report, map);
        if (facts.Length != 2 || facts.Any(f => f.Edge.Resolution != "INFERRED") ||
            facts[0].Edge.SourceLine != 3 || facts[0].Evidence.Dispatch != "VIRTUAL")
            throw new Exception("Deep projection upgraded an uncertain call or lost physical evidence");
    }
}
