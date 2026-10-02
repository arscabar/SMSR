using System.ComponentModel;
using ModelContextProtocol.Server;
namespace SMSR.App.Mvp;
[McpServerToolType]
public sealed class GraphOverviewTools(GraphOverviewService service,Microsoft.Extensions.Hosting.IHostApplicationLifetime lifetime)
{
    [McpServerTool(Name = "get_graph_overview"), Description("Graphify 그룹 분석 상태를 즉시 반환합니다. ANALYSIS_PENDING이면 retryAfterSeconds 후 재조회, READY의 page에 결과가 있습니다. 최대 8분·1 GiB 워커, host 종료 취소. 의미·실행 요약 아님.")]
    public Task<string> Get(string projectId, int offset = 0, int limit = 12)
        => GraphTools.ReplyAsync(() => service.StateAsync(projectId, offset, limit,lifetime.ApplicationStopping));
    [McpServerTool(Name = "get_graph_group_members"), Description("그룹 안의 실제 노드를 리비전 고정 페이지로 조회합니다.")]
    public Task<string> Members(string projectId, int groupId, int revision, int offset = 0, int limit = 50)
        => GraphTools.ReplyAsync(() => service.MembersAsync(projectId, groupId, revision, offset, limit));
}
