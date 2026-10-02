using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
namespace SMSR.App.Mvp;
internal static class GraphQuestionHttpSelfCheck
{
    internal static async Task RunAsync(string root,GraphQuestionRequest request)
    {
        await using var host=await LocalServer.StartAsync(root,0);using var client=new HttpClient();
        using(var denied=await client.PostAsJsonAsync(host.Address+"/api/graph/question-search",request))
            if((int)denied.StatusCode!=401)throw new Exception("Question cross-origin request accepted");
        client.DefaultRequestHeaders.Add("Origin",host.Address);
        using(var http=await client.PostAsJsonAsync(host.Address+"/api/graph/question-evidence",request))
        {
            if(!http.IsSuccessStatusCode)throw new Exception("Question HTTP failed");
            var evidence=await http.Content.ReadFromJsonAsync<GraphQuestionEvidence>();
            if(evidence?.Nodes.Length!=2)throw new Exception("Question HTTP evidence mismatch");
        }
        var gateway=new McpHttpGateway(host.Address,root);
        var response=await gateway.CallAsync("search_graph_question",new{request});
        if(JsonSerializer.Deserialize<GraphQuestionSearch>(response,GraphWorker.Json)?.Hits.Single().Node.NodeId!="a")throw new Exception("Question MCP search mismatch");
        var evidenceTool=JsonSerializer.Deserialize<GraphQuestionEvidence>(await gateway.CallAsync("get_graph_question_evidence",new{request}),GraphWorker.Json);
        var vocabulary=JsonSerializer.Deserialize<GraphVocabulary>(await gateway.CallAsync("get_graph_vocabulary",new{projectId="question"}),GraphWorker.Json);
        var lessons=JsonSerializer.Deserialize<GraphFeedbackLessons>(await gateway.CallAsync("get_graph_feedback_lessons",new{projectId="question",sourceId="a"}),GraphWorker.Json);
        if(evidenceTool?.Edges.Single().TargetId!="b"||vocabulary?.Terms.Any(t=>t.Term=="entry")!=true||lessons?.StaleExcluded!=4)
            throw new Exception("Query skill MCP evidence/vocabulary/lesson flow failed");
        try{await new GraphQuestionService(new EventStore(System.IO.Path.Combine(root,"smsr.db")),new GraphFeedbackService(new EventStore(System.IO.Path.Combine(root,"smsr.db"))))
            .SearchAsync(request with{Terms=["not-in-vocabulary"]});throw new Exception("Invented query term accepted");}catch(ArgumentException){}
    }
}
