namespace SMSR.App.Mvp;
public sealed partial class GraphQuestionService
{
    public async Task<GraphQuestionEvidence> EvidenceAsync(GraphQuestionRequest request,CancellationToken ct=default)
    {
        var search=await SearchAsync(request,ct);var selected=new Dictionary<string,GraphNode>();var edges=new HashSet<GraphEdge>();
        var lessons=new List<GraphFeedbackLesson>();var queue=new Queue<(string Id,int Depth)>();var truncated=search.Truncated;
        var lessonCache=new Dictionary<string,GraphFeedbackLessons>();
        async Task<GraphFeedbackLessons> Learn(string id)
        {
            if(lessonCache.TryGetValue(id,out var existing))return existing;
            var value=await LessonsAsync(request.ProjectId,id,ct);
            if(value.Revision!=search.Revision)throw new InvalidOperationException("피드백 리비전이 변경되었습니다.");
            lessonCache[id]=value;lessons.AddRange(value.Items);truncated|=value.Truncated;return value;
        }
        foreach(var hit in search.Hits.Take(3)){selected[hit.Node.NodeId]=hit.Node;queue.Enqueue((hit.Node.NodeId,0));}
        var visited=new HashSet<string>();
        while(queue.TryDequeue(out var entry))
        {
            ct.ThrowIfCancellationRequested();if(entry.Depth>=request.Depth||!visited.Add(entry.Id))continue;
            var context=await new GraphQueryService(store).ContextAsync(request.ProjectId,entry.Id,30,ct);
            var learned=await Learn(entry.Id);
            if(context.Revision!=search.Revision)throw new InvalidOperationException("리비전 변경·다시 조회하세요.");
            truncated|=context.Truncated;
            var neighbors=request.Direction=="incoming"?context.Incoming:request.Direction=="outgoing"?context.Outgoing:context.Incoming.Concat(context.Outgoing);
            foreach(var neighbor in neighbors.OrderBy(n=>learned.Items.Any(l=>l.Feedback.TargetId==n.Edge.TargetId&&l.Feedback.Verdict=="USEFUL")?0:1))
            {
                var e=neighbor.Edge;
                if(request.Relation is not null && e.Relation!=request.Relation)continue;
                if(!request.IncludeInferred&&(e.Confidence=="INFERRED"||e.Resolution!="RESOLVED"))continue;
                if(selected.Count>=30||edges.Count>=50){truncated=true;break;}
                var sourceLessons=await Learn(e.SourceId);
                var lesson=sourceLessons.Items.FirstOrDefault(l=>l.Feedback.TargetId==e.TargetId&&l.Feedback.Relation==e.Relation&&l.Feedback.OwnerPath==e.OwnerPath&&l.Feedback.SourceLine==e.SourceLine);
                if(lesson?.Feedback.Verdict=="DEAD_END")continue;
                selected[neighbor.Node.NodeId]=neighbor.Node;edges.Add(e);queue.Enqueue((neighbor.Node.NodeId,entry.Depth+1));
            }
        }
        if((await store.GetGraphInfoAsync(request.ProjectId,ct))?.Revision!=search.Revision)throw new InvalidOperationException("근거 리비전이 변경되었습니다. 다시 조회하세요.");
        return GraphQuestionBudget.Pack(search.Revision,selected.Values.ToArray(),edges.ToArray(),lessons.Distinct().ToArray(),truncated,request.MaxTokens);
    }
}
