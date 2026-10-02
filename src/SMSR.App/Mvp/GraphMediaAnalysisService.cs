using System.IO;
using System.Text.Json;
namespace SMSR.App.Mvp;
public sealed partial class GraphMediaAnalysisService(EventStore store,GraphWorker worker)
{
    private readonly Dictionary<string,Task<GraphMediaAnalysis>> _jobs=new();
    private readonly object _sync=new();
    public async Task<GraphMediaAnalysis> ReadAsync(string projectId,string path,CancellationToken ct=default)
    {
        var result=await await AcquireAsync(projectId,path,ct);
        if((await store.GetGraphInfoAsync(projectId,ct))?.Revision!=result.Revision)throw new InvalidOperationException("조회 중 색인이 변경됐습니다.");
        return result;
    }
    internal async Task<Task<GraphMediaAnalysis>> AcquireAsync(string projectId,string path,CancellationToken ct,bool retry=false)
    {
        var info=await store.GetGraphInfoAsync(projectId,ct)??throw new KeyNotFoundException("색인이 없습니다.");
        // Validate current disk hash even on a cache hit.
        await using var validated=(await new GraphMediaService(store).OpenAsync(projectId,path,ct)).Stream;
        var key=projectId+"\n"+path+"\n"+info.Revision+"\n"+GraphMediaRuntime.Fingerprint();
        Task<GraphMediaAnalysis> job;
        lock(_sync)
        {
            if(retry&&_jobs.TryGetValue(key,out var failed)&&(failed.IsFaulted||failed.IsCanceled))_jobs.Remove(key);
            if(!_jobs.TryGetValue(key,out job!))
            {
                foreach(var old in _jobs.Where(p=>p.Value.IsCompleted).Select(p=>p.Key).Take(Math.Max(0,_jobs.Count-3)).ToArray())_jobs.Remove(old);
                if(_jobs.Count>=4)throw new InvalidOperationException("최대 4개 미디어 분석만 대기할 수 있습니다.");
                job=RunAsync(projectId,path,ct);_jobs.Add(key,job);
                _=job.ContinueWith(t=>{_=t.Exception;},TaskContinuationOptions.OnlyOnFaulted);
            }
        }
        return job;
    }
    private async Task<GraphMediaAnalysis> RunAsync(string projectId,string path,CancellationToken ct)
    {
        var info=await store.GetGraphInfoAsync(projectId,ct)??throw new KeyNotFoundException("색인이 없습니다.");
        await using var source=(await new GraphMediaService(store).OpenAsync(projectId,path,ct)).Stream;
        var node=await store.GetGraphNodeAsync(projectId,"file:"+path,ct,info.Revision)??throw new KeyNotFoundException("미디어가 없습니다.");
        var fingerprint=GraphMediaRuntime.Fingerprint();
        var raw=await worker.RunMediaAsync(new{operation="media",root=info.RootPath,path=Path.GetFullPath(Path.Combine(info.RootPath,path)),hash=node.Hash},ct);
        var result=raw.Deserialize<GraphMediaAnalysis>(GraphWorker.Json)??throw new InvalidOperationException("미디어 분석 실패");
        if((await store.GetGraphInfoAsync(projectId,ct))?.Revision!=info.Revision)throw new InvalidOperationException("분석 중 색인이 변경됐습니다.");
        if(fingerprint!=GraphMediaRuntime.Fingerprint())throw new InvalidOperationException("분석 중 로컬 모델·도구가 변경됐습니다.");
        var safe=result.Blocks.Where(b=>!GraphDocumentRedaction.Excluded(b.Text)).ToArray();
        return result with{Revision=info.Revision,Blocks=safe,ExcludedBlocks=result.ExcludedBlocks+result.Blocks.Length-safe.Length,ExtractionFingerprint=fingerprint};
    }
}
