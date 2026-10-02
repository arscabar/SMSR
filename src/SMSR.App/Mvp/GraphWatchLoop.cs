using System.IO;
namespace SMSR.App.Mvp;
public sealed partial class GraphWatchService
{
    private async Task EnableAsync(string projectId,string root,CancellationToken ct)
    {
        root=Path.GetFullPath(root);
        if(!Directory.Exists(root)||new DirectoryInfo(root).Attributes.HasFlag(FileAttributes.ReparsePoint))
            throw new InvalidOperationException("감시할 실제 프로젝트 폴더가 없습니다.");
        _=await GraphFileScanner.FoldersAsync(root,ct);
        lock(_sync)
        {
            if(_entries.Remove(projectId,out var old)){old.Stop.Cancel();old.Watcher.Dispose();}
            if(_entries.Count>=8)throw new InvalidOperationException("동시에 8개 프로젝트까지 감시합니다.");
            var watcher=new FileSystemWatcher(root){IncludeSubdirectories=true,NotifyFilter=NotifyFilters.FileName|NotifyFilters.DirectoryName|NotifyFilters.LastWrite|NotifyFilters.Size};
            var entry=new Entry(projectId,watcher);_entries.Add(projectId,entry);
            watcher.Changed+=(_,e)=>Mark(entry,e.FullPath);watcher.Created+=(_,e)=>Mark(entry,e.FullPath);
            watcher.Deleted+=(_,e)=>Mark(entry,e.FullPath);
            watcher.Renamed+=(_,e)=>{Mark(entry,e.OldFullPath);Mark(entry,e.FullPath);};
            watcher.Error+=(_,_)=>Mark(entry);watcher.EnableRaisingEvents=true;Mark(entry);
        }
    }
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        foreach(var project in await store.GetGraphProjectsAsync(ct))
        {
            if(await store.GetGraphDerivedAsync(project.ProjectId,"watch-config","",ct)!="true")continue;
            try{await EnableAsync(project.ProjectId,project.RootPath,ct);}
            catch(Exception e)when(e is IOException or ArgumentException or InvalidOperationException){/* Missing roots remain off; explicit enable retries. */}
        }
        try
        {
            while(!ct.IsCancellationRequested)
            {
                await Task.Delay(500,ct);Entry[] ready;
                lock(_sync){ready=_entries.Values.Where(e=>e.Pending>0&&!e.Running&&Environment.TickCount64-e.ChangedAt>=3000).ToArray();foreach(var e in ready){e.Running=true;e.Pending=0;}}
                // ponytail: shared index gate serializes manual/hooks/watch; only add parallel workers after measurement.
                foreach(var e in ready)
                {
                    using var linked=CancellationTokenSource.CreateLinkedTokenSource(ct,e.Stop.Token);
                    try{await index.IndexAsync(e.Project,e.Root,null,false,linked.Token,true);e.Last=DateTimeOffset.UtcNow;}
                    catch(OperationCanceledException)when(e.Stop.IsCancellationRequested){}
                    catch(Exception error)when(error is not OperationCanceledException){e.Error="자동 색인 실패: 기존 결과를 유지했습니다. 다시 켜거나 파일 변경 후 재시도하세요.";}
                    finally{lock(_sync)e.Running=false;}
                }
            }
        }
        catch(OperationCanceledException)when(ct.IsCancellationRequested){}
        finally{lock(_sync){foreach(var e in _entries.Values){e.Stop.Cancel();e.Watcher.Dispose();}_entries.Clear();}}
    }
}
