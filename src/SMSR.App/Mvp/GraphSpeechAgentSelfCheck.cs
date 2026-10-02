using System.IO;
using System.Text.Json;
namespace SMSR.App.Mvp;
internal static class GraphSpeechAgentSelfCheck
{
    internal static async Task RunAsync(string root,EventStore store,GraphIndexService index)
    {
        using var fixture=JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory,"docs/test-data/media-speech-agent-2026-10-01.json")));
        var data=fixture.RootElement;var path="speech.wav";
        File.Copy(Path.Combine(Environment.CurrentDirectory,data.GetProperty("source").GetString()!),Path.Combine(root,path));
        await index.IndexAsync("documents",root);
        var docs=new GraphDocumentService(store,new GraphWorker());var doc=await docs.ReadAsync("documents",path);
        var nodes=data.GetProperty("nodes").EnumerateArray().Select(n=>new GraphSemanticNode(n[0].GetString()!,n[1].GetString()!,n[2].GetString()!,n[3].GetString()!,n[4].GetString()!,false)).ToArray();
        var edges=data.GetProperty("edges").EnumerateArray().Select(e=>new GraphSemanticEdge(e[0].GetString()!,e[1].GetString()!,e[2].GetString()!,nodes[e[3].GetInt32()].Location,nodes[e[3].GetInt32()].Quote,false)).ToArray();
        var service=new GraphSemanticService(store,docs);
        var saved=await service.SubmitAsync(new("documents",path,doc.SourceHash,doc.Revision,data.GetProperty("model").GetString()!,"semantic-v1",nodes,edges));
        if(saved.Nodes.Length!=6||saved.Edges.Length!=4||(await service.ReadAsync("documents",path)).Stale)throw new Exception("Speech semantic time evidence acceptance failed");
        var html=GraphDocumentPage.Render("documents",path,doc,nodes[2].Location,null);
        if(!html.Contains("#t=2")||!html.Contains("<audio controls"))throw new Exception("Speech evidence playback location missing");
        File.Copy(Path.Combine(Environment.CurrentDirectory,"artifacts/media-acceptance/scan.pdf"),Path.Combine(root,"scan.pdf"));
        await index.IndexAsync("documents",root);
        var pdf=await docs.ReadAsync("documents","scan.pdf");
        var word=pdf.Blocks.First(b=>b.Text=="Store");
        if(!GraphDocumentPage.Render("documents","scan.pdf",pdf,word.Location,null).Contains("media-region")
            ||(await docs.PreviewAsync("documents","scan.pdf",1,default)).Length<100)throw new Exception("Scanned PDF evidence region/preview failed");
    }
}
