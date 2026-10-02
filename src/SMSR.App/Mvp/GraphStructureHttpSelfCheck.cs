using System.Net.Http;
using System.Net.Http.Json;
namespace SMSR.App.Mvp;
internal static class GraphStructureHttpSelfCheck
{
    internal static async Task RunAsync(HttpClient client,string url)
    {
        var structure=await client.GetFromJsonAsync<GraphStructurePage>(url+"structure?projectId=knowledge&path=A.cs&revision=1");
        if(structure?.TotalNodes!=3||structure.External.Single().Path!="B.cs"
            ||structure.Links.Single().Evidence.Single().Evidence?.SourceHash!=GraphKnowledgeFixture.Hash)
            throw new Exception("Structure snapshot HTTP/evidence failed");
        using(var stale=await client.GetAsync(url+"structure?projectId=knowledge&revision=0"))
            if((int)stale.StatusCode!=409)throw new Exception("Stale structure query accepted");
        using(var invalid=await client.GetAsync(url+"structure?projectId=knowledge&path=../outside"))
            if((int)invalid.StatusCode!=400)throw new Exception("Unsafe structure path accepted");
        var found=await client.GetFromJsonAsync<GraphOverviewPage>(url+"overview?projectId=knowledge&q=A.cs");
        var missing=await client.GetFromJsonAsync<GraphOverviewPage>(url+"overview?projectId=knowledge&q=no-such-label");
        if(found?.TotalGroups!=1||found.Groups.Single().Paths?.Contains("A.cs")!=true||missing?.TotalGroups!=0)
            throw new Exception("General group path filter HTTP contract failed");
    }
}
