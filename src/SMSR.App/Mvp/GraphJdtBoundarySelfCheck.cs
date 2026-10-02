using System.IO;

namespace SMSR.App.Mvp;

internal static class GraphJdtBoundarySelfCheck
{
    internal static async Task RunAsync(string root)
    {
        await RejectAsync<KeyNotFoundException>(Path.Combine(root,"filename"),"Secret.java",
            "class Secret { }","비밀 파일은 열 수 없습니다.");
        await RejectAsync<InvalidOperationException>(Path.Combine(root,"content"),"Boundary.java",
            "class Boundary { String api_key=\"fixture-secret-value\"; }","민감 패턴");
        using var cancelled=new CancellationTokenSource();cancelled.Cancel();
        await RejectAsync<OperationCanceledException>(Path.Combine(root,"cancelled"),"Safe.java",
            "class Safe { }",null,cancelled.Token);
    }

    private static async Task RejectAsync<T>(string root,string path,string source,string? message,
        CancellationToken ct=default) where T:Exception
    {
        var store=await GraphJdtProject.CreateAsync(root,new Dictionary<string,string>{{path,source}},[""],default);
        var request=new GraphJdtRequest("snapshot",[path],path,0,6);
        var service=new GraphAdvancedService(store,new GraphWorker());
        try { await service.QueryJdtAsync(request,ct); }
        catch(T error) when(message is null || error.Message.Contains(message,StringComparison.Ordinal))
        {
            if(await store.GetGraphDerivedAsync("snapshot","jdt",request.Normalize().Key)!=null)
                throw new Exception($"Rejected JDT result saved: {path}");
            return;
        }
        throw new Exception($"JDT boundary accepted: {path}");
    }
}
