namespace SMSR.App.Mvp;
internal static class GraphReportRetrySelfCheck
{
    internal static async Task RunAsync(EventStore store,string root,GraphNode[] nodes,GraphEdge[] edges)
    {
        await store.ApplyGraphScanAsync("report-retry",new(root,[new("poll.py","fixture","code")],nodes,edges,[],["poll.py"],[],[]),false);
        var reports=new GraphReportService(store,new GraphOverviewService(store,new GraphWorker()));
        using var stop=new CancellationTokenSource();
        var first=await reports.GetAsync("report-retry",stop.Token);stop.Cancel();
        if(first.Status!="ANALYSIS_PENDING")throw new Exception("Report not asynchronous");
        var until=DateTimeOffset.UtcNow.AddSeconds(30);var result=first;
        while(result.Status=="ANALYSIS_PENDING"&&DateTimeOffset.UtcNow<until)
        {await Task.Delay(100);result=await reports.GetAsync("report-retry");}
        if(result.Status!="FAILED"||!result.Limits.Any(l=>l.StartsWith("ANALYSIS_CANCELLED")))
            throw new Exception("Cancelled report failure hidden");
        result=await reports.GetAsync("report-retry");until=DateTimeOffset.UtcNow.AddSeconds(30);
        while(result.Status=="ANALYSIS_PENDING"&&DateTimeOffset.UtcNow<until)
        {await Task.Delay(500);result=await reports.GetAsync("report-retry");}
        if(result.Status!="READY")throw new Exception("Explicit report retry failed");
    }
}
