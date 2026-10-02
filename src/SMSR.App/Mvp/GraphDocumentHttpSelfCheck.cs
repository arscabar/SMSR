using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace SMSR.App.Mvp;

internal static class GraphDocumentHttpSelfCheck
{
    internal static async Task RunAsync(string root)
    {
        await using var host = await LocalServer.StartAsync(root, 0); using var client = new HttpClient();
        var doc = await client.GetFromJsonAsync<GraphDocument>(host.Address + "/api/graph/document?projectId=documents&path=plan.docx");
        var gateway = new McpHttpGateway(host.Address, root);
        var tool = JsonSerializer.Deserialize<GraphDocument>(await gateway.CallAsync("read_graph_document", new { projectId = "documents", path = "plan.docx" }), GraphWorker.Json);
        if (doc?.SourceHash != tool?.SourceHash || doc?.Blocks.Single().Text != "Office requirement") throw new Exception("Document HTTP/MCP mismatch");
        var text = await client.GetFromJsonAsync<GraphDocument>(host.Address + "/api/graph/document?projectId=documents&path=plan.md");
        var request = GraphSemanticFixture.Request(text!);
        using(var deniedPost=await client.PostAsJsonAsync(host.Address+"/api/graph/document-semantic",request))
            if((int)deniedPost.StatusCode!=401)throw new Exception("Cross-origin semantic mutation accepted");
        client.DefaultRequestHeaders.Add("Origin", host.Address);
        using (var post = await client.PostAsJsonAsync(host.Address + "/api/graph/document-semantic", request))
            if (!post.IsSuccessStatusCode) throw new Exception("Semantic HTTP submit failed");
        text = await client.GetFromJsonAsync<GraphDocument>(host.Address + "/api/graph/document?projectId=documents&path=plan.md");
        var submitted = await gateway.CallAsync("submit_graph_document_semantics", new { request = GraphSemanticFixture.Request(text!) });
        if (!submitted.Contains("semantic-v1")) throw new Exception("Semantic MCP submit failed");
        var result = JsonSerializer.Deserialize<GraphSemanticStatus>(await gateway.CallAsync("get_graph_document_semantics", new { projectId = "documents", path = "plan.md" }), GraphWorker.Json);
        if (result?.Stale != false || result.Report.Nodes.Length != 3) throw new Exception("Semantic MCP result failed");
        var page = await client.GetStringAsync(host.Address + "/graph/document?projectId=documents&path=plan.docx&location=paragraph:1");
        if (!page.Contains("Office requirement") || !page.Contains("id=\"selected-evidence\"")) throw new Exception("Document evidence page failed");
        await GraphOfficeSemanticSelfCheck.RunAsync(gateway);
        using var denied = await client.GetAsync(host.Address + "/api/graph/document?projectId=documents&path=../outside.docx");
        if ((int)denied.StatusCode != 404 && (int)denied.StatusCode != 400) throw new Exception("Document boundary accepted");
    }
}
