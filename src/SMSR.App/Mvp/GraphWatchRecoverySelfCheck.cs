using System.IO;
using System.Reflection;
namespace SMSR.App.Mvp;
internal static class GraphWatchRecoverySelfCheck
{
    internal static async Task RunAsync(string root,EventStore store)
    {
        var index=new GraphIndexService(store);var service=new GraphWatchService(store,index);
        await service.SetAsync("documents",true);
        var entries=(System.Collections.IDictionary)typeof(GraphWatchService).GetField("_entries",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(service)!;
        var entry=entries["documents"]!;var mark=typeof(GraphWatchService).GetMethod("Mark",BindingFlags.NonPublic|BindingFlags.Instance)!;
        for(var i=0;i<10010;i++)mark.Invoke(service,[entry,null]); // Same path as actual watcher.Error overflow callback.
        if(service.Get("documents").PendingChanges!=10000)throw new Exception("Watch queue unbounded");
        await service.SetAsync("documents",false);service.Dispose();
        await store.SaveGraphDerivedAsync("documents","watch-config","",(await store.GetGraphInfoAsync("documents"))!.Revision,"true");
        using var restored=new GraphWatchService(store,index);await restored.StartAsync(default);
        for(var i=0;i<20&&!restored.Get("documents").Enabled;i++)await Task.Delay(100);
        if(!restored.Get("documents").Enabled)throw new Exception("Saved watch configuration not restored");
        await restored.SetAsync("documents",false);await restored.StopAsync(default);
        var before=(await store.GetGraphInfoAsync("documents"))!.Revision;
        await File.AppendAllTextAsync(Path.Combine(root,"plan.md"),"\nAfter disabled\n");await Task.Delay(3500);
        if((await store.GetGraphInfoAsync("documents"))!.Revision!=before)throw new Exception("Disabled watcher still indexed");
        await GraphWatchBusySelfCheck.RunAsync(root,store);
    }
}
