using System.IO;
using System.Text.Json;

namespace SMSR.App.Mvp;

public sealed partial class GraphAdvancedService
{
    private async Task<JsonElement> SaveJdtAsync(GraphJdtRequest request,int revision,Dictionary<string,string> hashes,
        IReadOnlyList<GraphLspDefinition> candidates,int excluded,CancellationToken ct)
    {
        var payload=JsonSerializer.Serialize(new {analysisVersion=1,revision,analyzedAt=DateTimeOffset.UtcNow,
            profile="redhat.java-"+GraphJdtInstallation.Version,status="LSP_DEFINITION_CANDIDATE",
            inputHashes=hashes,path=request.Path,position=new GraphLspPosition(request.Line,request.Character),
            sourceRoots=request.SourceRoots,candidates,excluded},GraphWorker.Json);
        await store.SaveGraphDerivedAsync(request.ProjectId,"jdt",request.Key,revision,payload,ct);
        return JsonSerializer.Deserialize<JsonElement>(payload);
    }
    internal async Task<object> JdtResultAsync(GraphJdtRequest request,CancellationToken ct=default)
    {
        request=request.Normalize();var info=await RequireAsync(request.ProjectId,ct);
        var saved=await store.GetGraphDerivedAsync(request.ProjectId,"jdt",request.Key,ct)
            ?? throw new KeyNotFoundException("같은 입력 파일·위치·소스 루트의 정의 조회 결과가 없습니다.");
        var report=JsonSerializer.Deserialize<JsonElement>(saved);
        var stale=report.GetProperty("revision").GetInt32()!=info.Revision || report.GetProperty("analysisVersion").GetInt32()!=1;
        foreach(var hash in report.GetProperty("inputHashes").EnumerateObject())
            try { stale|=(await new GraphSourceService(store).ReadAsync(request.ProjectId,hash.Name,ct)).Hash!=hash.Value.GetString(); }
            catch(Exception e) when(e is InvalidOperationException or IOException or KeyNotFoundException or UnauthorizedAccessException){stale=true;}
        return new {report,currentRevision=info.Revision,stale};
    }
}
