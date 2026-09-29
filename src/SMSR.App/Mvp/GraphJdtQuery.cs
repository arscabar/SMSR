using System.Text;
using System.Text.Json;

namespace SMSR.App.Mvp;

public sealed partial class GraphAdvancedService
{
    private static readonly SemaphoreSlim JdtGate=new(1,1);
    internal async Task<JsonElement> QueryJdtAsync(GraphJdtRequest request,CancellationToken ct=default)
    {
        request=request.Normalize();var info=await RequireAsync(request.ProjectId,ct);
        if(!await JdtGate.WaitAsync(0,ct)) throw new InvalidOperationException("Java 정의 조회가 실행 중입니다.");
        try
        {
            var installation=GraphJdtInstallation.Resolve();
            var sources=new Dictionary<string,string>();var hashes=new Dictionary<string,string>();long size=0;
            var reader=new GraphSourceService(store);
            foreach(var path in request.Paths)
            {
                var source=await reader.ReadAsync(request.ProjectId,path,ct);
                if(GraphBodyChunks.Excluded(source.Text)) throw new InvalidOperationException("민감 패턴이 있는 Java 파일은 복사하지 않습니다.");
                if(source.Revision!=info.Revision) throw new InvalidOperationException("색인이 변경됐습니다.");
                size+=Encoding.UTF8.GetByteCount(source.Text);
                if(size>16*1024*1024) throw new ArgumentException("Java 묶음은16 MiB 이하여야 합니다.");
                sources.Add(path,source.Text);hashes.Add(path,source.Hash);
            }
            GraphLspLocations.ValidatePosition(sources[request.Path],new(request.Line,request.Character));
            var result=await GraphJdtExecution.RunAsync(installation,request,sources,ct);
            foreach(var path in request.Paths)
                if((await reader.ReadAsync(request.ProjectId,path,ct)).Hash!=hashes[path])
                    throw new InvalidOperationException("분석 도중 원문이 변경됐습니다.");
            return await SaveJdtAsync(request,info.Revision,hashes,result.Candidates,result.Excluded,ct);
        }
        finally { JdtGate.Release(); }
    }
}
