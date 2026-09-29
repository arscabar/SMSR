using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SMSR.App.Mvp;

internal static class GraphBodyEndpointSelfCheck
{
    public static async Task RunAsync(string root)
    {
        var folder = Path.Combine(root, "body-http");
        await using var host = await LocalServer.StartAsync(folder, 0);
        var store = new EventStore(Path.Combine(folder, "smsr.db"));
        var text = "본문에서 파일 저장과 오류 복구 과정을 검색합니다.";
        await File.WriteAllTextAsync(Path.Combine(root, "body-http.txt"), text, new UTF8Encoding(false));
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
        await store.ApplyGraphScanAsync("body", new(root, [new("body-http.txt", hash, "code")],
            [new("file:body-http.txt", "body-http.txt", "code", "body-http.txt", "body-http.txt", 1, hash)],
            [], [], ["body-http.txt"], [], []), false);
        using var client = new HttpClient();
        var url = host.Address + "/api/graph/body-semantic";
        using var denied = await client.PostAsJsonAsync(url, new { projectId = "body", input = "파일 저장" });
        if (denied.StatusCode != HttpStatusCode.Unauthorized) throw new Exception("본문 검색 출처 보호 실패");
        client.DefaultRequestHeaders.Add("Origin", host.Address);
        using var response = await client.PostAsJsonAsync(url, new { projectId = "body", input = "파일 저장", scope = "body-http.txt" });
        var result = await response.Content.ReadFromJsonAsync<GraphBodyResult>();
        if (!response.IsSuccessStatusCode || result?.Hits.Single().Path != "body-http.txt") throw new Exception("본문 HTTP 검색 실패");
        var gateway = new McpHttpGateway(host.Address, folder);
        var tool = JsonSerializer.Deserialize<JsonElement>(await gateway.CallAsync("search_graph_body", new { projectId = "body", query = "오류 복구" }));
        if (tool.GetProperty("hits")[0].GetProperty("path").GetString() != "body-http.txt") throw new Exception("본문 MCP 검색 실패");
        using var invalid = await client.PostAsJsonAsync(url, new { projectId = "body", input = "search", scope = "../outside" });
        if (invalid.StatusCode != HttpStatusCode.BadRequest) throw new Exception("본문 범위 입력 검증 실패");
    }
}
