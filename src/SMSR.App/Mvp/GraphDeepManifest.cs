using System.Security.Cryptography;
using System.Text.Json;
using static SMSR.App.Mvp.GraphDeepMapping;

namespace SMSR.App.Mvp;

public sealed record GraphDeepManifest(string Analyzer, string AnalysisKey, int AnalysisVersion,
    string Profile, string? Runtime, string? CompilerVersion, IReadOnlyDictionary<string, string> InputHashes,
    JsonElement Settings, JsonElement IndexContext)
{
    internal static GraphDeepManifest Read(string kind, string key, JsonElement report)
    {
        var result = Object(report, "result");
        var hashes = Object(report, "inputHashes");
        var inputs = hashes.ValueKind == JsonValueKind.Object
            ? hashes.EnumerateObject().OrderBy(p => p.Name, StringComparer.Ordinal).ToDictionary(p => p.Name, p => p.Value.GetString()!)
            : new Dictionary<string, string> { [key] = Text(report, "hash")! };
        var settings = new Dictionary<string, JsonElement>();
        foreach (var field in new[] { "languageVersion", "defines", "positionEncoding" })
            if (result.ValueKind == JsonValueKind.Object && result.TryGetProperty(field, out var value)) settings[field] = value.Clone();
        if (report.TryGetProperty("sourceRoots", out var roots)) settings["sourceRoots"] = roots.Clone();
        var context = Object(report, "graphContext");
        return new(kind, key, Number(report, "analysisVersion"), Text(result, "profile") ?? Text(report, "profile")
            ?? Text(result, "status") ?? kind, Text(result, "runtime") ?? Text(Object(result, "compiler"), "runtime"),
            Text(result, "compilerVersion"), inputs, JsonSerializer.SerializeToElement(settings),
            context.ValueKind == JsonValueKind.Object ? context.Clone() : JsonSerializer.SerializeToElement(new { }));
    }
    internal string Hash => Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(this, GraphWorker.Json)));
    internal string SettingsHash => Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(
        new { AnalysisKey, Settings, IndexContext }, GraphWorker.Json)));
}
