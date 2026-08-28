using System.Net;
using System.Net.Http;
using System.IO;

namespace SMSR.App.Mvp;

internal static class OAuthStandaloneSelfCheck
{
    public static async Task RunAsync()
    {
        var path = Path.Combine(Path.GetTempPath(), $"smsr-oauth-{Guid.NewGuid():N}");
        try
        {
            await using var server = await LocalServer.StartAsync(path, 0);
            using var client = new HttpClient();
            using var denied = await client.GetAsync($"{server.Address}/mcp");
            if (denied.StatusCode != HttpStatusCode.Unauthorized
                || !denied.Headers.WwwAuthenticate.ToString().Contains("resource_metadata", StringComparison.Ordinal))
                throw new InvalidOperationException("OAuth MCP challenge 검증이 실패했습니다.");
            await OAuthSelfCheck.RunAsync(server.Address);
            if (!server.HasAuthorizedCodex)
                throw new InvalidOperationException("OAuth 연결 상태 검증이 실패했습니다.");
        }
        finally
        {
            if (Directory.Exists(path)) Directory.Delete(path, true);
        }
    }
}
