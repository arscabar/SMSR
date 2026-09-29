using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace SMSR.App.Mvp;

internal static class GraphJavaStorageSelfCheck
{
    internal static async Task RunAsync(string root,string folder,string address,HttpClient client,EventStore store,GraphJavaRequest request)
    {
        var normalized=request.Normalize(); var key=normalized.Key;
        var saved=await store.GetGraphDerivedAsync("java","java",key)??throw new Exception("Java report missing");
        if(saved.Contains("JAVA_RAW_MARKER") || saved.Contains("class Entry")) throw new Exception("Java raw source stored");
        GraphJavaSelfCheck.Verify(JsonSerializer.Deserialize<JsonElement>(saved));
        if((request with {Paths=request.Paths.Reverse().ToArray()}).Normalize().Key!=key) throw new Exception("Java key order dependent");
        var gateway=new McpHttpGateway(address,folder);
        var arguments=new {projectId=request.ProjectId,paths=request.Paths};
        GraphJavaSelfCheck.Verify(JsonSerializer.Deserialize<JsonElement>(await gateway.CallAsync("analyze_graph_java_bundle",arguments)));
        var loaded=JsonSerializer.Deserialize<JsonElement>(await gateway.CallAsync("get_graph_java_bundle",arguments));
        GraphJavaSelfCheck.Verify(loaded.GetProperty("report"));
        if(loaded.GetProperty("stale").GetBoolean()) throw new Exception("Fresh Java result stale");
        var legacy=JsonNode.Parse(saved)!; legacy["analysisVersion"]=5;
        await store.SaveGraphDerivedAsync("java","java",key,legacy["revision"]!.GetValue<int>(),legacy.ToJsonString());
        using var old=await client.PostAsJsonAsync(address+"/api/graph/java-result",request);
        if(!(await old.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("stale").GetBoolean()) throw new Exception("Legacy Java result fresh");
        foreach(var paths in new[]{new[]{"../Lib.java"},Array.Empty<string>(),new[]{"Lib.java","lib.java"}})
        {
            using var invalid=await client.PostAsJsonAsync(address+"/api/graph/java",request with {Paths=paths});
            if(invalid.StatusCode!=HttpStatusCode.BadRequest) throw new Exception("Invalid Java path accepted");
        }
        await store.SaveGraphDerivedAsync("java","java",key,legacy["revision"]!.GetValue<int>(),saved);
        await File.AppendAllTextAsync(Path.Combine(root,"Lib.java")," // changed");
        using var stale=await client.PostAsJsonAsync(address+"/api/graph/java-result",request);
        if(!(await stale.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("stale").GetBoolean()) throw new Exception("Java source drift missing");
        using var rejected=await client.PostAsJsonAsync(address+"/api/graph/java",request);
        if(rejected.StatusCode!=HttpStatusCode.Conflict) throw new Exception("Stale Java source accepted");
        await store.DeleteProjectAsync("java");
        if(await store.GetGraphDerivedAsync("java","java",key)!=null) throw new Exception("Java report survived deletion");
    }
}
