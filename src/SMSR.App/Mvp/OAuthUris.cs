using Microsoft.AspNetCore.Http;

namespace SMSR.App.Mvp;

internal static class OAuthUris
{
    public const string Scope = "smsr:mcp";

    public static string Base(HttpRequest request)
    {
        var port = request.Host.Port ?? LocalServer.Port;
        return $"http://127.0.0.1:{port}";
    }

    public static string Resource(HttpRequest request) => $"{Base(request)}/mcp";
    public static string Metadata(HttpRequest request) => $"{Base(request)}/.well-known/oauth-protected-resource/mcp";
}
