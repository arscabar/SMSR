using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
namespace SMSR.App.Mvp;
internal static class GraphOverviewSelfCheck
{
    internal static async Task RunAsync(string root,EventStore store)
    {
        await GraphOverviewPollSelfCheck.RunAsync(store,root);
        var nodes=Enumerable.Range(0,11).Select(i=>new GraphNode("g"+i,"graph.py","symbol",i==0?"int":"Component"+i,"graph.py",i+1,"fixture")).ToArray();
        var edges=new List<GraphEdge>();foreach(var start in new[]{0,5})for(var i=start;i<start+5;i++)for(var j=i+1;j<start+5;j++)edges.Add(new("g"+i,"g"+j,"CALLS","graph.py",i+1,"RESOLVED","EXTRACTED"));
        edges.Add(new("g4","g5","CALLS","graph.py",5,"RESOLVED","EXTRACTED"));
        var hash=new string('a',64);nodes=nodes.Select(n=>n with{Hash=hash}).ToArray();
        edges=edges.Select(e=>e with{Evidence=new("fixture-host","1",hash)}).ToList();
        await store.ApplyGraphScanAsync("overview",new(root,[new("graph.py",hash,"code")],nodes,edges,[],["graph.py"],[],[]),false);
        var service=new GraphOverviewService(store,new GraphWorker());var overview=await service.PageAsync("overview",limit:1);
        var members=await service.MembersAsync("overview",overview.Groups[0].Id,overview.Revision,limit:2);
        var full=await service.PageAsync("overview");
        if((await service.PageAsync("overview",q:"graph.py")).TotalGroups!=full.TotalGroups
            ||(await service.PageAsync("overview",q:"no-such-label")).TotalGroups!=0
            ||full.Groups.Any(g=>g.Paths?.Single()!="graph.py"))throw new Exception("Group path/name filtering failed");
        if(full.Groups.Sum(g=>g.NodeCount)!=10||full.IsolatedNodes!=1||!members.Truncated||members.Nodes.Length!=2
            ||full.Core.Any(n=>n.Label=="int")||full.Surprises.Any(s=>!edges.Contains(s.Edge)))throw new Exception("Group/core/original-edge summary failed");
        var raw=await store.GetGraphDerivedAsync("overview","overview-v1","");await service.PageAsync("overview");
        if(raw!=await store.GetGraphDerivedAsync("overview","overview-v1",""))throw new Exception("Overview cache changed");
        try{await service.MembersAsync("overview",0,0);throw new Exception("Stale group revision accepted");}catch(InvalidOperationException){}
        await using var host=await LocalServer.StartAsync(root,0);using var client=new HttpClient();
        var http=await client.GetFromJsonAsync<GraphOverviewPage>(host.Address+"/api/graph/overview?projectId=overview");
        var mcp=JsonSerializer.Deserialize<GraphOverviewState>(await new McpHttpGateway(host.Address,root).CallAsync("get_graph_overview",new{projectId="overview"}),GraphWorker.Json);
        if(mcp?.Status!="READY"||http?.TotalGroups!=mcp?.Page?.TotalGroups||http?.Revision!=overview.Revision)throw new Exception("Group HTTP/MCP mismatch");
        var report=await new GraphReportService(store,service).GetAsync("overview");
        var remote=await client.GetFromJsonAsync<GraphReport>(host.Address+"/api/graph/report?projectId=overview");
        var tool=JsonSerializer.Deserialize<GraphReport>(await new McpHttpGateway(host.Address,root).CallAsync("get_graph_report",new{projectId="overview"}),GraphWorker.Json);
        if(report.Revision!=overview.Revision||report.Coverage?.Revision!=report.Revision||remote?.Status!="READY"||tool?.Revision!=report.Revision||report.Limits.Length<3)
            throw new Exception("Report snapshot HTTP/MCP mismatch");
    }
}
