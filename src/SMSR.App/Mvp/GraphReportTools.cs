using System.ComponentModel;
using ModelContextProtocol.Server;
namespace SMSR.App.Mvp;
[McpServerToolType]
public sealed class GraphReportTools(GraphReportService service,Microsoft.Extensions.Hosting.IHostApplicationLifetime lifetime)
{
    [McpServerTool(Name="get_graph_report"),Description("동일 리비전의 구조·핵심 구성요소·교차 관계·설계 이유·커버리지와 한계를 반환합니다. ANALYSIS_PENDING은 재조회, 사실과 추론을 구분하세요.")]
    public Task<string> Get(string projectId)=>GraphTools.ReplyAsync(()=>service.GetAsync(projectId,lifetime.ApplicationStopping));
}
[McpServerToolType]
public sealed class StdioGraphReportTools(McpHttpGateway gateway)
{
    [McpServerTool(Name="get_graph_report"),Description("저장된 그래프 근거 기반 보고서 자료를 조회합니다.")]
    public Task<string> Get(string projectId)=>gateway.CallAsync("get_graph_report",new{projectId});
}
