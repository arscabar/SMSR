namespace SMSR.App.Mvp;
public sealed partial class GraphQuestionService(EventStore store, GraphFeedbackService feedback)
{
    internal async Task<(GraphInfo Info, IReadOnlyList<GraphNode> Nodes)> Snapshot(string projectId, CancellationToken ct)
    {
        if (EventValidation.ValidateWorkflowIds(projectId,"graph") is { } error) throw new ArgumentException(error);
        var info = await store.GetGraphInfoAsync(projectId,ct) ?? throw new KeyNotFoundException("색인이 없습니다.");
        if (info.NodeCount > 50000) throw new InvalidOperationException("질문 탐색은 5만 노드 이하에서 지원합니다.");
        // ponytail: bounded O(nodes) vocabulary, revision cache only if measured query load needs it.
        return (info,await store.GetGraphRevisionNodesAsync(projectId,info.Revision,ct));
    }
    public async Task<GraphVocabulary> VocabularyAsync(string projectId,int offset=0,int limit=100,CancellationToken ct=default)
    {
        GraphOverviewService.Bounds(offset,limit);
        var (info,nodes)=await Snapshot(projectId,ct); var words=GraphQuestionWords.Vocabulary(nodes);
        return new(info.Revision,words.OrderByDescending(p=>p.Value).ThenBy(p=>p.Key,StringComparer.Ordinal)
            .Skip(offset).Take(limit).Select(p=>new GraphVocabularyTerm(p.Key,p.Value)).ToArray(),words.Count,offset+limit<words.Count);
    }
    public async Task<GraphQuestionSearch> SearchAsync(GraphQuestionRequest request,CancellationToken ct=default)
    {
        Validate(request); var (info,nodes)=await Snapshot(request.ProjectId,ct);
        var words=GraphQuestionWords.Vocabulary(nodes);
        var terms=request.Terms ?? GraphQuestionWords.Expand(request.Question,words);
        if (terms.Length>12 || terms.Any(t=>t is null || !words.ContainsKey(t))) throw new ArgumentException("현재 그래프 어휘에서 최대 12개를 선택하세요.");
        terms=terms.Distinct().ToArray(); var labels=nodes.GroupBy(n=>n.Label).ToDictionary(g=>g.Key,g=>g.Count());
        var hits=nodes.Where(n=>n.Kind!="heading").Select(n=>{
            var own=GraphQuestionWords.Split(n.Label+" "+n.SourcePath).ToHashSet();var matched=terms.Where(own.Contains).ToArray();
            return new GraphQuestionHit(n,matched.Sum(t=>Math.Log(1+(double)nodes.Count/words[t])),matched,labels[n.Label]>1);
        }).Where(h=>h.Score>0).OrderByDescending(h=>h.Score).ThenBy(h=>h.Node.NodeId,StringComparer.Ordinal).Take(21).ToArray();
        return new(info.Revision,terms,hits.Take(20).ToArray(),hits.Length>20,"현재 라벨·경로 어휘 기반 후보 순위이며 정답·의미 일치 보장이 아닙니다.");
    }
    internal static void Validate(GraphQuestionRequest r)
    {
        if (r.Question is null || r.Question.Length>512 || GraphDocumentRedaction.Excluded(r.Question)
            || r.MaxTokens is < 1024 or > 16000 || r.Direction is not ("both" or "incoming" or "outgoing") || r.Depth is <1 or >3
            || (r.Relation is not null && !System.Text.RegularExpressions.Regex.IsMatch(r.Relation,@"^[A-Z][A-Z0-9_]{0,63}$")))
            throw new ArgumentException("질문·분량·방향·깊이가 올바르지 않습니다.");
    }
}
