using System.Text.Json;
using System.Text.Json.Nodes;

namespace SMSR.App.Mvp;

internal static class GraphCSharpVersionSelfCheck
{
    internal static async Task RunAsync(EventStore store, McpHttpGateway gateway,
        GraphCSharpRequest request, string saved)
    {
        var document = JsonNode.Parse(saved)!.AsObject();
        GraphCSharpConnectionsSelfCheck.Verify(JsonSerializer.Deserialize<JsonElement>(saved).GetProperty("result"));
        var functions = document["result"]!["functions"]!.AsArray();
        if (functions.Count != 3 || functions.Any(f => f!["status"]!.GetValue<string>() != "COMPILER_CFG"))
            throw new Exception("C# stored compiler flow incomplete");
        var revision = document["revision"]!.GetValue<int>();
        foreach (var oldVersion in new[] { 0, 2, 3, 4, 5, 6, 7, 8 })
        {
            if (oldVersion == 0) document.Remove("analysisVersion");
            else document["analysisVersion"] = oldVersion;
            await store.SaveGraphDerivedAsync("csharp", "csharp", request.Key, revision, document.ToJsonString(), default);
            var legacy = JsonSerializer.Deserialize<JsonElement>(await gateway.CallAsync("get_graph_csharp_bundle", new {
                projectId = "csharp", paths = request.Paths }));
            if (!legacy.GetProperty("stale").GetBoolean()) throw new Exception("Old compiler analysis reported fresh");
        }
        await store.SaveGraphDerivedAsync("csharp", "csharp", request.Key, revision, saved, default);
    }
}
