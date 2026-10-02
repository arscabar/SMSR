using System.ComponentModel;
using ModelContextProtocol.Server;
namespace SMSR.App.Mvp;
[McpServerToolType]
public sealed class GraphMediaAnalysisTools(GraphMediaAnalysisService service,Microsoft.Extensions.Hosting.IHostApplicationLifetime lifetime)
{
    [McpServerTool(Name="read_graph_media"),Description("색인 지문을 검증한 로컬 이미지·OCR 영역·음성 시간 구간·영상 표본 프레임을 읽습니다. 설치된 도구/모델만 사용, 외부 전송·다운로드 없음. 누락 OCR/전사는 UNAVAILABLE로 표시.")]
    public Task<string> Read(string projectId,string path,bool retry=false)=>GraphTools.ReplyAsync(()=>GraphMediaPolling.StateAsync(service,projectId,path,lifetime.ApplicationStopping,retry));
}
[McpServerToolType]
public sealed class StdioGraphMediaAnalysisTools(McpHttpGateway gateway)
{
    [McpServerTool(Name="read_graph_media"),Description("로컬 미디어 영역·시간·프레임 근거 읽기. 도구 없으면 명시적 unavailable.")]
    public Task<string> Read(string projectId,string path,bool retry=false)=>gateway.CallAsync("read_graph_media",new{projectId,path,retry});
}
