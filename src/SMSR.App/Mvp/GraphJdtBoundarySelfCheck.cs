namespace SMSR.App.Mvp;

internal static class GraphJdtBoundarySelfCheck
{
    internal static async Task RunAsync(string root)
    {
        var sources=new Dictionary<string,string>{{"Secret.java","class Secret { String api_key=\"fixture-secret-value\"; }"}};
        var store=await GraphJdtProject.CreateAsync(root,sources,[""],default);
        var request=new GraphJdtRequest("snapshot",["Secret.java"],"Secret.java",0,6);
        var service=new GraphAdvancedService(store,new GraphWorker());
        try { await service.QueryJdtAsync(request);throw new Exception("Sensitive Java source accepted"); }
        catch(InvalidOperationException error) when(error.Message.Contains("민감 패턴")) { }
        if(await store.GetGraphDerivedAsync("snapshot","jdt",request.Normalize().Key)!=null)
            throw new Exception("Sensitive JDT result saved");
        using var cancelled=new CancellationTokenSource();cancelled.Cancel();
        try { await service.QueryJdtAsync(request,cancelled.Token);throw new Exception("JDT cancellation ignored"); }
        catch(OperationCanceledException) { }
    }
}
