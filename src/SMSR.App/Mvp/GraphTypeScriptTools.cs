using System.ComponentModel;
using ModelContextProtocol.Server;

namespace SMSR.App.Mvp;

public sealed partial class GraphAdvancedTools
{
    [McpServerTool(Name="analyze_graph_typescript_bundle"),Description("명시한 색인 TypeScript 파일 묶음을 설치된 TypeScript 6.0 컴파일러로 분석·저장합니다. 빌드/플러그인/대상 코드는 실행하지 않습니다. 의존성 자동 해소·CFG/PDG/taint는 미지원. 원문 미저장.")]
    public Task<string> TypeScript(string projectId,string[] paths)=>Reply(()=>service.AnalyzeTypeScriptAsync(new(projectId,paths)));
    [McpServerTool(Name="get_graph_typescript_bundle"),Description("동일 TypeScript 파일 묶음의 저장 분석과 현재 원문 기준 오래됨을 조회합니다.")]
    public Task<string> TypeScriptResult(string projectId,string[] paths)=>Reply(()=>service.TypeScriptAnalysisAsync(new(projectId,paths)));
}

public sealed partial class StdioGraphAdvancedTools
{
    [McpServerTool(Name="analyze_graph_typescript_bundle"),Description("TypeScript 6.0 입력 파일 묶음의 컴파일러 심벌/정적 호출 시그니처을 분석·저장합니다. 실제 프로젝트 빌드·PDG/taint 아님. 원문 미저장.")]
    public Task<string> TypeScript(string projectId,string[] paths)=>gateway.CallAsync("analyze_graph_typescript_bundle",new {projectId,paths});
    [McpServerTool(Name="get_graph_typescript_bundle"),Description("TypeScript 파일 묶음의 저장 결과와 오래됨을 조회합니다.")]
    public Task<string> TypeScriptResult(string projectId,string[] paths)=>gateway.CallAsync("get_graph_typescript_bundle",new {projectId,paths});
}
