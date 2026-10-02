using System.ComponentModel;
using System.IO;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace SMSR.App.Mvp;

[McpServerToolType]
public sealed class GraphTools(GraphIndexService index, GraphQueryService queryService,
    GraphEvidenceService evidence, GraphFeedbackService feedback, GraphValidationService validation, EventStore store)
{
    [McpServerTool(Name = "index_project_graph"), Description("명시한 로컬 Git 저장소의 언어 공통 파일·문서 관계를 색인합니다. 비밀·생성 파일과 원문 코드는 저장하지 않습니다.")]
    public Task<string> IndexProjectGraph(string projectId, string rootPath, bool allowLargeReduction = false)
        => ReplyAsync(() => index.IndexAsync(projectId, rootPath, allowLargeReduction));

    [McpServerTool(Name = "search_project_graph"), Description("색인된 파일·문서 제목을 검색합니다. 동명 노드는 경로로 구별합니다.")]
    public Task<string> SearchProjectGraph(string projectId, string query, int limit = 50)
        => ReplyAsync(() => queryService.SearchAsync(projectId, query, limit));

    [McpServerTool(Name = "get_graph_context"), Description("노드의 들어오고 나가는 관계와 근거 위치·해소 상태를 조회합니다.")]
    public Task<string> GetGraphContext(string projectId, string nodeId, int limit = 30)
        => ReplyAsync(() => queryService.ContextAsync(projectId, nodeId, limit));

    [McpServerTool(Name = "find_graph_path"), Description("방향을 보존한 최단 관계 경로를 제한된 깊이에서 조회합니다.")]
    public Task<string> FindGraphPath(string projectId, string fromId, string toId, int maxDepth = 5, int maxNodes = 1000)
        => ReplyAsync(() => queryService.PathAsync(projectId, fromId, toId, maxDepth, maxNodes));

    [McpServerTool(Name = "get_graph_impact"), Description("노드로 들어오는 관계를 역방향 탐색해 영향받는 파일·문서를 조회합니다.")]
    public Task<string> GetGraphImpact(string projectId, string nodeId, int maxDepth = 3, int maxNodes = 100)
        => ReplyAsync(() => queryService.ImpactAsync(projectId, nodeId, maxDepth, maxNodes));

    [McpServerTool(Name = "get_graph_cycles"), Description("방향 관계의 순환 연결 그룹을 제한된 규모에서 조회합니다.")]
    public Task<string> GetGraphCycles(string projectId)
        => ReplyAsync(() => queryService.CyclesAsync(projectId));

    [McpServerTool(Name = "compare_graph_revisions"), Description("같은 프로젝트의 저장된 두 색인 리비전에서 노드·관계의 추가·삭제·변경을 비교합니다.")]
    public Task<string> CompareGraphRevisions(string projectId, int fromRevision, int toRevision)
        => ReplyAsync(() => queryService.DiffAsync(projectId, fromRevision, toRevision));

    [McpServerTool(Name = "record_graph_feedback"), Description("색인 관계에 USEFUL/ERROR/CORRECTED 판정을 저장합니다. 수정됨은 사용자 판정이며 자동 정확성 보증이 아닙니다. 질문·응답 원문은 저장하지 않습니다.")]
    public Task<string> RecordGraphFeedback(string projectId, string sourceId, string targetId,
        string relation, string ownerPath, int sourceLine, string verdict)
        => ReplyAsync(() => feedback.RecordAsync(new(projectId, sourceId, targetId, relation, ownerPath, sourceLine, verdict)));

    [McpServerTool(Name = "get_graph_feedback"), Description("선택 노드의 구조화된 관계 피드백과 오래됨을 조회합니다.")]
    public Task<string> GetGraphFeedback(string projectId, string sourceId)
        => ReplyAsync(() => feedback.GetAsync(projectId, sourceId));

    [McpServerTool(Name = "get_graph_route_map"), Description("코드에 선언된 API 경로와 MCP 도구 이름의 파일 근거 지도를 조회합니다. 값·토큰·인수는 포함하지 않습니다.")]
    public Task<string> GetGraphRouteMap(string projectId)
        => ReplyAsync(() => queryService.RoutesAsync(projectId));

    [McpServerTool(Name = "get_workflow_validation_gaps"), Description("작업 산출물과 구조화된 검증 기록의 누락을 구분해 조회합니다. 실제 테스트 미실행을 단정하지 않습니다.")]
    public Task<string> GetWorkflowValidationGaps(string projectId, string workflowId)
        => ReplyAsync(() => validation.GetAsync(projectId, workflowId));

    [McpServerTool(Name = "export_graph_scope"), Description("선택 노드에서 깊이 1~3·방향별 최대 200노드/500관계를 근거와 함께 JSON으로 내보냅니다.")]
    public Task<string> ExportGraphScope(string projectId, string nodeId, int depth = 2, string direction = "both")
        => ReplyAsync(() => queryService.ExportScopeAsync(projectId, nodeId, depth, direction));

    [McpServerTool(Name = "get_graph_communities"), Description("대형 그래프의 읽기 전용 커뮤니티 요약과 대표 노드를 제한된 비용으로 조회합니다.")]
    public Task<string> GetGraphCommunities(string projectId)
        => ReplyAsync(() => queryService.CommunitiesAsync(projectId));

    [McpServerTool(Name = "get_cross_repo_graph"), Description("색인된 저장소 사이의 명시적 파일·프로젝트 참조만 조회합니다. 동명 심벌 추정 결합은 하지 않습니다.")]
    public Task<string> GetCrossRepoGraph(string projectId)
        => ReplyAsync(() => queryService.CrossRepoAsync(projectId));

    [McpServerTool(Name = "get_graph_health"), Description("색인 리비전·규모·미해소 관계·고아 관계를 조회합니다.")]
    public Task<string> GetGraphHealth(string projectId)
        => ReplyAsync(() => store.GetGraphHealthAsync(projectId));

    [McpServerTool(Name = "check_graph_freshness"), Description("현재 Git 파일과 저장된 해시를 읽기 전용으로 비교해 추가·변경·삭제를 조회합니다.")]
    public Task<string> CheckGraphFreshness(string projectId)
        => ReplyAsync(() => index.CheckFreshnessAsync(projectId));

    [McpServerTool(Name = "get_workflow_graph_evidence"), Description("작업 산출물 중 색인된 파일과 정확히 일치하는 근거를 연결합니다. 모호한 참조는 미해소로 남깁니다.")]
    public Task<string> GetWorkflowGraphEvidence(string projectId, string workflowId)
        => ReplyAsync(() => evidence.GetAsync(projectId, workflowId));

    internal static async Task<string> ReplyAsync<T>(Func<Task<T>> action)
    {
        try { return JsonSerializer.Serialize(await action()); }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException or KeyNotFoundException or IOException)
        { return JsonSerializer.Serialize(new { error = error.Message }); }
    }
}
