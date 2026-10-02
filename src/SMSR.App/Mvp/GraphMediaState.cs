namespace SMSR.App.Mvp;
public sealed record GraphMediaFrame(string Location,double? TimeSeconds,string Mime,string Data);
public sealed record GraphMediaAnalysis(string SourceHash,string ExtractorVersion,GraphDocumentBlock[] Blocks,
    string Status,int ExcludedBlocks,string Engine,GraphMediaFrame[] Frames,int? Width,int? Height,
    double? DurationSeconds,string? Limitation,int Revision=0,string? ExtractionFingerprint=null);
public sealed record GraphMediaState(string Status,GraphMediaAnalysis? Analysis,int RetryAfterSeconds,string? Error=null);
internal static class GraphMediaPolling
{
    internal static async Task<GraphMediaState> StateAsync(GraphMediaAnalysisService service,string projectId,string path,CancellationToken shutdown,bool retry=false)
    {
        var task=await service.AcquireAsync(projectId,path,shutdown,retry);
        if(!task.IsCompleted)
        {
            _=task.ContinueWith(t=>{_=t.Exception;},TaskContinuationOptions.OnlyOnFaulted);
            return new("ANALYSIS_PENDING",null,3);
        }
        try{return new("READY",await task,0);}
        catch(Exception error)when(error is InvalidOperationException or OperationCanceledException)
        {return new("FAILED",null,0,"로컬 분석 실패·취소. 설치·모델·입력 제한을 확인한 뒤 명시적으로 재시도하세요.");}
    }
}
