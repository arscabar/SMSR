using System.ComponentModel;
using ModelContextProtocol.Server;

namespace SMSR.App.Mvp;

[McpServerToolType]
public sealed class GraphKnowledgeTools(GraphQueryService query)
{
    [McpServerTool(Name = "get_graph_hyperedges"), Description("3개 이상 노드의 다중 참여 관계와 역할·원문 근거를 리비전별로 조회합니다. pairwise 호출 관계가 아닙니다.")]
    public Task<string> Hyperedges(string projectId, string? nodeId = null, int offset = 0,
        int limit = 20, int? expectedRevision = null)
        => GraphTools.ReplyAsync(() => query.HyperedgesAsync(projectId, nodeId, offset, limit, expectedRevision));

    [McpServerTool(Name = "get_graph_coverage"), Description("파일·심벌 세부 정보·원문 근거의 보존 범위를 조회합니다. 실제 분석 정확도나 전체 관계 완전성을 보장하지 않습니다. UNKNOWN은 추정 복원하지 않습니다.")]
    public Task<string> Coverage(string projectId, int offset = 0, int limit = 50)
        => GraphTools.ReplyAsync(() => query.CoverageAsync(projectId, offset, limit));
}
