using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SMSR.App.Mvp;

internal static class GraphTypeScriptSelfCheck
{
    internal static async Task RunAsync(string root)
    {
        var folder=Path.Combine(root,"typescript-http");
        await using var host=await LocalServer.StartAsync(folder,0);
        var store=new EventStore(Path.Combine(folder,"smsr.db"));
        var files=new List<GraphFile>(); var nodes=new List<GraphNode>();
        var input=new Dictionary<string,string> {
            ["Lib.ts"]="export function pick<T>(value:T):T{return value;}"+
                "export function continued(a:number){let x=0;for(let i=0;i<1;x=a){i++;continue;}return x;}"+
                "export function broken(a:number){let x=0;for(;;x=a){break;}return x;}",
            ["Entry.ts"]="import {pick} from './Lib'; export const result=pick(1); // TS_RAW_MARKER",
            ["Wrapper.ts"]="import {pick} from './Lib'; export function wrap(a:number){return pick(a);}",
            ["ZControl.ts"]=GraphTypeScriptControlSelfCheck.Source,
            ["ZSwitch.ts"]=GraphTypeScriptSwitchSelfCheck.Source
        };
        foreach(var (path,text) in input)
        {
            await File.WriteAllTextAsync(Path.Combine(root,path),text,new UTF8Encoding(false));
            var hash=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
            files.Add(new(path,hash,"code")); nodes.Add(new("file:"+path,path,"file",path,path,1,hash));
        }
        await store.ApplyGraphScanAsync("typescript",new(root,files,nodes,[],[],input.Keys.ToArray(),[],[]),false);
        using var client=new HttpClient(); var request=new GraphTypeScriptRequest("typescript",input.Keys.ToArray());
        using var denied=await client.PostAsJsonAsync(host.Address+"/api/graph/typescript",request);
        if(denied.StatusCode!=HttpStatusCode.Unauthorized) throw new Exception("TS origin guard missing");
        client.DefaultRequestHeaders.Add("Origin",host.Address);
        using var response=await client.PostAsJsonAsync(host.Address+"/api/graph/typescript",request);
        response.EnsureSuccessStatusCode(); Verify(await response.Content.ReadFromJsonAsync<JsonElement>());
        await GraphTypeScriptStorageSelfCheck.RunAsync(root,folder,host.Address,client,store,request);
    }
    internal static void Verify(JsonElement report)
    {
        var result=report.GetProperty("result"); var call=result.GetProperty("calls").EnumerateArray().Single(c=>c.GetProperty("path").GetString()=="Entry.ts");
        if(report.GetProperty("analysisVersion").GetInt32()!=8 || result.GetProperty("status").GetString()!="COMPILER_BINDINGS" ||
            call.GetProperty("target").GetProperty("path").GetString()!="Lib.ts" ||
            call.GetProperty("arguments")[0].GetProperty("parameterSymbolId").ValueKind!=JsonValueKind.String ||
            call.GetProperty("returnType").GetProperty("kind").GetString()!="NUMBER")
            throw new Exception("TypeScript compiler binding not persisted");
        GraphTypeScriptConnectionSelfCheck.Verify(result);
        GraphTypeScriptValuesSelfCheck.Verify(result);
        GraphTypeScriptLoopsSelfCheck.Verify(result);
        GraphTypeScriptSummarySelfCheck.Verify(result);
        GraphTypeScriptControlSelfCheck.Verify(result);
        GraphTypeScriptSwitchSelfCheck.Verify(result);
    }
}
