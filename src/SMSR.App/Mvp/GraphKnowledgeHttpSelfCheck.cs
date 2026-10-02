using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
namespace SMSR.App.Mvp;

internal static class GraphKnowledgeHttpSelfCheck
{
    internal static async Task RunAsync(string root)
    {
        await using var host = await LocalServer.StartAsync(root, 0);
        using var client = new HttpClient();
        var gateway = new McpHttpGateway(host.Address, root);
        var url = host.Address + "/api/graph/";
        var groups = await client.GetFromJsonAsync<GraphHyperedgePage>(url + "hyperedges?projectId=knowledge&nodeId=method");
        var tool = JsonSerializer.Deserialize<GraphHyperedgePage>(await gateway.CallAsync("get_graph_hyperedges",
            new { projectId = "knowledge", nodeId = "method" }), GraphWorker.Json);
        var coverage = await client.GetFromJsonAsync<GraphCoverage>(url + "coverage?projectId=knowledge");
        var graphCoverage = JsonSerializer.Deserialize<GraphCoverage>(await gateway.CallAsync("get_graph_coverage",
            new { projectId = "knowledge" }), GraphWorker.Json);
        if (groups?.Items.Count != 1 || tool?.Items.Single().Members.Count != 3
            || coverage?.DetailedSymbols != 3 || graphCoverage?.DetailedSymbols != 3)
            throw new Exception("Knowledge HTTP/MCP roundtrip failed");
        var relations = await client.GetFromJsonAsync<GraphRelationPage>(url + "relations?projectId=knowledge&nodeId=method");
        if (relations?.Items.Single().Edge.Evidence?.SourceHash != GraphKnowledgeFixture.Hash
            || relations.Items.Single().Node.Details?.EntityKind != "function")
            throw new Exception("Relations HTTP lost metadata/evidence");
        await GraphStructureHttpSelfCheck.RunAsync(client, url);
        await GraphVisualSelfCheck.RunAsync(client, url);
        foreach (var query in new[] { "hyperedges?projectId=knowledge&offset=-1", "coverage?projectId=knowledge&limit=0" })
        {
            using var response = await client.GetAsync(url + query);
            if (response.StatusCode != HttpStatusCode.BadRequest) throw new Exception("Invalid knowledge query not rejected");
        }
    }
}
