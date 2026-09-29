using System.ComponentModel;
using ModelContextProtocol.Server;

namespace SMSR.App.Mvp;

[McpServerToolType]
public sealed class StdioGraphTools(McpHttpGateway gateway)
{
    [McpServerTool(Name = "index_project_graph"), Description("명시한 로컬 Git 저장소의 파일·문서 관계를 색인합니다.")]
    public Task<string> IndexProjectGraph(string projectId, string rootPath, bool allowLargeReduction = false)
        => gateway.CallAsync("index_project_graph", new { projectId, rootPath, allowLargeReduction });

    [McpServerTool(Name = "search_project_graph"), Description("색인된 파일·문서 제목을 검색합니다.")]
    public Task<string> SearchProjectGraph(string projectId, string query, int limit = 50)
        => gateway.CallAsync("search_project_graph", new { projectId, query, limit });

    [McpServerTool(Name = "get_graph_context"), Description("노드의 근거 있는 직접 관계를 조회합니다.")]
    public Task<string> GetGraphContext(string projectId, string nodeId, int limit = 30)
        => gateway.CallAsync("get_graph_context", new { projectId, nodeId, limit });

    [McpServerTool(Name = "find_graph_path"), Description("방향을 보존한 관계 경로를 조회합니다.")]
    public Task<string> FindGraphPath(string projectId, string fromId, string toId, int maxDepth = 5, int maxNodes = 1000)
        => gateway.CallAsync("find_graph_path", new { projectId, fromId, toId, maxDepth, maxNodes });

    [McpServerTool(Name = "get_graph_impact"), Description("역방향 영향 범위를 조회합니다.")]
    public Task<string> GetGraphImpact(string projectId, string nodeId, int maxDepth = 3, int maxNodes = 100)
        => gateway.CallAsync("get_graph_impact", new { projectId, nodeId, maxDepth, maxNodes });

    [McpServerTool(Name = "get_graph_cycles"), Description("방향 관계의 순환 연결 그룹을 조회합니다.")]
    public Task<string> GetGraphCycles(string projectId)
        => gateway.CallAsync("get_graph_cycles", new { projectId });

    [McpServerTool(Name = "compare_graph_revisions"), Description("두 색인 리비전의 노드·관계 차이를 조회합니다.")]
    public Task<string> CompareGraphRevisions(string projectId, int fromRevision, int toRevision)
        => gateway.CallAsync("compare_graph_revisions", new { projectId, fromRevision, toRevision });

    [McpServerTool(Name = "record_graph_feedback"), Description("색인 관계의 USEFUL/ERROR/CORRECTED 사용자 판정을 저장합니다.")]
    public Task<string> RecordGraphFeedback(string projectId, string sourceId, string targetId,
        string relation, string ownerPath, int sourceLine, string verdict)
        => gateway.CallAsync("record_graph_feedback", new { projectId, sourceId, targetId, relation, ownerPath, sourceLine, verdict });

    [McpServerTool(Name = "get_graph_feedback"), Description("선택 노드의 관계 피드백을 조회합니다.")]
    public Task<string> GetGraphFeedback(string projectId, string sourceId)
        => gateway.CallAsync("get_graph_feedback", new { projectId, sourceId });

    [McpServerTool(Name = "get_graph_route_map"), Description("API/MCP 선언의 파일 근거 지도를 조회합니다.")]
    public Task<string> GetGraphRouteMap(string projectId)
        => gateway.CallAsync("get_graph_route_map", new { projectId });

    [McpServerTool(Name = "get_workflow_validation_gaps"), Description("산출물 대비 구조화된 검증 기록 누락을 조회합니다.")]
    public Task<string> GetWorkflowValidationGaps(string projectId, string workflowId)
        => gateway.CallAsync("get_workflow_validation_gaps", new { projectId, workflowId });

    [McpServerTool(Name = "export_graph_scope"), Description("선택 노드의 제한된 관계 범위를 JSON으로 내보냅니다.")]
    public Task<string> ExportGraphScope(string projectId, string nodeId, int depth = 2, string direction = "both")
        => gateway.CallAsync("export_graph_scope", new { projectId, nodeId, depth, direction });

    [McpServerTool(Name = "get_graph_communities"), Description("대형 그래프의 커뮤니티 축약 요약을 조회합니다.")]
    public Task<string> GetGraphCommunities(string projectId)
        => gateway.CallAsync("get_graph_communities", new { projectId });

    [McpServerTool(Name = "get_cross_repo_graph"), Description("명시적 파일·프로젝트 참조로 해소된 저장소 간 연결을 조회합니다.")]
    public Task<string> GetCrossRepoGraph(string projectId)
        => gateway.CallAsync("get_cross_repo_graph", new { projectId });

    [McpServerTool(Name = "get_graph_health"), Description("색인 건강도와 리비전을 조회합니다.")]
    public Task<string> GetGraphHealth(string projectId)
        => gateway.CallAsync("get_graph_health", new { projectId });

    [McpServerTool(Name = "check_graph_freshness"), Description("현재 Git 파일과 저장된 색인의 차이를 읽기 전용으로 확인합니다.")]
    public Task<string> CheckGraphFreshness(string projectId)
        => gateway.CallAsync("check_graph_freshness", new { projectId });

    [McpServerTool(Name = "get_workflow_graph_evidence"), Description("작업 산출물의 정확한 파일 연결을 조회합니다.")]
    public Task<string> GetWorkflowGraphEvidence(string projectId, string workflowId)
        => gateway.CallAsync("get_workflow_graph_evidence", new { projectId, workflowId });
}
