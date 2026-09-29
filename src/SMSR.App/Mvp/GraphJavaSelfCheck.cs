using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SMSR.App.Mvp;

internal static class GraphJavaSelfCheck
{
    internal static async Task RunAsync(string root)
    {
        var folder=Path.Combine(root,"java-http");
        await using var host=await LocalServer.StartAsync(folder,0);
        var store=new EventStore(Path.Combine(folder,"smsr.db"));
        var files=new List<GraphFile>(); var nodes=new List<GraphNode>();
        var input=new Dictionary<string,string> {
            ["Lib.java"]="class Lib { static int pick(int x){return x;} static String pick(String x){return x;} static int kill(int x){x=0;return x;} }",
            ["Entry.java"]="class Entry { int run(int input){return Lib.pick(input);} } // JAVA_RAW_MARKER",
            ["Control.java"]=GraphJavaControlSelfCheck.Source,
            ["Switches.java"]=GraphJavaSwitchSelfCheck.Source,
            ["Yields.java"]=GraphJavaYieldSelfCheck.Source
        };
        foreach(var (path,text) in input)
        {
            await File.WriteAllTextAsync(Path.Combine(root,path),text,new UTF8Encoding(false));
            var hash=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
            files.Add(new(path,hash,"code")); nodes.Add(new("file:"+path,path,"file",path,path,1,hash));
        }
        await store.ApplyGraphScanAsync("java",new(root,files,nodes,[],[],input.Keys.ToArray(),[],[]),false);
        using var client=new HttpClient(); var request=new GraphJavaRequest("java",input.Keys.ToArray());
        using var denied=await client.PostAsJsonAsync(host.Address+"/api/graph/java",request);
        if(denied.StatusCode!=HttpStatusCode.Unauthorized) throw new Exception("Java origin guard missing");
        client.DefaultRequestHeaders.Add("Origin",host.Address);
        using var response=await client.PostAsJsonAsync(host.Address+"/api/graph/java",request);
        response.EnsureSuccessStatusCode(); Verify(await response.Content.ReadFromJsonAsync<JsonElement>());
        await GraphJavaStorageSelfCheck.RunAsync(root,folder,host.Address,client,store,request);
    }
    internal static void Verify(JsonElement report)
    {
        var result=report.GetProperty("result"); var call=result.GetProperty("calls").EnumerateArray().Single();
        if(report.GetProperty("analysisVersion").GetInt32()!=6 || result.GetProperty("status").GetString()!="BOUND_INPUT_BUNDLE" ||
            call.GetProperty("targetId").GetString()!="source:Lib#pick(int)" ||
            call.GetProperty("arguments")[0].GetProperty("parameterOrdinal").GetInt32()!=0 ||
            call.GetProperty("source").GetProperty("path").GetString()!="Entry.java")
            throw new Exception("Java compiler binding not persisted");
        GraphJavaValuesSelfCheck.Verify(result);
        GraphJavaSummarySelfCheck.Verify(result);
        GraphJavaControlSelfCheck.Verify(result);
        GraphJavaSwitchSelfCheck.Verify(result);
        GraphJavaYieldSelfCheck.Verify(result);
    }
}
