using System.Text;
using System.Text.Json;

namespace SMSR.App.Mvp;

public sealed partial class GraphAdvancedService
{
    private const int TypeScriptAnalysisVersion=8;
    internal async Task<JsonElement> AnalyzeTypeScriptAsync(GraphTypeScriptRequest request,CancellationToken ct=default)
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
            if(size>16*1024*1024) throw new ArgumentException("TypeScript 묶음은 전체 16 MiB 이하여야 합니다.");
            files.Add(new {path,text=file.Text}); hashes.Add(path,file.Hash);
        }
        var result=await worker.RunAsync(new {operation="typescript",files},ct);
        foreach(var pair in hashes)
            if((await reader.ReadAsync(request.ProjectId,pair.Key,ct)).Hash!=pair.Value)
                throw new InvalidOperationException("분석 도중 원문이 변경됐습니다.");
        var payload=JsonSerializer.Serialize(new {analysisVersion=TypeScriptAnalysisVersion,revision=info.Revision,
            analyzedAt=DateTimeOffset.UtcNow,inputHashes=hashes,result},GraphWorker.Json);
        await store.SaveGraphDerivedAsync(request.ProjectId,"typescript",request.Key,info.Revision,payload,ct);
        return JsonSerializer.Deserialize<JsonElement>((await store.GetGraphDerivedAsync(request.ProjectId,"typescript",request.Key,ct))!);
    }
    internal async Task<object> TypeScriptAnalysisAsync(GraphTypeScriptRequest request,CancellationToken ct=default)
    {
        request=request.Normalize(); var info=await RequireAsync(request.ProjectId,ct);
        var saved=await store.GetGraphDerivedAsync(request.ProjectId,"typescript",request.Key,ct)
            ?? throw new KeyNotFoundException("같은 TypeScript 파일 묶음의 분석 결과가 없습니다.");
        var report=JsonSerializer.Deserialize<JsonElement>(saved);
        var stale=!await store.IsGraphDeepReportCurrentAsync(request.ProjectId,"typescript",request.Key,report,ct);
        foreach(var pair in report.GetProperty("inputHashes").EnumerateObject())
            try { stale|=(await new GraphSourceService(store).ReadAsync(request.ProjectId,pair.Name,ct)).Hash!=pair.Value.GetString(); }
            catch(Exception e) when(e is InvalidOperationException or KeyNotFoundException or System.IO.IOException or UnauthorizedAccessException) { stale=true; }
        return new {report,currentRevision=info.Revision,stale};
    }
}
