using System.Text.Json;
namespace SMSR.App.Mvp;
internal static class GraphOfficeSemanticSelfCheck
{
    internal static async Task RunAsync(McpHttpGateway gateway)
    {
        var doc=JsonSerializer.Deserialize<GraphDocument>(await gateway.CallAsync("read_graph_document",new{projectId="documents",path="plan.docx"}),GraphWorker.Json)!;
        var block=doc.Blocks.Single();var request=new GraphSemanticRequest("documents","plan.docx",doc.SourceHash,doc.Revision,
            "office-fixture-host","semantic-v1",[new("office-check","requirement","Office requirement",block.Location,block.Text)],[]);
        await gateway.CallAsync("submit_graph_document_semantics",new{request});
        var status=JsonSerializer.Deserialize<GraphSemanticStatus>(await gateway.CallAsync("get_graph_document_semantics",new{projectId="documents",path="plan.docx"}),GraphWorker.Json);
        if(status?.Stale!=false||status.Report.Nodes.Single().Location!="paragraph:1")throw new Exception("Office semantic MCP provenance roundtrip failed");
    }
}
