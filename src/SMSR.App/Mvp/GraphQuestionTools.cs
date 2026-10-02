using System.ComponentModel;
using ModelContextProtocol.Server;
namespace SMSR.App.Mvp;
[McpServerToolType]
public sealed class GraphQuestionTools(GraphQuestionService service)
{
    [McpServerTool(Name="get_graph_vocabulary"),Description("현재 그래프의 실제 어휘와 빈도를 페이지 조회합니다. 라벨·경로 기반입니다.")]
    public Task<string> Vocabulary(string projectId,int offset=0,int limit=100)=>GraphTools.ReplyAsync(()=>service.VocabularyAsync(projectId,offset,limit));
    [McpServerTool(Name="search_graph_question"),Description("질문 표현을 실제 어휘로 한정하고 관련 후보·동명 여부를 반환합니다. 원문 질문은 저장하지 않습니다.")]
    public Task<string> Search(GraphQuestionRequest request)=>GraphTools.ReplyAsync(()=>service.SearchAsync(request));
    [McpServerTool(Name="get_graph_question_evidence"),Description("방향·깊이·관계 종류·보수적 분량 예산에 맞춰 원문 지문·분석 출처·피드백 근거를 모읍니다. 질문 미저장, 외부 모델 미호출.")]
    public Task<string> Evidence(GraphQuestionRequest request)=>GraphTools.ReplyAsync(()=>service.EvidenceAsync(request));
    [McpServerTool(Name="get_graph_feedback_lessons"),Description("현재 근거의 유용·오류·교정·막힌 경로 피드백을 안전하게 요약합니다. 오래된 피드백 제외, 사실 변경 없음.")]
    public Task<string> Lessons(string projectId,string sourceId)=>GraphTools.ReplyAsync(()=>service.LessonsAsync(projectId,sourceId));
}
