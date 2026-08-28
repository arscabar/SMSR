using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace SMSR.App.Mvp;

internal static class OAuthRegistrationEndpoints
{
    public static void Map(WebApplication app, LocalOAuthStore store, OAuthAuditLog audit)
        => app.MapPost("/oauth/register", (OAuthRegistrationRequest request) => RegisterAsync(request, store, audit));

    private static async Task<IResult> RegisterAsync(OAuthRegistrationRequest request, LocalOAuthStore store, OAuthAuditLog audit)
    {
        if (request.RedirectUris is not { Length: > 0 } || request.RedirectUris.Any(uri => !OAuthValidation.IsAllowedRedirect(uri)))
        {
            await audit.WriteAsync("register", "rejected_redirect");
            return Error("invalid_redirect_uri", "Codex의 loopback callback URI만 등록할 수 있습니다.");
        }
        if (request.TokenEndpointAuthMethod is not null and not "none")
        {
            await audit.WriteAsync("register", "rejected_auth_method");
            return Error("invalid_client_metadata", "공개 PKCE 클라이언트만 지원합니다.");
        }
        if (request.ResponseTypes?.Contains("code") == false || request.GrantTypes?.Contains("authorization_code") == false)
        {
            await audit.WriteAsync("register", "rejected_grant");
            return Error("invalid_client_metadata", "authorization_code grant가 필요합니다.");
        }

        var client = store.Register(request.RedirectUris, string.IsNullOrWhiteSpace(request.ClientName) ? "Codex" : request.ClientName);
        await audit.WriteAsync("register", "accepted");
        return Results.Json(new
        {
            client_id = client.ClientId,
            client_id_issued_at = client.IssuedAt,
            redirect_uris = client.RedirectUris,
            client_name = client.ClientName,
            token_endpoint_auth_method = "none",
            grant_types = new[] { "authorization_code", "refresh_token" },
            response_types = new[] { "code" }
        }, statusCode: StatusCodes.Status201Created);
    }

    private static IResult Error(string error, string description)
        => Results.Json(new { error, error_description = description }, statusCode: StatusCodes.Status400BadRequest);
}

internal sealed record OAuthRegistrationRequest(
    [property: JsonPropertyName("redirect_uris")] string[] RedirectUris,
    [property: JsonPropertyName("client_name")] string? ClientName,
    [property: JsonPropertyName("token_endpoint_auth_method")] string? TokenEndpointAuthMethod,
    [property: JsonPropertyName("grant_types")] string[]? GrantTypes,
    [property: JsonPropertyName("response_types")] string[]? ResponseTypes);
