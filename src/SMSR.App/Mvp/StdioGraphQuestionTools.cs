using System.ComponentModel;
using ModelContextProtocol.Server;
namespace SMSR.App.Mvp;
[McpServerToolType]
public sealed class StdioGraphQuestionTools(McpHttpGateway gateway)
{
    [McpServerTool(Name="get_graph_vocabulary"),Description("현재 그래프의 실제 어휘·빈도 페이지입니다.")]
    public Task<string> Vocabulary(string projectId,int offset=0,int limit=100)=>gateway.CallAsync("get_graph_vocabulary",new{projectId,offset,limit});
    [McpServerTool(Name="search_graph_question"),Description("질문 관련 후보·동명 여부를 반환합니다. 질문 미저장.")]
    public Task<string> Search(GraphQuestionRequest request)=>gateway.CallAsync("search_graph_question",new{request});
    [McpServerTool(Name="get_graph_question_evidence"),Description("정적 근거와 피드백을 분량 예산에 맞춰 모읍니다. 질문 미저장·외부 전송 없음.")]
    public Task<string> Evidence(GraphQuestionRequest request)=>gateway.CallAsync("get_graph_question_evidence",new{request});
    [McpServerTool(Name="get_graph_feedback_lessons"),Description("현재 근거의 안전한 피드백 요약입니다. 오래된 피드백 제외.")]
    public Task<string> Lessons(string projectId,string sourceId)=>gateway.CallAsync("get_graph_feedback_lessons",new{projectId,sourceId});
}
