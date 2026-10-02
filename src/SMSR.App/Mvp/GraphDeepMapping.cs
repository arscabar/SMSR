using System.Text.Json;

namespace SMSR.App.Mvp;

internal sealed class GraphDeepMapping(IEnumerable<GraphNode> nodes)
{
    private readonly Dictionary<(string, int), GraphNode[]> locations = nodes.Where(n => n.Kind == "symbol")
        .GroupBy(n => (n.OwnerPath, n.Line)).ToDictionary(g => g.Key, g => g.ToArray());

    internal GraphNode? Callable(string? path, int line)
    {
        if (path is null || line < 1 || !locations.TryGetValue((path, line), out var candidates)) return null;
        // ponytail: baseline stores no declaration columns; same-line overloads stay unmapped.
        var matches = candidates.Where(n => n.Label.Contains('(')).ToArray();
        return matches.Length == 1 ? matches[0] : null;
    }

    internal GraphNode? Declaration(string? path, int line)
        => path is not null && locations.TryGetValue((path, line), out var candidates)
            && candidates.Length == 1 ? candidates[0] : null;

    internal static string? Text(JsonElement item, string key)
        => item.ValueKind == JsonValueKind.Object && item.TryGetProperty(key, out var value)
            && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    internal static JsonElement[] Items(JsonElement item, string key)
        => item.ValueKind == JsonValueKind.Object && item.TryGetProperty(key, out var value)
            && value.ValueKind == JsonValueKind.Array ? value.EnumerateArray().ToArray() : [];
    internal static JsonElement Object(JsonElement item, string key)
        => item.ValueKind == JsonValueKind.Object && item.TryGetProperty(key, out var value) ? value : default;
    internal static int Number(JsonElement item, string key)
        => Object(item, key) is { ValueKind: JsonValueKind.Number } value && value.TryGetInt32(out var n) ? n : -1;
    internal GraphNode? Span(JsonElement span)
        => Callable(Text(span, "path"), Number(Object(span, "start"), "line") + 1);
}

public sealed record GraphDeepEvidence(string Analyzer, int AnalysisVersion, string AnalysisKey,
    string Profile, string Binding, string Dispatch, string InputHash, string SettingsHash, string? ManifestHash = null);
internal sealed record GraphDeepFact(GraphEdge Edge, GraphDeepEvidence Evidence);
