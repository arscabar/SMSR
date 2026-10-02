namespace SMSR.App.Mvp;
internal static class GraphOverviewPollSelfCheck
{
    internal static async Task RunAsync(EventStore store,string root)
    {
        GraphOverviewFailureSelfCheck.Run();
        var nodes=Enumerable.Range(0,10).Select(i=>new GraphNode("p"+i,"poll.py","symbol","Poll"+i,"poll.py",1,"fixture")).ToArray();
        var edges=Enumerable.Range(0,9).Select(i=>new GraphEdge("p"+i,"p"+(i+1),"CALLS","poll.py",1,"RESOLVED","EXTRACTED")).ToArray();
        await GraphReportRetrySelfCheck.RunAsync(store,root,nodes,edges);
        await store.ApplyGraphScanAsync("poll",new(root,[new("poll.py","fixture","code")],nodes,edges,[],["poll.py"],[],[]),false);
        var service=new GraphOverviewService(store,new GraphWorker());var pending=await service.StateAsync("poll");
        if(pending.Status!="ANALYSIS_PENDING"||pending.RetryAfterSeconds!=5)throw new Exception("Uncached overview held the request");
        var until=DateTimeOffset.UtcNow.AddSeconds(30);GraphOverviewState state=pending;
        while(state.Status=="ANALYSIS_PENDING"&&DateTimeOffset.UtcNow<until){await Task.Delay(1000);state=await service.StateAsync("poll");}
        if(state.Status!="READY"||state.Page?.ScannedNodes!=10)throw new Exception("Background group result not reusable");
        var cancelled=new GraphOverviewService(store,new GraphWorker());using var stop=new CancellationTokenSource();stop.Cancel();
        try{await cancelled.StateAsync("poll",shutdown:stop.Token);throw new Exception("Shutdown cancellation ignored");}catch(OperationCanceledException){}
    }
}
