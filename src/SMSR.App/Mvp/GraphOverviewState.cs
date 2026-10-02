using System.Text.Json;
namespace SMSR.App.Mvp;
public sealed partial class GraphOverviewService
{
    private readonly Dictionary<string,Task<GraphOverview>> _jobs=new();
    private readonly object _jobLock=new();
    public async Task<GraphOverviewState> StateAsync(string projectId,int offset=0,int limit=12,CancellationToken shutdown=default)
    {
        Bounds(offset,limit);
        if(EventValidation.ValidateWorkflowIds(projectId,"graph") is{}error)throw new ArgumentException(error);
        var info=await store.GetGraphInfoAsync(projectId,shutdown)??throw new KeyNotFoundException("색인이 없습니다.");
        if(info.NodeCount>50000||info.EdgeCount>200000)throw new InvalidOperationException("그룹 분석 한도는 5만 노드·20만 관계입니다.");
        var raw=await store.GetGraphDerivedAsync(projectId,"overview-v1","",shutdown);
        if(raw is not null&&JsonSerializer.Deserialize<GraphOverview>(raw,GraphWorker.Json)?.Revision==info.Revision)
            return new("READY",info.Revision,await PageAsync(projectId,offset,limit,shutdown),0);
        Task<GraphOverview> job;
        lock(_jobLock)
        {
            if(_jobs.TryGetValue(projectId,out job!)&&job.IsCompleted)
            {
                _jobs.Remove(projectId);
                if(job.IsFaulted||job.IsCanceled)return new("FAILED",info.Revision,null,0,GraphOverviewFailure.Describe(job));
            }
            if(!_jobs.TryGetValue(projectId,out job!))
            {
                foreach(var done in _jobs.Where(p=>p.Value.IsCompleted).Select(p=>p.Key).ToArray())_jobs.Remove(done);
                if(_jobs.Count>=4)throw new InvalidOperationException("최대 4개 그룹 분석이 대기 중입니다.");
                job=SummaryAsync(projectId,shutdown);_jobs[projectId]=job;
                _=job.ContinueWith(t=>{_=t.Exception;},TaskContinuationOptions.OnlyOnFaulted);
            }
        }
        // Observe faults without logging payloads; a later poll returns FAILED.
        return new("ANALYSIS_PENDING",info.Revision,null,5);
    }
}
