using System.ComponentModel;
using ModelContextProtocol.Server;

namespace SMSR.App.Mvp;

public sealed partial class GraphAdvancedTools
{
    [McpServerTool(Name="query_graph_java_definition"),Description("기존 VS Code Red Hat Java1.56.0을 사용해 선택한 색인 Java 복사본의 정의 후보를 조회·저장합니다. line/character는0부터 UTF16, sourceRoots는 상대 폴더(기본 루트). 원본 설정/빌드/의존성 가져오기 없음. 실제 디스패치·PDG/taint 확정 아님.")]
    public Task<string> Jdt(string projectId,string[] paths,string path,int line,int character,string[]? sourceRoots=null,CancellationToken cancellationToken=default)
        =>Reply(()=>service.QueryJdtAsync(new(projectId,paths,path,line,character,sourceRoots),cancellationToken));
    [McpServerTool(Name="get_graph_java_definition"),Description("동일 Java 정의 조회의 저장 결과·원문/색인 오래됨을 확인합니다. 서버를 실행하지 않습니다.")]
    public Task<string> JdtResult(string projectId,string[] paths,string path,int line,int character,string[]? sourceRoots=null)
        =>Reply(()=>service.JdtResultAsync(new(projectId,paths,path,line,character,sourceRoots)));
}

public sealed partial class StdioGraphAdvancedTools
{
    [McpServerTool(Name="query_graph_java_definition"),Description("설치된 Java LSP로 명시 파일 복사본의 정의 후보를 조회·저장합니다. 좌표는0부터 UTF16. 프로젝트 빌드/원문 저장 없음.")]
    public Task<string> Jdt(string projectId,string[] paths,string path,int line,int character,string[]? sourceRoots=null,CancellationToken cancellationToken=default)
        =>gateway.CallAsync("query_graph_java_definition",new {projectId,paths,path,line,character,sourceRoots},cancellationToken);
    [McpServerTool(Name="get_graph_java_definition"),Description("동일 입력의 저장된 정의 후보와 오래됨을 확인합니다.")]
    public Task<string> JdtResult(string projectId,string[] paths,string path,int line,int character,string[]? sourceRoots=null)
        =>gateway.CallAsync("get_graph_java_definition",new {projectId,paths,path,line,character,sourceRoots});
}
