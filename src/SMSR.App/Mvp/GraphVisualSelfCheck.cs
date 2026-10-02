using System.Net.Http;
using System.Net.Http.Json;
namespace SMSR.App.Mvp;
internal static class GraphVisualSelfCheck
{
    internal static async Task RunAsync(HttpClient client,string url)
    {
        var visual=await client.GetFromJsonAsync<GraphVisualSnapshot>(url+"visual?projectId=knowledge");
        if(visual?.Mode!="nodes"||visual.Nodes.Length!=5||visual.Hyperedges.Single().Nodes.Length!=3
            ||visual.Edges.Single().Evidence.Single().Evidence?.SourceHash!=GraphKnowledgeFixture.Hash)
            throw new Exception("Visual original node/direction/provenance/hyperedge roundtrip failed");
        using(var stale=await client.GetAsync(url+"visual?projectId=knowledge&revision=0"))
            if((int)stale.StatusCode!=409)throw new Exception("Stale visual revision accepted");
        using(var missing=await client.GetAsync(url+"visual?projectId=knowledge&groupId=-99"))
            if((int)missing.StatusCode!=404)throw new Exception("Unknown visual group accepted");
        var nodes=Enumerable.Range(0,6000).Select(i=>new GraphNode("n"+i,"a.ts","symbol","N"+i,"a.ts",1,"hash")).ToArray();
        var groups=Enumerable.Range(0,60).Select(i=>new GraphOverviewGroup(i,"Group"+i,100,.5,nodes.Skip(i*100).Take(100).Select(n=>n.NodeId).ToArray(),[])).ToArray();
        var membership=groups.SelectMany(g=>g.MemberIds.Select(id=>(id,g.Id))).ToDictionary(p=>p.id,p=>p.Id);
        var edge=new GraphEdge("n1","n101","CALLS","a.ts",2,"RESOLVED","EXTRACTED");
        var summary=new GraphOverview(1,"fixture",groups,[],[],[],[],0,6000,1,false,"fixture");
        var meta=GraphVisualProjection.Build(summary,nodes,[edge],membership,groups.ToDictionary(g=>g.Id),true);
        if(meta.Nodes.Length!=60||meta.OmittedNodes!=0||meta.Edges.Single().SourceId!="group:0"
            ||meta.Edges.Single().TargetId!="group:1"||meta.Edges.Single().Evidence.Single()!=edge)
            throw new Exception("Large visual projection lost groups/direction/evidence");
        var capped=GraphVisualProjection.Build(summary,nodes,[],membership,groups.ToDictionary(g=>g.Id),false);
        if(capped.Nodes.Length!=5000||capped.OmittedNodes!=1000)throw new Exception("Visual ceiling not disclosed");
    }
}
