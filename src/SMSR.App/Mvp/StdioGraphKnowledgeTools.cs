using System.ComponentModel;
using ModelContextProtocol.Server;

namespace SMSR.App.Mvp;

[McpServerToolType]
public sealed class StdioGraphKnowledgeTools(McpHttpGateway gateway)
{
    [McpServerTool(Name = "get_graph_hyperedges"), Description("다중 참여 관계·역할·원문 근거를 리비전별로 조회합니다.")]
    public Task<string> Hyperedges(string projectId, string? nodeId = null, int offset = 0,
        int limit = 20, int? expectedRevision = null)
        => gateway.CallAsync("get_graph_hyperedges", new { projectId, nodeId, offset, limit, expectedRevision });

    [McpServerTool(Name = "get_graph_coverage"), Description("색인 파일·심벌·관계의 근거 보존 범위와 구형 메타데이터 상태를 조회합니다.")]
    public Task<string> Coverage(string projectId, int offset = 0, int limit = 50)
        => gateway.CallAsync("get_graph_coverage", new { projectId, offset, limit });
}
