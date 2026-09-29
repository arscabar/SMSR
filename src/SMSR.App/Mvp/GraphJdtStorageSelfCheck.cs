using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace SMSR.App.Mvp;

internal static class GraphJdtStorageSelfCheck
{
    internal static async Task RunAsync(string root,string address,HttpClient client,EventStore store,GraphJdtRequest request)
    {
        var key=request.Normalize().Key;
        var saved=await store.GetGraphDerivedAsync("snapshot","jdt",key)??throw new Exception("JDT storage missing");
        if(saved.Contains("PRIVATE_JDT") || saved.Contains("class Entry") || saved.Contains(root)) throw new Exception("JDT raw data leaked");
        var gateway=new McpHttpGateway(address,root);
        var args=new {projectId=request.ProjectId,paths=request.Paths,path=request.Path,line=request.Line,character=request.Character,sourceRoots=request.SourceRoots};
        GraphJdtProductSelfCheck.Verify(JsonSerializer.Deserialize<JsonElement>(await gateway.CallAsync("query_graph_java_definition",args)));
        var loaded=JsonSerializer.Deserialize<JsonElement>(await gateway.CallAsync("get_graph_java_definition",args));
        GraphJdtProductSelfCheck.Verify(loaded.GetProperty("report"));
        if(loaded.GetProperty("stale").GetBoolean()) throw new Exception("Fresh JDT marked stale");
        foreach(var invalid in new[]{request with {Paths=[]},request with {Path="../Entry.java"},request with {Line=-1},
            request with {SourceRoots=["../src"]},request with {SourceRoots=["","src"]},request with {SourceRoots=["wrong"]},
            request with {Paths=["src./Entry.java"],Path="src./Entry.java"}})
        {
            using var rejected=await client.PostAsJsonAsync(address+"/api/graph/jdt",invalid);
            if(rejected.StatusCode!=HttpStatusCode.BadRequest) throw new Exception("Invalid JDT selection accepted");
        }
        if((request with {Paths=request.Paths.Reverse().ToArray()}).Normalize().Key!=key) throw new Exception("JDT key unstable");
        await File.AppendAllTextAsync(Path.Combine(root,"project","src/sample/한글.java"),"// changed");
        using var stale=await client.PostAsJsonAsync(address+"/api/graph/jdt-result",request);
        if(!(await stale.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("stale").GetBoolean()) throw new Exception("JDT dependency drift missed");
        saved=await store.GetGraphDerivedAsync("snapshot","jdt",key);
        using var refused=await client.PostAsJsonAsync(address+"/api/graph/jdt",request);
        if(refused.StatusCode!=HttpStatusCode.Conflict || saved!=await store.GetGraphDerivedAsync("snapshot","jdt",key))
            throw new Exception("JDT stale request overwritten previous result");
        await store.DeleteProjectAsync("snapshot");
        if(await store.GetGraphDerivedAsync("snapshot","jdt",key)!=null) throw new Exception("JDT survived deletion");
    }
}
