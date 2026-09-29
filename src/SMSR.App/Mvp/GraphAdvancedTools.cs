using System.ComponentModel;
using System.IO;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace SMSR.App.Mvp;

[McpServerToolType]
public sealed partial class GraphAdvancedTools(GraphAdvancedService service)
{
    [McpServerTool(Name = "query_graph_cypher"), Description("로컬 Kuzu 읽기 전용 Cypher를 실행합니다. Node/REL 스키마, 안전 함수, 200행 제한. 원문은 저장하지 않습니다.")]
    public Task<string> Cypher(string projectId, string query) => Reply(() => service.CypherAsync(projectId, query));

    [McpServerTool(Name = "search_graph_semantic"), Description("경로·제목·종류의 로컬 다국어 임베딩을 검색합니다. 본문 전체 검색이 아니며 질문을 저장하지 않습니다.")]
    public Task<string> Semantic(string projectId, string query) => Reply(() => service.SemanticAsync(projectId, query));

    [McpServerTool(Name = "search_graph_body"), Description("색인된 파일의 본문을 로컬 벡터로 검색합니다. scope는 상대 파일/폴더이며 빈 값은 전체입니다. 본문·질문은 저장하지 않고 벡터·해시만 저장합니다. 비밀 패턴 파일 제외, 최대 1만 조각, 근거 줄을 반환합니다.")]
    public Task<string> Body(string projectId, string query, string scope = "")
        => Reply(() => service.BodySemanticAsync(projectId, query, scope));

    [McpServerTool(Name = "analyze_graph_code"), Description("현재 파일의 구문·미해소 호출·MAY_DEPEND·휴리스틱 taint를 저장합니다. Python3.12 슬롯·전이·도달 정의·연산/반복/분해·반환/호출 인자·수신 객체 후보를 제공합니다. 반복 종료 전이와 미지원/미해석 경계를 표시하며 실제 호출 대상·정밀 taint·안전성 보장이 아닙니다. 대상 코드 미실행.")]
    public Task<string> Analyze(string projectId, string path) => Reply(() => service.AnalyzeAsync(projectId, path));

    [McpServerTool(Name = "get_graph_analysis"), Description("저장한 코드 분석과 색인 해시 기준 오래됨을 조회합니다.")]
    public Task<string> Analysis(string projectId, string path) => Reply(() => service.AnalysisAsync(projectId, path));

    [McpServerTool(Name = "analyze_graph_csharp_bundle"), Description("명시한 색인 C# 묶음의 심벌·CFG·인자/반환 후보를 분석·저장합니다. 호스트 .NET 기준이며 csproj·NuGet·생성 코드는 미반영. 가상/동적 호출과 컴파일 오류를 확정 연결하지 않습니다. 값 전파/taint 확정 아님. 원문 미저장.")]
    public Task<string> CSharp(string projectId, string[] paths, string languageVersion = "CSharp12", string[]? defines = null)
        => Reply(() => service.AnalyzeCSharpAsync(new(projectId, paths, languageVersion, defines)));

    [McpServerTool(Name = "get_graph_csharp_bundle"), Description("동일 C# 파일·언어 버전·조건부 심벌 묶음의 저장 결과와 오래됨을 조회합니다.")]
    public Task<string> CSharpResult(string projectId, string[] paths, string languageVersion = "CSharp12", string[]? defines = null)
        => Reply(() => service.CSharpAnalysisAsync(new(projectId, paths, languageVersion, defines)));

    private static async Task<string> Reply<T>(Func<Task<T>> action)
    {
        try { return JsonSerializer.Serialize(await action(), GraphWorker.Json); }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException or KeyNotFoundException or IOException)
        { return JsonSerializer.Serialize(new { error = error.Message }); }
    }
}
