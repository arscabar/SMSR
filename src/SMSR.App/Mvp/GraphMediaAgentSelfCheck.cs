using System.IO;
using System.Text.Json;
namespace SMSR.App.Mvp;
internal static class GraphMediaAgentSelfCheck
{
    internal static async Task RunAsync(string root,EventStore store,GraphIndexService index)
    {
        using var fixture=JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory,"docs/test-data/media-semantic-agent-2026-10-01.json")));
        var data=fixture.RootElement;var path="flow.png";
        File.Copy(Path.Combine(Environment.CurrentDirectory,data.GetProperty("source").GetString()!),Path.Combine(root,path));
        await index.IndexAsync("documents",root);
        var docs=new GraphDocumentService(store,new GraphWorker());var doc=await docs.ReadAsync("documents",path);
        var quote=doc.Blocks[0].Text;
        var nodes=data.GetProperty("nodes").EnumerateArray().Select(n=>new GraphSemanticNode(n[0].GetString()!,"concept",n[1].GetString()!,n[2].GetString()!,quote,true)).ToArray();
        var edges=data.GetProperty("edges").EnumerateArray().Select(e=>new GraphSemanticEdge(e[0].GetString()!,e[1].GetString()!,"SEMANTIC_CANDIDATE",e[2].GetString()!,quote,true)).ToArray();
        var service=new GraphSemanticService(store,docs);
        var saved=await service.SubmitAsync(new("documents",path,doc.SourceHash,doc.Revision,data.GetProperty("model").GetString()!,"semantic-v1",nodes,edges));
        var stored=await store.GetGraphRevisionNodesAsync("documents",saved.Revision);
        if(stored.Count(n=>n.OwnerPath==path&&n.Details?.SourceLocation?.StartsWith("image:")==true)!=3
            ||(await service.ReadAsync("documents",path)).Stale)throw new Exception("Actual visual extraction region/semantic acceptance failed");
        try{GraphSemanticValidation.Evidence(doc,"image:899,0,2,10",quote);throw new Exception("Out-of-image region accepted");}catch(ArgumentException){}
        var html=GraphDocumentPage.Render("documents",path,doc,nodes[1].Location,null);
        if(!html.Contains("330,70,240,120")||!html.Contains("media-region"))throw new Exception("Visual evidence region missing");
    }
}
