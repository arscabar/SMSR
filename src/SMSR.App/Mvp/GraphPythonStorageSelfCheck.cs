using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace SMSR.App.Mvp;

internal static class GraphPythonStorageSelfCheck
{
    internal static async Task RunAsync(string root,string folder,string address,HttpClient client,EventStore store)
    {
        var saved=await store.GetGraphDerivedAsync("python","analysis","scope.py")??throw new Exception("Python report missing");
        if(saved.Contains("PYTHON_SOURCE_MARKER") || saved.Contains("def outer")) throw new Exception("Python source stored");
        GraphPythonSelfCheck.Verify(JsonSerializer.Deserialize<JsonElement>(saved));
        var gateway=new McpHttpGateway(address,folder); var args=new {projectId="python",path="scope.py"};
        GraphPythonSelfCheck.Verify(JsonSerializer.Deserialize<JsonElement>(await gateway.CallAsync("analyze_graph_code",args)));
        var loaded=JsonSerializer.Deserialize<JsonElement>(await gateway.CallAsync("get_graph_analysis",args));
        GraphPythonSelfCheck.Verify(loaded.GetProperty("report"));
        if(loaded.GetProperty("stale").GetBoolean()) throw new Exception("Python fresh report stale");
        var legacy=JsonNode.Parse(saved)!; legacy.AsObject().Remove("analysisVersion");
        await store.SaveGraphDerivedAsync("python","analysis","scope.py",legacy["revision"]!.GetValue<int>(),legacy.ToJsonString());
        var old=JsonSerializer.Deserialize<JsonElement>(await gateway.CallAsync("get_graph_analysis",args));
        if(!old.GetProperty("stale").GetBoolean()) throw new Exception("Old Python result fresh");
        foreach(var version in new[]{2,3,4,5,6,7,8,9})
        {
            legacy["analysisVersion"]=version;
            await store.SaveGraphDerivedAsync("python","analysis","scope.py",legacy["revision"]!.GetValue<int>(),legacy.ToJsonString());
            var outdated=JsonSerializer.Deserialize<JsonElement>(await gateway.CallAsync("get_graph_analysis",args));
            if(!outdated.GetProperty("stale").GetBoolean()) throw new Exception($"Python version{version} result fresh");
        }
        await store.SaveGraphDerivedAsync("python","analysis","scope.py",legacy["revision"]!.GetValue<int>(),saved);
        await File.AppendAllTextAsync(Path.Combine(root,"scope.py"),"\n# changed");
        using var stale=await client.PostAsJsonAsync(address+"/api/graph/analysis",new {projectId="python",input="scope.py"});
        if(!(await stale.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("stale").GetBoolean()) throw new Exception("Python source drift missing");
        using var rejected=await client.PostAsJsonAsync(address+"/api/graph/analyze",new {projectId="python",input="scope.py"});
        if(rejected.StatusCode!=HttpStatusCode.Conflict) throw new Exception("Python stale source accepted");
        await store.DeleteProjectAsync("python");
        if(await store.GetGraphDerivedAsync("python","analysis","scope.py")!=null) throw new Exception("Python report survived deletion");
    }
}
