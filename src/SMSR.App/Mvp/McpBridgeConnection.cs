using System.IO;
using System.Net.Http;

namespace SMSR.App.Mvp;

internal static class McpBridgeConnection
{
    public static async Task<bool> NotifyAsync(
        string? address = null, string? dataPath = null, CancellationToken cancellationToken = default)
    {
        address ??= $"http://127.0.0.1:{LocalServer.Port}";
        dataPath ??= Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SMSR");
        var token = new McpBridgeToken(dataPath).Value;
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        for (var attempt = 0; attempt <= 30; attempt++)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, $"{address}/api/mcp-bridge/connected");
                request.Headers.Add("X-SMSR-Bridge-Token", token);
                using var response = await client.SendAsync(request, cancellationToken);
                return response.IsSuccessStatusCode;
            }
            catch (HttpRequestException) when (attempt < 30)
            {
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested && attempt < 30)
            {
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }
        return false;
    }
}
