using System.ComponentModel;
using ModelContextProtocol.Server;
namespace SMSR.App.Mvp;
[McpServerToolType]
public sealed class StdioGraphOverviewTools(McpHttpGateway gateway)
{
    [McpServerTool(Name = "get_graph_overview"), Description("Graphify 구조 그룹·응집도·핵심 노드와 실제 교차 관계를 조회합니다.")]
    public Task<string> Get(string projectId, int offset = 0, int limit = 12)
        => gateway.CallAsync("get_graph_overview", new { projectId, offset, limit });
    [McpServerTool(Name = "get_graph_group_members"), Description("그룹의 실제 노드를 리비전 고정 페이지로 조회합니다.")]
    public Task<string> Members(string projectId, int groupId, int revision, int offset = 0, int limit = 50)
        => gateway.CallAsync("get_graph_group_members", new { projectId, groupId, revision, offset, limit });
}
