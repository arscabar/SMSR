using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace SMSR.App.Mvp;

internal static class OAuthDiscoveryEndpoints
{
    public static void Map(WebApplication app)
    {
        app.MapGet("/.well-known/oauth-protected-resource", ProtectedResource);
        app.MapGet("/.well-known/oauth-protected-resource/mcp", ProtectedResource);
        app.MapGet("/.well-known/oauth-authorization-server", AuthorizationServer);
    }

    private static IResult ProtectedResource(HttpRequest request) => Results.Json(new
    {
        resource = OAuthUris.Resource(request),
        authorization_servers = new[] { OAuthUris.Base(request) },
        scopes_supported = new[] { OAuthUris.Scope },
        bearer_methods_supported = new[] { "header" }
    });

    private static IResult AuthorizationServer(HttpRequest request)
    {
        var root = OAuthUris.Base(request);
        return Results.Json(new
        {
            issuer = root,
            authorization_endpoint = $"{root}/oauth/authorize",
            token_endpoint = $"{root}/oauth/token",
            registration_endpoint = $"{root}/oauth/register",
            response_types_supported = new[] { "code" },
            grant_types_supported = new[] { "authorization_code", "refresh_token" },
            token_endpoint_auth_methods_supported = new[] { "none" },
            code_challenge_methods_supported = new[] { "S256" },
            scopes_supported = new[] { OAuthUris.Scope },
            authorization_response_iss_parameter_supported = true
        });
    }
}
