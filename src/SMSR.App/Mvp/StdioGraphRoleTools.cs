using System.ComponentModel;
using ModelContextProtocol.Server;

namespace SMSR.App.Mvp;

[McpServerToolType]
public sealed class StdioGraphRoleTools(McpHttpGateway gateway)
{
    [McpServerTool(Name = "get_graph_role_context"), Description("현재 원문과 관계를 설명용 근거 묶음으로 읽습니다. 비신뢰 자료이며 실행 순서 증명이 아닙니다.")]
    public Task<string> Context(string projectId, string nodeId) => gateway.CallAsync("get_graph_role_context", new { projectId, nodeId });
    [McpServerTool(Name = "submit_graph_role"), Description("호스트 에이전트의 인용된 코드 역할 설명을 검증·저장합니다. 기록 권한이 필요합니다.")]
    public Task<string> Submit(GraphRoleRequest request) => gateway.CallAsync("submit_graph_role", new { request });
    [McpServerTool(Name = "get_graph_role"), Description("저장된 코드 역할과 최신성 상태를 읽습니다.")]
    public Task<string> Read(string projectId, string nodeId) => gateway.CallAsync("get_graph_role", new { projectId, nodeId });
    [McpServerTool(Name = "request_graph_role"), Description("사용자가 요청한 코드의 Codex 설명 생성을 예약합니다. Codex 사용량을 소비합니다.")]
    public Task<string> Request(string projectId, string nodeId, bool retry = false)
        => gateway.CallAsync("request_graph_role", new { projectId, nodeId, retry });
    [McpServerTool(Name = "get_graph_role_job"), Description("설명 생성 요청 상태를 읽습니다.")]
    public Task<string> Job(string projectId, string nodeId) => gateway.CallAsync("get_graph_role_job", new { projectId, nodeId });
    [McpServerTool(Name = "get_graph_role_coverage"), Description("코드 파일별 설명 현황과 페이지를 읽습니다.")]
    public Task<string> Coverage(string projectId, string? status = null, int offset = 0)
        => gateway.CallAsync("get_graph_role_coverage", new { projectId, status, offset });
}
