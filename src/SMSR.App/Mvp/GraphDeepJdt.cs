using System.Text.Json;
using static SMSR.App.Mvp.GraphDeepMapping;

namespace SMSR.App.Mvp;

internal static partial class GraphDeepProjection
{
    private static GraphDeepFact[] Jdt(JsonElement report, GraphDeepMapping map, GraphDeepEvidence evidence)
    {
        var candidates = Items(report, "candidates");
        if (candidates.Length != 1 || Text(report, "status") != "LSP_DEFINITION_CANDIDATE") return [];
        var candidate = candidates[0];
        var target = map.Declaration(Text(candidate, "path"), Number(Object(Object(candidate, "range"), "start"), "line") + 1);
        var source = Text(report, "path");
        var hashes = Object(report, "inputHashes");
        if (target is null || source is null || !hashes.TryGetProperty(source, out var sourceHash) ||
            Text(candidate, "hash") != target.Hash) return [];
        var line = Number(Object(report, "position"), "line") + 1;
        if (line < 1) return [];
        return [new(new("file:" + source, target.NodeId, "REFERENCES", source, line, "INFERRED", "INFERRED"),
            evidence with { Binding = "LSP_DEFINITION_CANDIDATE", Dispatch = "DEFINITION_ONLY", InputHash = sourceHash.GetString()! })];
    }
}
