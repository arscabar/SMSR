namespace SMSR.App.Mvp;
internal static class GraphQuestionFeedbackSelfCheck
{
    internal static async Task RunAsync(EventStore store,GraphQuestionService service,GraphFeedbackService feedback,
        GraphQuestionRequest request,GraphEdge[] edges,Func<string,GraphScan> scan)
    {
        GraphFeedbackRequest Input(string verdict)=>new("question","a","b","CALLS","entry.py",1,verdict);
        await feedback.RecordAsync(Input("USEFUL"));await feedback.RecordAsync(Input("ERROR"));
        var lessons=await service.LessonsAsync("question","a");
        if(lessons.Items.Single().Count!=2||lessons.Items.Single().Feedback.Verdict!="ERROR")throw new Exception("Latest lesson aggregation failed");
        await feedback.RecordAsync(Input("CORRECTED"));
        if(!(await service.LessonsAsync("question","a")).Items.Single().Guidance.StartsWith("교정 피드백"))throw new Exception("Correction guidance missing");
        await feedback.RecordAsync(Input("DEAD_END"));
        var reused=await service.EvidenceAsync(request);
        if(reused.Edges.Length!=0||reused.Lessons.Single().Feedback.Verdict!="DEAD_END"
            ||(await store.GetGraphRevisionEdgesAsync("question",reused.Revision)).Count!=edges.Length)throw new Exception("Feedback rewrote facts or was not reused");
        await store.ApplyGraphScanAsync("question",scan("changed"),false);
        var stale=await service.LessonsAsync("question","a");
        if(stale.Items.Length!=0||stale.StaleExcluded!=4)throw new Exception("Stale feedback reused");
    }
}
