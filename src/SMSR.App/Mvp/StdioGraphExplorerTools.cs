using System.ComponentModel;
using ModelContextProtocol.Server;

namespace SMSR.App.Mvp;

[McpServerToolType]
public sealed class StdioGraphExplorerTools(McpHttpGateway gateway)
{
    [McpServerTool(Name = "get_graph_relations"), Description("관계 방향·종류별 페이지를 조회합니다.")]
    public Task<string> Relations(string projectId, string nodeId, string direction = "both",
        string? relation = null, int offset = 0, int limit = 20, int? expectedRevision = null)
        => gateway.CallAsync("get_graph_relations", new { projectId, nodeId, direction, relation, offset, limit, expectedRevision });
    [McpServerTool(Name = "trace_graph_path"), Description("단계별 근거가 있는 정적 연결 경로를 조회합니다.")]
    public Task<string> Trace(string projectId, string fromId, string toId, string? relation = null,
        bool includeInferred = false, int depth = 5)
        => gateway.CallAsync("trace_graph_path", new { projectId, fromId, toId, relation, includeInferred, depth });
    [McpServerTool(Name = "get_graph_affected"), Description("직접·간접 영향 후보와 근거를 조회합니다.")]
    public Task<string> Affected(string projectId, string nodeId, string? relation = null,
        bool includeInferred = false, int depth = 3)
        => gateway.CallAsync("get_graph_affected", new { projectId, nodeId, relation, includeInferred, depth });
    [McpServerTool(Name = "explain_graph_node"), Description("노드 근거·원문 변경 상태·진단을 조회합니다.")]
    public Task<string> Explain(string projectId, string nodeId)
        => gateway.CallAsync("explain_graph_node", new { projectId, nodeId });
    [McpServerTool(Name = "get_graph_issues"), Description("파일별/프로젝트 진단 페이지를 조회합니다.")]
    public Task<string> Issues(string projectId, string? ownerPath = null, int offset = 0)
        => gateway.CallAsync("get_graph_issues", new { projectId, ownerPath, offset });
}
