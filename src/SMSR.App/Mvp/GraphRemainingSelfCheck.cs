using System.IO;
using System.Net.Http;
using System.Xml.Linq;
namespace SMSR.App.Mvp;
internal static class GraphRemainingSelfCheck
{
    internal static async Task RunAsync(string root,EventStore store)
    {
        var export=new GraphExportService(store,new GraphQueryService(store));
        var data=await export.GetAsync("overview");
        if(data.Nodes.Count!=11||data.Edges.Count!=21||data.Truncated)throw new Exception("Full graph export mismatch");
        _=XDocument.Parse(GraphExportGraphML.Render(data));
        _=XDocument.Parse(GraphExportSvg.Render(data));
        var attack=data with{Nodes=[data.Nodes[0] with{Label="</script><script>alert(1)</script>"}]};
        if(GraphExportHtml.Render(attack).Contains("</script><script>alert(1)"))throw new Exception("Export script injection");
        if(GraphExportWiki.Render(data,true).Length<100)throw new Exception("Wiki export missing");
        if(GraphWatchPath.Allowed(root,Path.Combine(root,".git/index"))||GraphWatchPath.Allowed(root,Path.Combine(root,"secrets/token.py"))
            ||GraphWatchPath.Allowed(root,Path.GetFullPath(Path.Combine(root,"../outside.py"))))throw new Exception("Watch boundary failed");
        await using var host=await LocalServer.StartAsync(root,0);using var client=new HttpClient();client.DefaultRequestHeaders.Add("Origin",host.Address);
        var response=await client.GetAsync(host.Address+"/api/graph/knowledge-export?projectId=overview&format=graphml");
        if(!response.IsSuccessStatusCode)throw new Exception("Export HTTP failed");
        if(!(await client.GetAsync(host.Address+"/assets/graph-explorer-knowledge.js")).IsSuccessStatusCode)throw new Exception("Knowledge module missing");
        if(!(await client.GetAsync(host.Address+"/assets/graph-explorer-media.js")).IsSuccessStatusCode)throw new Exception("Media module missing");
        if(!(await client.GetAsync(host.Address+"/assets/graph-explorer-export.js")).IsSuccessStatusCode)throw new Exception("Selection export module missing");
        using var enabled=await client.PostAsync(host.Address+"/api/graph/watch",new StringContent("{\"projectId\":\"documents\",\"enabled\":true}",System.Text.Encoding.UTF8,"application/json"));
        if(!enabled.IsSuccessStatusCode)throw new Exception("Watch enable failed");
        await File.AppendAllTextAsync(Path.Combine(root,"plan.md"),"\nWatcher requirement\n");
        var before=(await store.GetGraphInfoAsync("documents"))!.Revision;var updated=false;
        for(var i=0;i<40;i++){await Task.Delay(500);if((await store.GetGraphInfoAsync("documents"))!.Revision>before){updated=true;break;}}
        if(!updated)throw new Exception("Actual file watch did not index");
        await GraphWatchLifecycleSelfCheck.RunAsync(root,store,client,host.Address);
        using var disabled=await client.PostAsync(host.Address+"/api/graph/watch",new StringContent("{\"projectId\":\"documents\",\"enabled\":false}",System.Text.Encoding.UTF8,"application/json"));
        if(!disabled.IsSuccessStatusCode)throw new Exception("Watch disable failed");
        var tool=await new McpHttpGateway(host.Address,root).CallAsync("get_graph_watch",new{projectId="documents"});
        if(!tool.Contains("DISABLED"))throw new Exception("Watch MCP mismatch");
        await GraphWatchRecoverySelfCheck.RunAsync(root,store);
    }
}
