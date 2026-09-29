using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace SMSR.App.Mvp;

internal static class GraphJdtProductSelfCheck
{
    internal static async Task RunAsync()
    {
        await GraphJdtCleanupSelfCheck.RunAsync();
        var root=Path.Combine(Path.GetTempPath(),"smsr-jdt-product-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var sources=GraphJdtFixture.Sources.ToDictionary(p=>"src/sample/"+p.Key,p=>"package sample;\r\n"+p.Value);
            var store=await GraphJdtProject.CreateAsync(root,sources,["src"],default);
            var settings=Path.Combine(root,"project",".project");await File.WriteAllTextAsync(settings,"SOURCE_SETTINGS_MUST_NOT_RUN");
            await using var host=await LocalServer.StartAsync(root,0);
            using var client=new HttpClient();
            var path="src/sample/Entry.java";var line=2;
            var column=sources[path].Split('\n')[line].IndexOf("pick",StringComparison.Ordinal);
            var request=new GraphJdtRequest("snapshot",sources.Keys.ToArray(),path,line,column,["src"]);
            Verify(await new GraphAdvancedService(store,new GraphWorker()).QueryJdtAsync(request));
            using var denied=await client.PostAsJsonAsync(host.Address+"/api/graph/jdt",request);
            if(denied.StatusCode!=HttpStatusCode.Unauthorized) throw new Exception("JDT origin guard missing");
            client.DefaultRequestHeaders.Add("Origin",host.Address);
            using var response=await client.PostAsJsonAsync(host.Address+"/api/graph/jdt",request);
            if(!response.IsSuccessStatusCode) throw new Exception("JDT HTTP failed: "+await response.Content.ReadAsStringAsync());
            Verify(await response.Content.ReadFromJsonAsync<JsonElement>());
            await GraphJdtStorageSelfCheck.RunAsync(root,host.Address,client,store,request);
            if(await File.ReadAllTextAsync(settings)!="SOURCE_SETTINGS_MUST_NOT_RUN") throw new Exception("Original project settings changed");
            await GraphJdtBoundarySelfCheck.RunAsync(root);
        }
        finally { Directory.Delete(root,true); }
    }
    internal static void Verify(JsonElement report)
    {
        var hits=report.GetProperty("candidates");
        if(report.GetProperty("status").GetString()!="LSP_DEFINITION_CANDIDATE" || hits.GetArrayLength()!=1 ||
            hits[0].GetProperty("path").GetString()!="src/sample/한글.java" ||
            hits[0].GetProperty("range").GetProperty("start").GetProperty("line").GetInt32()!=2 ||
            hits[0].GetProperty("range").GetProperty("start").GetProperty("character").GetInt32()!=12 ||
            hits[0].GetProperty("range").GetProperty("end").GetProperty("character").GetInt32()!=16)
            throw new Exception("Published JDT definition mismatch");
    }
}
