namespace SMSR.App.Mvp;
public sealed partial class GraphQuestionService
{
    public async Task<GraphFeedbackLessons> LessonsAsync(string projectId,string sourceId,CancellationToken ct=default)
    {
        var info=await store.GetGraphInfoAsync(projectId,ct) ?? throw new KeyNotFoundException("색인이 없습니다.");
        var items=await feedback.GetAsync(projectId,sourceId,ct);
        var rows=items.Where(f=>!f.Stale).GroupBy(f=>new {f.SourceId,f.TargetId,f.Relation,f.OwnerPath,f.SourceLine})
            .Select(g=>{var newest=g.OrderByDescending(f=>f.CreatedAt).ThenBy(f=>f.FeedbackId,StringComparer.Ordinal).First();
                var guidance=newest.Verdict switch {"USEFUL"=>"이 근거를 우선 확인", "ERROR"=>"오류 피드백: 다른 근거와 대조",
                    "DEAD_END"=>"이 경로는 이전 탐색에서 유용하지 않았음",_=>"교정 피드백: 현재 원문에서 재확인"};
                return new GraphFeedbackLesson(newest,g.Count(),guidance);}).ToArray();
        if ((await store.GetGraphInfoAsync(projectId,ct))?.Revision!=info.Revision) throw new InvalidOperationException("피드백 조회 중 색인이 변경되었습니다.");
        return new(info.Revision,rows,items.Count(f=>f.Stale),items.Count==100);
    }
}
