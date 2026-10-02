using System.IO;
using System.Net.Http;
namespace SMSR.App.Mvp;
internal static class GraphWatchLifecycleSelfCheck
{
    internal static async Task RunAsync(string root,EventStore store,HttpClient client,string address)
    {
        var path=Path.Combine(root,"watch-added.md");
        async Task Changed(Func<Task> action)
        {
            var revision=(await store.GetGraphInfoAsync("documents"))!.Revision;
            await action();
            for(var i=0;i<40;i++){await Task.Delay(500);if((await store.GetGraphInfoAsync("documents"))!.Revision>revision)return;}
            throw new Exception("Watch create/rename/delete did not update");
        }
        await Changed(()=>File.WriteAllTextAsync(path,"Watch evidence"));
        await Changed(()=>{File.Move(path,Path.Combine(root,"watch-renamed.md"));return Task.CompletedTask;});
        await Changed(()=>{File.Delete(Path.Combine(root,"watch-renamed.md"));return Task.CompletedTask;});
        var current=(await store.GetGraphInfoAsync("documents"))!.Revision;
        if(await store.GetGraphNodeAsync("documents","file:watch-renamed.md",default,current)is not null)throw new Exception("Watch deletion retained file");
    }
}
