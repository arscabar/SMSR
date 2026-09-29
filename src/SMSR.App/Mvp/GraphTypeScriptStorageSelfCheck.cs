using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace SMSR.App.Mvp;

internal static class GraphTypeScriptStorageSelfCheck
{
    internal static async Task RunAsync(string root,string folder,string address,HttpClient client,EventStore store,GraphTypeScriptRequest request)
    {
        var normalized=request.Normalize(); var key=normalized.Key;
        var saved=await store.GetGraphDerivedAsync("typescript","typescript",key)??throw new Exception("TypeScript report missing");
        if(saved.Contains("TS_RAW_MARKER") || saved.Contains("export const result")) throw new Exception("TypeScript raw source stored");
        GraphTypeScriptSelfCheck.Verify(JsonSerializer.Deserialize<JsonElement>(saved));
        if((request with {Paths=request.Paths.Reverse().ToArray()}).Normalize().Key!=key) throw new Exception("TypeScript key order dependent");
        var gateway=new McpHttpGateway(address,folder);
        var arguments=new {projectId=request.ProjectId,paths=request.Paths};
        GraphTypeScriptSelfCheck.Verify(JsonSerializer.Deserialize<JsonElement>(await gateway.CallAsync("analyze_graph_typescript_bundle",arguments)));
        var loaded=JsonSerializer.Deserialize<JsonElement>(await gateway.CallAsync("get_graph_typescript_bundle",arguments));
        GraphTypeScriptSelfCheck.Verify(loaded.GetProperty("report"));
        if(loaded.GetProperty("stale").GetBoolean()) throw new Exception("Fresh TypeScript result stale");
        var legacy=JsonNode.Parse(saved)!; legacy["analysisVersion"]=7;
        await store.SaveGraphDerivedAsync("typescript","typescript",key,legacy["revision"]!.GetValue<int>(),legacy.ToJsonString());
        using var old=await client.PostAsJsonAsync(address+"/api/graph/typescript-result",request);
        if(!(await old.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("stale").GetBoolean()) throw new Exception("Legacy TypeScript result fresh");
        foreach(var paths in new[]{new[]{"../Lib.ts"},Array.Empty<string>(),new[]{"Lib.ts","lib.ts"}})
        {
            using var invalid=await client.PostAsJsonAsync(address+"/api/graph/typescript",request with {Paths=paths});
            if(invalid.StatusCode!=HttpStatusCode.BadRequest) throw new Exception("Invalid TypeScript path accepted");
        }
        await store.SaveGraphDerivedAsync("typescript","typescript",key,legacy["revision"]!.GetValue<int>(),saved);
        await File.AppendAllTextAsync(Path.Combine(root,"Lib.ts")," // changed");
        using var stale=await client.PostAsJsonAsync(address+"/api/graph/typescript-result",request);
        if(!(await stale.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("stale").GetBoolean()) throw new Exception("TypeScript source drift missing");
        using var rejected=await client.PostAsJsonAsync(address+"/api/graph/typescript",request);
        if(rejected.StatusCode!=HttpStatusCode.Conflict) throw new Exception("Stale TypeScript source accepted");
        await store.DeleteProjectAsync("typescript");
        if(await store.GetGraphDerivedAsync("typescript","typescript",key)!=null) throw new Exception("TypeScript report survived deletion");
    }
}
