using System.IO;
using System.Text.Json;

namespace SMSR.App.Mvp;

internal static class GraphDeepStorageSelfCheck
{
    internal static async Task RunAsync()
    {
        GraphDeepMappingSelfCheck.Run();
        GraphDeepJdtSelfCheck.Run();
        var root = Path.Combine(Path.GetTempPath(), "smsr-deep-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var store = new EventStore(Path.Combine(root, "smsr.db"));
            await store.InitializeAsync();
            GraphNode Symbol(string id, int line, string hash = "hash") => new(id, "A.cs", "symbol", id + "()", "A.cs", line, hash);
            var nodes = new[] { new GraphNode("file:A.cs", "A.cs", "code", "A.cs", "A.cs", 1, "hash"), Symbol("caller", 2), Symbol("target", 4) };
            await store.ApplyGraphScanAsync("deep", new(root, [new("A.cs", "hash", "code")], nodes, [], [], ["A.cs"], [], []), false);
            var report = JsonSerializer.Serialize(new { analysisVersion = 9, revision = 1, inputHashes = new { }, result = new {
                status = "BOUND_INPUT_BUNDLE", symbols = new[] {
                    new { id = "source:caller", kind = "Method", source = new { path = "A.cs", start = new { line = 1 } } },
                    new { id = "source:target", kind = "Method", source = new { path = "A.cs", start = new { line = 3 } } } },
                calls = new[] { new { callerId = "source:caller", targetId = "source:target", resolution = "BOUND_IN_BUNDLE",
                    dispatch = "DIRECT", source = new { path = "A.cs", start = new { line = 2 } } } } } }, GraphWorker.Json);
            var document = System.Text.Json.Nodes.JsonNode.Parse(report)!;
            document["inputHashes"] = JsonSerializer.SerializeToNode(new Dictionary<string,string> { ["A.cs"] = "hash" });
            await store.SaveGraphDerivedAsync("deep", "csharp", "key", 1, document.ToJsonString());
            var info = await store.GetGraphInfoAsync("deep") ?? throw new Exception("Graph missing");
            if (info.Revision != 2 || (await store.GetGraphDeepStatusAsync("deep")).Single().Applied != 1)
                throw new Exception("Saved analysis did not automatically create a separate deep revision");
            var saved = JsonSerializer.Deserialize<JsonElement>((await store.GetGraphDerivedAsync("deep", "csharp", "key"))!);
            if (!await store.IsGraphDeepReportCurrentAsync("deep", "csharp", "key", saved, default) ||
                await store.RefreshGraphDeepAsync("deep", 2) != 2) throw new Exception("Stable deep inputs not reused");
            await GraphDeepQuerySelfCheck.RunAsync(store);
            await GraphDeepHttpSelfCheck.RunAsync(root);
            await GraphDeepInvalidationSelfCheck.RunAsync(store, root, nodes, saved);
        }
        finally { Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools(); Directory.Delete(root, true); }
    }
}
