using System.IO;
using System.Text.Json;
namespace SMSR.App.Mvp;
internal static class GraphQuestionSelfCheck
{
    internal static async Task RunAsync(string root,EventStore store)
    {
        var nodes=new[]{new GraphNode("a","entry.py","symbol","SavePayment","entry.py",1,"hash"),
            new GraphNode("b","ledger.py","symbol","PaymentLedger","ledger.py",1,"hash"),
            new GraphNode("c","uncertain.py","symbol","UnknownHandler","uncertain.py",1,"hash"),
            new GraphNode("d","copy.py","symbol","SavePayment","copy.py",1,"hash")};
        var edges=new[]{new GraphEdge("a","b","CALLS","entry.py",1,"RESOLVED","EXTRACTED"),new GraphEdge("a","c","CALLS","entry.py",2,"CANDIDATE","INFERRED")};
        GraphScan Scan(string hash)=>new(root,nodes.Select(n=>new GraphFile(n.SourcePath,hash,"code")).ToArray(),nodes.Select(n=>n with{Hash=hash}).ToArray(),edges,[],nodes.Select(n=>n.SourcePath).ToArray(),[],[]);
        await store.ApplyGraphScanAsync("question",Scan("hash"),false);
        var feedback=new GraphFeedbackService(store);var service=new GraphQuestionService(store,feedback);
        var request=new GraphQuestionRequest("question","entry raw_question_canary_82ad",MaxTokens:8000,Direction:"outgoing",Depth:1,Relation:"CALLS");
        var search=await service.SearchAsync(request);
        if(search.Hits.Length!=1||!(await service.SearchAsync(new("question","save payment"))).Hits.Any(h=>h.Ambiguous)
            ||(await service.SearchAsync(new("question","不存在"))).Hits.Length!=0)throw new Exception("Vocabulary/relevance/ambiguity failed");
        var evidence=await service.EvidenceAsync(request);
        if(evidence.Edges.Length!=1||evidence.Edges[0]!=edges[0]||(await service.EvidenceAsync(request with{Direction="incoming"})).Edges.Length!=0
            ||(await service.EvidenceAsync(request with{IncludeInferred=true})).Edges.Length!=2)throw new Exception("Direction/candidate evidence policy failed");
        var small=await service.EvidenceAsync(request with{MaxTokens=1024});var encoded=JsonSerializer.SerializeToUtf8Bytes(small,GraphWorker.Json);
        if(encoded.Length>1024||small.ByteCount<encoded.Length||small.Edges.Any(e=>!small.Nodes.Any(n=>n.NodeId==e.SourceId)||!small.Nodes.Any(n=>n.NodeId==e.TargetId)))throw new Exception("Evidence budget/dangling endpoint failed");
        await GraphQuestionFeedbackSelfCheck.RunAsync(store,service,feedback,request,edges,Scan);
        await GraphQuestionHttpSelfCheck.RunAsync(root,request);
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        foreach(var file in Directory.GetFiles(root,"smsr.db*"))
            if(System.Text.Encoding.UTF8.GetString(await File.ReadAllBytesAsync(file)).Contains("raw_question_canary_82ad"))throw new Exception("Raw question persisted");
    }
}
