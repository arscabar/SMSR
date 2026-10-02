using System.Text.Json;
using static SMSR.App.Mvp.GraphDeepMapping;

namespace SMSR.App.Mvp;

internal static partial class GraphDeepProjection
{
    internal static GraphDeepFact[] Read(string kind, string key, JsonElement report, GraphDeepMapping map)
    {
        var result = Object(report, "result");
        var manifest = GraphDeepManifest.Read(kind, key, report);
        var evidence = new GraphDeepEvidence(kind, manifest.AnalysisVersion, key, manifest.Profile, "", "", "",
            manifest.SettingsHash, manifest.Hash);
        return kind switch
        {
            "csharp" or "java" => Bundle(result, map, evidence),
            "typescript" => TypeScript(result, map, evidence),
            "analysis" => Python(result, key, map, evidence),
            "jdt" => Jdt(report, map, evidence),
            _ => []
        };
    }

    private static GraphDeepFact[] Bundle(JsonElement result, GraphDeepMapping map,
        GraphDeepEvidence evidence)
    {
        if (Text(result, "status") != "BOUND_INPUT_BUNDLE") return [];
        var symbols = Items(result, "symbols").Where(s => Text(s, "kind") is "Method" or "METHOD" or "CONSTRUCTOR")
            .GroupBy(s => Text(s, "id") ?? "").Where(g => g.Key != "" && g.Count() == 1)
            .ToDictionary(g => g.Key, g => map.Span(Object(g.Single(), "source")));
        var facts = new List<GraphDeepFact>();
        foreach (var call in Items(result, "calls"))
        {
            var binding = Text(call, "resolution");
            if (binding is not ("BOUND_IN_BUNDLE" or "BOUND_INPUT_BUNDLE" or "STATIC_TARGET_ONLY")) continue;
            if (!symbols.TryGetValue(Text(call, "callerId") ?? "", out var caller) || caller is null ||
                !symbols.TryGetValue(Text(call, "targetId") ?? "", out var target) || target is null) continue;
            var span = Object(call, "source");
            if (Text(span, "path") != caller.OwnerPath) continue;
            Add(facts, caller, target, Number(Object(span, "start"), "line") + 1,
                evidence with { Binding = binding, Dispatch = Text(call, "dispatch") ?? "UNKNOWN" });
        }
        return facts.ToArray();
    }

    private static void Add(List<GraphDeepFact> facts, GraphNode caller, GraphNode target, int line,
        GraphDeepEvidence evidence)
    {
        if (line < 1) return;
        var certain = evidence.Dispatch == "DIRECT" && evidence.Binding is "BOUND_IN_BUNDLE" or "BOUND_INPUT_BUNDLE";
        var edge = new GraphEdge(caller.NodeId, target.NodeId, "CALLS", caller.OwnerPath, line,
            certain ? "RESOLVED" : "INFERRED", certain ? "EXTRACTED" : "INFERRED");
        var fact = new GraphDeepFact(edge, evidence with { InputHash = caller.Hash });
        facts.Add(fact);
        if (caller.OwnerPath != target.OwnerPath)
            facts.Add(fact with { Edge = edge with { SourceId = "file:" + caller.OwnerPath, TargetId = "file:" + target.OwnerPath } });
    }
}
