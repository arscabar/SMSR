using System.Net.Http.Headers;
using System.Net.Http;
using System.IO;
using System.Text;
using System.Text.Json;

namespace SMSR.App.Mvp;

public sealed class McpHttpGateway(string? address = null, string? token = null)
{
    private const string ProtocolVersion = "2026-07-28";
    private readonly HttpClient _client = new() { Timeout = TimeSpan.FromSeconds(10) };
    private readonly string _address = address ?? $"http://127.0.0.1:{LocalServer.Port}";
    private readonly string _token = token ?? new LocalTokenStore(Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SMSR", "mcp-token.bin")).GetOrCreate();
    private int _requestId;

    public async Task<string> CallAsync(string name, object arguments, CancellationToken cancellationToken = default)
    {
        var body = JsonSerializer.Serialize(new { jsonrpc = "2.0", id = Interlocked.Increment(ref _requestId), method = "tools/call", @params = new { name, arguments, _meta = Meta() } });
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_address}/mcp") { Content = new StringContent(body, Encoding.UTF8, "application/json") };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
        request.Headers.Accept.ParseAdd("application/json, text/event-stream");
        request.Headers.Add("MCP-Protocol-Version", ProtocolVersion);
        request.Headers.Add("MCP-Method", "tools/call");
        request.Headers.Add("MCP-Name", name);
        try
        {
            using var response = await _client.SendAsync(request, cancellationToken);
            var payload = await response.Content.ReadAsStringAsync(cancellationToken);
            return response.IsSuccessStatusCode ? ResultText(payload) : JsonSerializer.Serialize(new { error = $"SMSR 서버 호출 실패: {(int)response.StatusCode}" });
        }
        catch (HttpRequestException)
        {
            return JsonSerializer.Serialize(new { error = "SMSR 로컬 서버가 실행 중이 아닙니다." });
        }
    }

    private static Dictionary<string, object> Meta() => new()
    {
        ["io.modelcontextprotocol/protocolVersion"] = ProtocolVersion,
        ["io.modelcontextprotocol/clientInfo"] = new { name = "smsr-stdio", version = "1.0" },
        ["io.modelcontextprotocol/clientCapabilities"] = new { }
    };

    internal static string ResultText(string payload)
    {
        var json = payload.Split('\n').FirstOrDefault(line => line.StartsWith("data: ", StringComparison.Ordinal))?[6..] ?? payload;
        using var document = JsonDocument.Parse(json);
        return document.RootElement.TryGetProperty("result", out var result) && result.TryGetProperty("content", out var content) && content.GetArrayLength() > 0 && content[0].TryGetProperty("text", out var text)
            ? text.GetString() ?? "" : JsonSerializer.Serialize(new { error = "SMSR MCP 응답을 읽지 못했습니다." });
    }
}
