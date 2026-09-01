using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace SMSR.App.Mvp;

public sealed class McpHttpGateway
{
    private const string ProtocolVersion = "2026-07-28";
    private readonly HttpClient _client = new() { Timeout = TimeSpan.FromSeconds(10) };
    private readonly string _address;
    private readonly string _token;
    private int _requestId;

    public McpHttpGateway() : this(null, null) { }

    internal McpHttpGateway(string? address, string? dataPath)
    {
        _address = address ?? $"http://127.0.0.1:{LocalServer.Port}";
        dataPath ??= Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SMSR");
        _token = new McpBridgeToken(dataPath).Value;
    }

    public async Task<string> CallAsync(string name, object arguments, CancellationToken cancellationToken = default)
    {
        var body = JsonSerializer.Serialize(new
        {
            jsonrpc = "2.0",
            id = Interlocked.Increment(ref _requestId),
            method = "tools/call",
            @params = new { name, arguments, _meta = Metadata() }
        });
        for (var attempt = 0; ; attempt++)
        {
            try { return await SendAsync(name, body, cancellationToken); }
            catch (HttpRequestException) when (attempt < 30)
            {
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }
    }

    private async Task<string> SendAsync(string name, string body, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_address}/mcp")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
        request.Headers.Accept.ParseAdd("application/json, text/event-stream");
        request.Headers.Add("MCP-Protocol-Version", ProtocolVersion);
        request.Headers.Add("MCP-Method", "tools/call");
        request.Headers.Add("MCP-Name", name);
        using var response = await _client.SendAsync(request, cancellationToken);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        return response.IsSuccessStatusCode ? McpHttpResponse.Text(payload)
            : JsonSerializer.Serialize(new { error = $"SMSR 서버 호출 실패: {(int)response.StatusCode}" });
    }

    private static Dictionary<string, object> Metadata() => new()
    {
        ["io.modelcontextprotocol/protocolVersion"] = ProtocolVersion,
        ["io.modelcontextprotocol/clientInfo"] = new { name = "smsr-stdio", version = "1.0" },
        ["io.modelcontextprotocol/clientCapabilities"] = new { }
    };

}
