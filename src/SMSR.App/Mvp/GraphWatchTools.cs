using System.ComponentModel;
using ModelContextProtocol.Server;
namespace SMSR.App.Mvp;
[McpServerToolType]
public sealed class GraphWatchTools(GraphWatchService service)
{
    [McpServerTool(Name="get_graph_watch"),Description("프로젝트 변경 감시 상태를 조회합니다. 기본 꺼짐, 기존 색인 범위만 적용.")]
    public GraphWatchState Get(string projectId)=>service.Get(projectId);
    [McpServerTool(Name="set_graph_watch"),Description("사용자가 요청한 색인 프로젝트의 파일 변경 감시를 켜거나 끕니다. 로컬 증분 색인만 수행, 외부 모델·훅 설치 없음.")]
    public Task<string> Set(string projectId,bool enabled)=>GraphTools.ReplyAsync(()=>service.SetAsync(projectId,enabled));
}
[McpServerToolType]
public sealed class StdioGraphWatchTools(McpHttpGateway gateway)
{
    [McpServerTool(Name="get_graph_watch"),Description("로컬 파일 변경 감시 상태 조회.")]
    public Task<string> Get(string projectId)=>gateway.CallAsync("get_graph_watch",new{projectId});
    [McpServerTool(Name="set_graph_watch"),Description("요청된 프로젝트의 로컬 증분 색인 감시 전환.")]
    public Task<string> Set(string projectId,bool enabled)=>gateway.CallAsync("set_graph_watch",new{projectId,enabled});
}
