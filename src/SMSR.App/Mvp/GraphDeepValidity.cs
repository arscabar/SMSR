using System.Text.Json;
using static SMSR.App.Mvp.GraphDeepMapping;

namespace SMSR.App.Mvp;

internal static class GraphDeepValidity
{
    internal static bool Supported(string kind) => kind is "analysis" or "csharp" or "java" or "typescript" or "jdt";
    internal static string? Invalid(string kind, string key, JsonElement report, int revision,
        IReadOnlyDictionary<string, string> files, JsonElement context)
    {
        if (Number(report, "analysisVersion") != GraphAdvancedService.DeepVersion(kind)) return "ANALYZER_VERSION_CHANGED";
        var hashes = Object(report, "inputHashes");
        var pairs = hashes.ValueKind == JsonValueKind.Object
            ? hashes.EnumerateObject().Select(p => (p.Name, p.Value.ValueKind == JsonValueKind.String ? p.Value.GetString() : null)).ToArray()
            : [(key, Text(report, "hash"))];
        if (pairs.Length == 0 || pairs.Any(p => p.Item2 is null || !files.TryGetValue(p.Item1, out var hash) || hash != p.Item2))
            return "INPUT_CHANGED_OR_REMOVED";
        var saved = Object(report, "graphContext");
        if (saved.ValueKind != JsonValueKind.Object)
            return Number(report, "revision") == revision ? null : "LEGACY_CONTEXT_REANALYZE";
        if (Text(saved, "analysisKey") != key || Text(saved, "kind") != kind) return "ANALYSIS_SCOPE_MISMATCH";
        if (Text(saved, "root") != Text(context, "root") || Text(saved, "scope") != Text(context, "scope"))
            return "INDEX_SCOPE_CHANGED";
        var previous = Object(saved, "configuration");
        var current = Object(context, "configuration");
        if (previous.ValueKind != JsonValueKind.Object) return "INVALID_CONTEXT";
        return previous.GetRawText() != current.GetRawText() ? "CONFIGURATION_CHANGED" : null;
    }
}

public sealed partial class GraphAdvancedService
{
    internal static int DeepVersion(string kind) => kind switch
    {
        "analysis" => CodeAnalysisVersion, "csharp" => CSharpAnalysisVersion,
        "java" => JavaAnalysisVersion, "typescript" => TypeScriptAnalysisVersion, "jdt" => 1, _ => -1
    };
}
