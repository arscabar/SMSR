using System.ComponentModel;
using ModelContextProtocol.Server;

namespace SMSR.App.Mvp;

[McpServerToolType]
public sealed class GraphExplorerTools(GraphQueryService query, EventStore store)
{
    [McpServerTool(Name = "get_graph_relations"), Description("관계를 방향·종류별로 페이지 조회합니다. expectedRevision으로 조회 시점을 고정합니다.")]
    public Task<string> Relations(string projectId, string nodeId, string direction = "both",
        string? relation = null, int offset = 0, int limit = 20, int? expectedRevision = null)
        => GraphTools.ReplyAsync(() => query.RelationsAsync(projectId, nodeId, direction, relation, offset, limit, expectedRevision));

    [McpServerTool(Name = "trace_graph_path"), Description("두 파일·심벌의 정적 경로와 단계별 원문 근거를 조회합니다. 실제 실행 순서가 아닙니다.")]
    public Task<string> Trace(string projectId, string fromId, string toId, string? relation = null,
        bool includeInferred = false, int depth = 5)
        => GraphTools.ReplyAsync(() => query.TraceAsync(projectId, fromId, toId, relation, includeInferred, depth));

    [McpServerTool(Name = "get_graph_affected"), Description("역방향 직접·간접 영향 후보와 단계별 관계 근거를 조회합니다.")]
    public Task<string> Affected(string projectId, string nodeId, string? relation = null,
        bool includeInferred = false, int depth = 3)
        => GraphTools.ReplyAsync(() => query.ImpactAsync(projectId, nodeId, depth, 100,
            relation: relation, includeInferred: includeInferred));

    [McpServerTool(Name = "explain_graph_node"), Description("선택 노드의 정적 근거·연결 건수·원문 변경 상태·진단을 조회합니다.")]
    public Task<string> Explain(string projectId, string nodeId)
        => GraphTools.ReplyAsync(() => query.ExplainAsync(projectId, nodeId));

    [McpServerTool(Name = "get_graph_issues"), Description("파싱 복구·미해소·모호한 관계 진단을 파일별 또는 프로젝트 전체에서 페이지 조회합니다.")]
    public Task<string> Issues(string projectId, string? ownerPath = null, int offset = 0)
        => GraphTools.ReplyAsync(() => store.GraphIssuesAsync(projectId, ownerPath, offset));
}
