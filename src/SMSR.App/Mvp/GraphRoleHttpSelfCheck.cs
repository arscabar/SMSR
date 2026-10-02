using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace SMSR.App.Mvp;

internal static class GraphRoleHttpSelfCheck
{
    internal static async Task RunAsync(HttpClient client, string address, McpHttpGateway gateway)
    {
        foreach (var module in new[] { "symbols", "role-job", "role-coverage", "edge-layout", "edge-style", "viewport" })
        {
            using var asset = await client.GetAsync(address + "/assets/graph-explorer-" + module + ".js");
            if (!asset.IsSuccessStatusCode || asset.Content.Headers.ContentType?.MediaType != "text/javascript")
                throw new Exception("Role module not served: " + module);
        }
        const string id = "file:docs/a.md";
        var context = await client.GetFromJsonAsync<GraphRoleContext>(address + "/api/graph/role-context?projectId=sample&nodeId=" + Uri.EscapeDataString(id))
            ?? throw new Exception("Role context missing");
        var tool = JsonSerializer.Deserialize<GraphRoleContext>(await gateway.CallAsync("get_graph_role_context", new { projectId = "sample", nodeId = id }), GraphWorker.Json);
        if (context.Fingerprint != tool?.Fingerprint) throw new Exception("Role HTTP/MCP context mismatch");
        var request = new GraphRoleRequest("sample", id, context.Revision, context.Fingerprint, "host-agent-test",
            [new("ROLE", "현재 문서 본문의 첫 부분을 제공하는 문서입니다.", "EXTRACTED", ["e0"], context.Evidence[0].Quote.Split('\n')[0])]);
        using var denied = await client.PostAsJsonAsync(address + "/api/graph/role", request);
        if ((int)denied.StatusCode != 401) throw new Exception("Cross-origin role write accepted");
        var result = await gateway.CallAsync("submit_graph_role", new { request });
        if (result.Contains("\"error\"")) throw new Exception("Role MCP submission failed: " + result);
        var saved = await client.GetFromJsonAsync<GraphRoleStatus>(address + "/api/graph/role?projectId=sample&nodeId=" + Uri.EscapeDataString(id));
        if (saved?.Status != "CURRENT") throw new Exception("Role HTTP persistence failed");
        using var message = new HttpRequestMessage(HttpMethod.Post, address + "/api/graph/role")
        { Content = JsonContent.Create(request with { Fingerprint = "wrong" }) };
        message.Headers.Add("Origin", address);
        using var stale = await client.SendAsync(message);
        if ((int)stale.StatusCode != 409) throw new Exception("Stale role HTTP submission accepted");
    }
}
