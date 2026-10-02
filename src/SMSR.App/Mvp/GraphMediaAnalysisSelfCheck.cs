using System.IO;
using System.Text.Json;
namespace SMSR.App.Mvp;
internal static class GraphMediaAnalysisSelfCheck
{
    internal static async Task RunAsync(string root,EventStore store,GraphIndexService index)
    {
        var path="fixture.png";
        // One-pixel PNG, never a user image.
        await File.WriteAllBytesAsync(Path.Combine(root,path),Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII="));
        await index.IndexAsync("documents",root);
        var worker=new GraphWorker();var media=new GraphMediaAnalysisService(store,worker);
        var read=await media.ReadAsync("documents",path);
        if(read.Width!=1||read.Frames.Length!=1||read.Blocks[0].Location!="image:0,0,1,1")throw new Exception("Image region extraction failed");
        var docs=new GraphDocumentService(store,worker,media);var document=await docs.ReadAsync("documents",path);
        var semantics=new GraphSemanticService(store,docs);
        var reason=new GraphSemanticNode("visual","concept","푸른 화면 후보",document.Blocks[0].Location,document.Blocks[0].Text,true);
        var saved=await semantics.SubmitAsync(new("documents",path,document.SourceHash,document.Revision,"fixture-host","semantic-v1",[reason],[]));
        if((await semantics.ReadAsync("documents",path)).Stale)throw new Exception("Media semantic evidence incorrectly stale");
        await GraphSemanticRuntimeSelfCheck.RunAsync(store,semantics,saved);
        await using(var host=await LocalServer.StartAsync(root,0))
        {
            var gateway=new McpHttpGateway(host.Address,root);GraphMediaState? state=null;
            for(var i=0;i<10;i++){state=JsonSerializer.Deserialize<GraphMediaState>(await gateway.CallAsync("read_graph_media",new{projectId="documents",path}),GraphWorker.Json);if(state?.Status=="READY")break;await Task.Delay(500);}
            if(state?.Analysis?.Frames.Length!=1)throw new Exception("Media MCP polling failed");
        }
        await File.WriteAllTextAsync(Path.Combine(root,path),"changed source");
        try{await media.ReadAsync("documents",path);throw new Exception("Stale media accepted");}catch(InvalidOperationException){}
        await index.IndexAsync("documents",root);
        if(!(await semantics.ReadAsync("documents",path)).Stale)throw new Exception("Changed media semantics remained current");
        if((await store.GetGraphRevisionEdgesAsync("documents",(await store.GetGraphInfoAsync("documents"))!.Revision)).Any(e=>e.OwnerPath==path&&e.Relation=="DESCRIBES"))throw new Exception("Stale media facts retained");
        await File.WriteAllTextAsync(Path.Combine(root,"corrupt.png"),"not an image");
        await index.IndexAsync("documents",root);
        var failed=await media.AcquireAsync("documents","corrupt.png",default);
        try{await failed;throw new Exception("Corrupt media accepted");}catch(InvalidOperationException){}
        if((await GraphMediaPolling.StateAsync(media,"documents","corrupt.png",default)).Status!="FAILED"
            ||!ReferenceEquals(failed,await media.AcquireAsync("documents","corrupt.png",default)))throw new Exception("Failed media auto-restarted");
        var retry=await media.AcquireAsync("documents","corrupt.png",default,true);
        if(ReferenceEquals(failed,retry))throw new Exception("Explicit media retry missing");
        try{await retry;}catch(InvalidOperationException){}
    }
}
