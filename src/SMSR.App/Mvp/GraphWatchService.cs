using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
namespace SMSR.App.Mvp;
public sealed record GraphWatchState(string ProjectId,bool Enabled,string Status,int PendingChanges,
    DateTimeOffset? LastIndexedAt,string? Error);
public sealed partial class GraphWatchService(EventStore store,GraphIndexService index):BackgroundService
{
    private sealed class Entry(string project,FileSystemWatcher watcher)
    {
        internal readonly string Project=project;
        internal readonly string Root=watcher.Path;
        internal readonly FileSystemWatcher Watcher=watcher;
        internal readonly CancellationTokenSource Stop=new();
        internal long ChangedAt;
        internal int Pending;
        internal bool Running;
        internal DateTimeOffset? Last;
        internal string? Error;
    }
    private readonly Dictionary<string,Entry> _entries=new();
    private readonly object _sync=new();
    private readonly SemaphoreSlim _settings=new(1,1);
    public GraphWatchState Get(string projectId)
    {
        if(EventValidation.ValidateWorkflowIds(projectId,"watch") is{}error)throw new ArgumentException(error);
        lock(_sync)return _entries.TryGetValue(projectId,out var e)
            ?new(projectId,true,e.Running?"INDEXING":e.Error is not null?"FAILED":e.Pending>0?"WAITING":"WATCHING",e.Pending,e.Last,e.Error)
            :new(projectId,false,"DISABLED",0,null,null);
    }
    public async Task<GraphWatchState> SetAsync(string projectId,bool enabled,CancellationToken ct=default)
    {
        _=Get(projectId);await _settings.WaitAsync(ct);
        try
        {
            var info=await store.GetGraphInfoAsync(projectId,ct)??throw new KeyNotFoundException("색인된 프로젝트만 감시할 수 있습니다.");
            if(enabled)await EnableAsync(projectId,info.RootPath,ct);
            else lock(_sync)if(_entries.Remove(projectId,out var old)){old.Stop.Cancel();old.Watcher.Dispose();}
            try {await store.SaveGraphDerivedAsync(projectId,"watch-config","",info.Revision,JsonSerializer.Serialize(enabled),ct);}
            catch {lock(_sync)if(_entries.Remove(projectId,out var old)){old.Stop.Cancel();old.Watcher.Dispose();}throw;}
            return Get(projectId);
        }
        finally {_settings.Release();}
    }
    private void Mark(Entry entry,string? path=null)
    {
        if(path is not null&& !GraphWatchPath.Allowed(entry.Root,path))return;
        lock(_sync){if(!_entries.TryGetValue(entry.Project,out var live)||live!=entry)return;
            entry.Pending=Math.Min(10000,entry.Pending+1);entry.ChangedAt=Environment.TickCount64;entry.Error=null;}
    }
}
