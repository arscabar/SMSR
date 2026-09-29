using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace SMSR.App.Mvp;

internal static class GraphCSharpStorageSelfCheck
{
    internal static async Task RunAsync(string root, EventStore store, HttpClient client, string address,
        string folder, GraphCSharpRequest request)
    {
        var normalized = request.Normalize();
        var saved = await store.GetGraphDerivedAsync("csharp", "csharp", normalized.Key)
            ?? throw new Exception("C# report not persisted");
        if (saved.Contains("SOURCE_MARKER_NOT_SAVED") || saved.Contains("public static class"))
            throw new Exception("C# raw source persisted");
        var reversed = request with { Paths = request.Paths.Reverse().ToArray() };
        if (reversed.Normalize().Key != normalized.Key) throw new Exception("C# bundle key order-dependent");
        var gateway = new McpHttpGateway(address, folder);
        var result = JsonSerializer.Deserialize<JsonElement>(await gateway.CallAsync("get_graph_csharp_bundle", new {
            projectId = "csharp", paths = reversed.Paths }));
        if (result.GetProperty("stale").GetBoolean()) throw new Exception("Fresh C# report marked stale");
        var analyzed = JsonSerializer.Deserialize<JsonElement>(await gateway.CallAsync("analyze_graph_csharp_bundle", new {
            projectId = "csharp", paths = reversed.Paths }));
        if (analyzed.GetProperty("result").GetProperty("calls").GetArrayLength() != 1)
            throw new Exception("MCP C# analysis failed");
        await GraphCSharpVersionSelfCheck.RunAsync(store, gateway, normalized, saved);
        GraphCSharpConnectionsSelfCheck.Verify(analyzed.GetProperty("result"));
        foreach (var paths in new[] { new[] { "../Library.cs" }, Array.Empty<string>(), new[] { "Library.cs", "library.cs" } })
        {
            using var invalid = await client.PostAsJsonAsync(address + "/api/graph/csharp", request with { Paths = paths });
            if (invalid.StatusCode != HttpStatusCode.BadRequest) throw new Exception("C# invalid paths accepted");
        }
        await File.AppendAllTextAsync(Path.Combine(root, "Library.cs"), " // changed dependency");
        using var stale = await client.PostAsJsonAsync(address + "/api/graph/csharp-result", request);
        if (!(await stale.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("stale").GetBoolean())
            throw new Exception("C# dependency drift missing");
        using var rejected = await client.PostAsJsonAsync(address + "/api/graph/csharp", request);
        if (rejected.StatusCode != HttpStatusCode.Conflict) throw new Exception("C# stale source accepted");
        await store.DeleteProjectAsync("csharp");
        if (await store.GetGraphDerivedAsync("csharp", "csharp", normalized.Key) is not null)
            throw new Exception("C# derived report survived project deletion");
    }
}
