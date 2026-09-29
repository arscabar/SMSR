using System.ComponentModel;
using ModelContextProtocol.Server;

namespace SMSR.App.Mvp;

[McpServerToolType]
public sealed partial class StdioGraphAdvancedTools(McpHttpGateway gateway)
{
    [McpServerTool(Name = "query_graph_cypher"), Description("읽기 전용 Cypher를 로컬 그래프에서 실행합니다.")]
    public Task<string> Cypher(string projectId, string query) => gateway.CallAsync("query_graph_cypher", new { projectId, query });
    [McpServerTool(Name = "search_graph_semantic"), Description("경로·제목·종류를 다국어 로컬 임베딩으로 검색합니다.")]
    public Task<string> Semantic(string projectId, string query) => gateway.CallAsync("search_graph_semantic", new { projectId, query });
    [McpServerTool(Name = "search_graph_body"), Description("본문을 로컬 벡터로 검색하고 근거 줄을 반환합니다. 상대 파일/폴더 scope, 원문·질문 미저장, 비밀 패턴 파일 제외.")]
    public Task<string> Body(string projectId, string query, string scope = "") => gateway.CallAsync("search_graph_body", new { projectId, query, scope });
    [McpServerTool(Name = "analyze_graph_code"), Description("현재 파일의 구문·보수적 흐름과 Python3.12 슬롯/전이/도달 정의/연산·반복·분해·반환·호출 인자·수신 객체 후보를 저장합니다. 반복 종료·미지원 사유·미해석 경계 구분. 대상 코드 미실행, 실제 호출 대상·정밀 taint 확정 아님.")]
    public Task<string> Analyze(string projectId, string path) => gateway.CallAsync("analyze_graph_code", new { projectId, path });
    [McpServerTool(Name = "get_graph_analysis"), Description("저장된 코드 분석과 오래됨을 조회합니다.")]
    public Task<string> Analysis(string projectId, string path) => gateway.CallAsync("get_graph_analysis", new { projectId, path });

    [McpServerTool(Name = "analyze_graph_csharp_bundle"), Description("명시한 C# 묶음의 심벌·CFG·인자/반환 후보 분석. 호스트 프레임워크 기준. 값 전파/taint 확정 아님. 원문 미저장.")]
    public Task<string> CSharp(string projectId, string[] paths, string languageVersion = "CSharp12", string[]? defines = null)
        => gateway.CallAsync("analyze_graph_csharp_bundle", new { projectId, paths, languageVersion, defines });

    [McpServerTool(Name = "get_graph_csharp_bundle"), Description("C# 파일 묶음의 저장 분석과 오래됨을 조회합니다.")]
    public Task<string> CSharpResult(string projectId, string[] paths, string languageVersion = "CSharp12", string[]? defines = null)
        => gateway.CallAsync("get_graph_csharp_bundle", new { projectId, paths, languageVersion, defines });
}
