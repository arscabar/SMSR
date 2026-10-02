using System.Text.Json;
namespace SMSR.App.Mvp;
internal static class GraphSkillFlowSelfCheck
{
    internal static async Task RunAsync(string root)
    {
        await using var host=await LocalServer.StartAsync(root,0);var gateway=new McpHttpGateway(host.Address,root);
        var explanation=JsonSerializer.Deserialize<GraphExplanation>(await gateway.CallAsync("explain_graph_node",new{projectId="question",nodeId="b"}),GraphWorker.Json);
        if(explanation?.Node.Label!="PaymentLedger"||explanation.SourceStatus=="CURRENT")throw new Exception("Explanation role source incorrectly claimed current");
        var affected=JsonSerializer.Deserialize<GraphImpact>(await gateway.CallAsync("get_graph_affected",new{projectId="question",nodeId="b"}),GraphWorker.Json);
        if(affected?.Nodes.Any(n=>n.NodeId=="a")!=true||affected.Nodes.Any(n=>n.NodeId=="c"))throw new Exception("Impact skill path failed");
        var relations=await gateway.CallAsync("get_graph_relations",new{projectId="question",nodeId="b",direction="incoming",expectedRevision=explanation.Revision});
        if(!relations.Contains("CALLS"))throw new Exception("Explain skill original relation missing");
        var coverage=JsonSerializer.Deserialize<GraphCoverage>(await gateway.CallAsync("get_graph_coverage",new{projectId="question"}),GraphWorker.Json);
        if(coverage?.UnknownSymbols!=4||coverage.DetailedSymbols!=0)throw new Exception("Maintenance skill unknown coverage guessed");
        var empty=JsonSerializer.Deserialize<GraphQuestionSearch>(await gateway.CallAsync("search_graph_question",new{request=new GraphQuestionRequest("question","not-a-real-word")}),GraphWorker.Json);
        if(empty?.Hits.Length!=0)throw new Exception("No-match skill fabricated evidence");
    }
}
