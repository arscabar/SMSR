using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace SMSR.App.Mvp;

internal static class GraphExplorerHttpSelfCheck
{
    internal static async Task RunAsync(HttpClient client, string address, McpHttpGateway gateway)
    {
        await DashboardDisclosureHttpSelfCheck.RunAsync(client, address);
        await GraphRoleHttpSelfCheck.RunAsync(client, address, gateway);
        const string nodeId = "file:docs/a.md";
        const string encoded = "file%3Adocs%2Fa.md";
        var page = await client.GetFromJsonAsync<GraphRelationPage>(address + "/api/graph/relations?projectId=sample&nodeId=" + encoded);
        var tool = JsonSerializer.Deserialize<GraphRelationPage>(await gateway.CallAsync("get_graph_relations",
            new { projectId = "sample", nodeId }), GraphWorker.Json);
        if (page is null || tool is null || page.Total != tool.Total || page.Revision != tool.Revision)
            throw new Exception("Relation HTTP/MCP mismatch");
        var trace = await client.GetFromJsonAsync<GraphTrace>(address + "/api/graph/trace?projectId=sample&fromId=" + encoded + "&toId=" + encoded);
        var traceTool = JsonSerializer.Deserialize<GraphTrace>(await gateway.CallAsync("trace_graph_path",
            new { projectId = "sample", fromId = nodeId, toId = nodeId }), GraphWorker.Json);
        if (trace?.Found != true || traceTool?.Found != true) throw new Exception("Trace HTTP/MCP mismatch");
        foreach (var pair in new[] { ("explain", "explain_graph_node"), ("affected", "get_graph_affected") })
        {
            using var response = await client.GetAsync(address + "/api/graph/" + pair.Item1 + "?projectId=sample&nodeId=" + encoded);
            var result = await gateway.CallAsync(pair.Item2, new { projectId = "sample", nodeId });
            if (!response.IsSuccessStatusCode || result.Contains("\"error\"")) throw new Exception("Explorer tool/HTTP error");
        }
        var issueTool = await gateway.CallAsync("get_graph_issues", new { projectId = "sample" });
        if (issueTool.Contains("\"error\"")) throw new Exception("Issues MCP error");
        foreach (var module in new[] { "role", "related", "relations", "trace", "trace-result", "impact", "diagnostics", "evidence", "network", "svg", "render", "expand", "drag", "edges", "deep" })
        {
            using var asset = await client.GetAsync(address + "/assets/graph-explorer-" + module + ".js?v=2");
            if (!asset.IsSuccessStatusCode || asset.Headers.CacheControl?.NoStore != true)
                throw new Exception("Explorer module not served: " + module);
        }
        using var bad = await client.GetAsync(address + "/api/graph/relations?projectId=sample&nodeId=" + encoded + "&offset=-1");
        if ((int)bad.StatusCode != 400) throw new Exception("Invalid page not rejected");
    }
}
