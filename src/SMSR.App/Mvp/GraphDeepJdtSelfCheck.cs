using System.Text.Json;

namespace SMSR.App.Mvp;

internal static class GraphDeepJdtSelfCheck
{
    internal static void Run()
    {
        var map = new GraphDeepMapping([new("target", "Lib.java", "symbol", "Lib.go()", "Lib.java", 3, "hash")]);
        var candidate = new { path = "Lib.java", hash = "hash", range = new { start = new { line = 2, character = 0 } } };
        JsonElement Report(int count) => JsonSerializer.SerializeToElement(new { analysisVersion = 1,
            status = "LSP_DEFINITION_CANDIDATE", path = "Entry.java", position = new { line = 4 },
            inputHashes = new Dictionary<string,string> { ["Entry.java"] = "caller", ["Lib.java"] = "hash" },
            candidates = Enumerable.Repeat(candidate, count).ToArray() });
        var facts = GraphDeepProjection.Read("jdt", "key", Report(1), map);
        if (facts.Length != 1 || facts[0].Edge.Relation != "REFERENCES" || facts[0].Edge.Resolution != "INFERRED" ||
            facts[0].Edge.SourceLine != 5 || GraphDeepProjection.Read("jdt", "key", Report(2), map).Length != 0)
            throw new Exception("JDT definition candidates became certain calls or ambiguous targets");
    }
}
