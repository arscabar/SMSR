using System.Net.Http;
using System.Net.Http.Json;
using System.Net;

namespace SMSR.App.Mvp;

internal static class GraphDeepHttpSelfCheck
{
    internal static async Task RunAsync(string root)
    {
        await using var host = await LocalServer.StartAsync(root, 0);
        using var client = new HttpClient();
        var page = await client.GetFromJsonAsync<GraphRelationPage>(host.Address + "/api/graph/relations?projectId=deep&nodeId=caller&relation=CALLS");
        var trace = await client.GetFromJsonAsync<GraphTrace>(host.Address + "/api/graph/trace?projectId=deep&fromId=caller&toId=target&relation=CALLS");
        var impact = await client.GetFromJsonAsync<GraphImpact>(host.Address + "/api/graph/impact?projectId=deep&nodeId=target&relation=CALLS");
        var export = await client.GetFromJsonAsync<GraphScopeExport>(host.Address + "/api/graph/export?projectId=deep&nodeId=caller");
        var statuses = await client.GetFromJsonAsync<GraphDeepStatus[]>(host.Address + "/api/graph/deep-status?projectId=deep");
        var evidence = await client.GetFromJsonAsync<GraphDeepManifest>(host.Address +
            "/api/graph/deep-evidence?projectId=deep&revision=2&analyzer=csharp&analysisKey=key");
        if (page?.Items.Single().Edge.Analysis?.Single().Analyzer != "csharp" || trace?.Found != true ||
            impact?.Steps?.Single().Edge.Analysis?.Count != 1 || export?.Edges.Single().Analysis?.Count != 1 ||
            statuses?.Single().Applied != 1 || evidence?.InputHashes["A.cs"] != "hash")
            throw new Exception("HTTP relation/path/impact/export lost integrated analysis provenance");
        using var invalid = await client.GetAsync(host.Address + "/api/graph/deep-evidence?projectId=deep&revision=0&analyzer=csharp&analysisKey=key");
        using var absent = await client.GetAsync(host.Address + "/api/graph/deep-evidence?projectId=deep&revision=1&analyzer=csharp&analysisKey=key");
        if (invalid.StatusCode != HttpStatusCode.BadRequest || absent.StatusCode != HttpStatusCode.NotFound)
            throw new Exception("Deep evidence query accepted invalid revision or fabricated absent evidence");
    }
}
