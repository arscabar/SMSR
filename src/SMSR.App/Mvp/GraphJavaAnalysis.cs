using System.Text;
using System.Text.Json;

namespace SMSR.App.Mvp;

public sealed partial class GraphAdvancedService
{
    private const int JavaAnalysisVersion=6;
    internal async Task<JsonElement> AnalyzeJavaAsync(GraphJavaRequest request,CancellationToken ct=default)
    {
        request=request.Normalize();
        var info=await RequireAsync(request.ProjectId,ct);
        var files=new List<object>(); var hashes=new Dictionary<string,string>(); long size=0;
        var reader=new GraphSourceService(store);
        foreach(var path in request.Paths)
        {
            var file=await reader.ReadAsync(request.ProjectId,path,ct);
            if(file.Revision!=info.Revision) throw new InvalidOperationException("분석 도중 색인이 변경됐습니다.");
            size+=Encoding.UTF8.GetByteCount(file.Text);
            if(size>16*1024*1024) throw new ArgumentException("Java 묶음은 전체 16 MiB 이하여야 합니다.");
            files.Add(new {path,text=file.Text}); hashes.Add(path,file.Hash);
        }
        var result=await worker.RunAsync(new {operation="java",files},ct);
        foreach(var pair in hashes)
            if((await reader.ReadAsync(request.ProjectId,pair.Key,ct)).Hash!=pair.Value)
                throw new InvalidOperationException("분석 도중 원문이 변경됐습니다.");
        var payload=JsonSerializer.Serialize(new {analysisVersion=JavaAnalysisVersion,revision=info.Revision,
            analyzedAt=DateTimeOffset.UtcNow,inputHashes=hashes,result},GraphWorker.Json);
        await store.SaveGraphDerivedAsync(request.ProjectId,"java",request.Key,info.Revision,payload,ct);
        return JsonSerializer.Deserialize<JsonElement>((await store.GetGraphDerivedAsync(request.ProjectId,"java",request.Key,ct))!);
    }
    internal async Task<object> JavaAnalysisAsync(GraphJavaRequest request,CancellationToken ct=default)
    {
        request=request.Normalize(); var info=await RequireAsync(request.ProjectId,ct);
        var saved=await store.GetGraphDerivedAsync(request.ProjectId,"java",request.Key,ct)
            ?? throw new KeyNotFoundException("같은 Java 파일 묶음의 분석 결과가 없습니다.");
        var report=JsonSerializer.Deserialize<JsonElement>(saved);
        var stale=!await store.IsGraphDeepReportCurrentAsync(request.ProjectId,"java",request.Key,report,ct);
        foreach(var pair in report.GetProperty("inputHashes").EnumerateObject())
            try { stale|=(await new GraphSourceService(store).ReadAsync(request.ProjectId,pair.Name,ct)).Hash!=pair.Value.GetString(); }
            catch(Exception e) when(e is InvalidOperationException or KeyNotFoundException or System.IO.IOException or UnauthorizedAccessException) { stale=true; }
        return new {report,currentRevision=info.Revision,stale};
    }
}
