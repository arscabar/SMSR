using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;

namespace SMSR.App.Mvp;

internal static class OAuthAuthorizationEndpoints
{
    public static void Map(WebApplication app, LocalOAuthStore clients, OAuthFlowStore flows, OAuthAuditLog audit)
    {
        app.MapGet("/oauth/authorize", (HttpRequest request) => BeginAsync(request, clients, flows, audit));
        app.MapPost("/oauth/authorize", (HttpRequest request) => CompleteAsync(request, flows, audit));
    }

    private static async Task<IResult> BeginAsync(HttpRequest request, LocalOAuthStore clients, OAuthFlowStore flows, OAuthAuditLog audit)
    {
        var query = request.Query;
        var clientId = query["client_id"].ToString();
        var redirectUri = query["redirect_uri"].ToString();
        var state = query["state"].ToString();
        var challenge = query["code_challenge"].ToString();
        var scope = query["scope"].ToString();
        if (string.IsNullOrWhiteSpace(scope)) scope = OAuthUris.Scope;
        var expectedResource = OAuthUris.Resource(request);
        var resource = query["resource"].ToString();
        if (string.IsNullOrWhiteSpace(resource)) resource = expectedResource;
        var client = clients.FindClient(clientId);
        if (client is null)
        {
            await audit.WriteAsync("authorize", "rejected_client");
            return Results.BadRequest(new { error = "invalid_request", error_description = "OAuth client가 등록되어 있지 않습니다." });
        }

        var error = query["response_type"] != "code" ? "response_type"
            : string.IsNullOrWhiteSpace(state) ? "state"
            : query["code_challenge_method"] != "S256" || string.IsNullOrWhiteSpace(challenge) ? "pkce"
            : !OAuthValidation.MatchesRedirect(redirectUri, client.RedirectUris) ? "redirect"
            : !OAuthValidation.HasScope(scope) ? "scope"
            : !OAuthValidation.IsResource(resource, expectedResource) ? "resource" : null;
        if (error is not null)
        {
            await audit.WriteAsync("authorize", $"rejected_{error}");
            return Results.BadRequest(new { error = "invalid_request", error_description = "OAuth 요청 또는 callback URI가 올바르지 않습니다." });
        }

        var id = flows.Add(new(clientId, redirectUri, state, challenge, scope, resource, DateTimeOffset.UtcNow.AddMinutes(5)));
        await audit.WriteAsync("authorize", "consent_shown");
        request.HttpContext.Response.Headers.CacheControl = "no-store";
        request.HttpContext.Response.Headers["X-Content-Type-Options"] = "nosniff";
        request.HttpContext.Response.Headers["X-Frame-Options"] = "DENY";
        var callbackOrigin = new Uri(redirectUri).GetLeftPart(UriPartial.Authority);
        request.HttpContext.Response.Headers.ContentSecurityPolicy =
            $"default-src 'none'; style-src 'unsafe-inline'; form-action 'self' {callbackOrigin}; frame-ancestors 'none'; base-uri 'none'";
        return Results.Content(OAuthConsentPage.Render(id, client.ClientName), "text/html; charset=utf-8");
    }

    private static async Task<IResult> CompleteAsync(HttpRequest request, OAuthFlowStore flows, OAuthAuditLog audit)
    {
        if (!request.HasFormContentType)
        {
            await audit.WriteAsync("consent", "rejected_content_type");
            return Results.BadRequest();
        }
        var form = await request.ReadFormAsync();
        var id = form["request_id"].ToString();
        if (form["decision"] == "deny")
        {
            var denied = flows.Deny(id);
            await audit.WriteAsync("consent", denied is null ? "rejected_expired" : "denied");
            return denied is null ? Results.BadRequest() : Redirect(denied.RedirectUri, new()
            {
                ["error"] = "access_denied", ["state"] = denied.State, ["iss"] = OAuthUris.Base(request)
            });
        }

        var approved = flows.Approve(id);
        await audit.WriteAsync("consent", approved is null ? "rejected_expired" : "approved_redirected");
        return approved is null ? Results.BadRequest() : Redirect(approved.Value.Request.RedirectUri, new()
        {
            ["code"] = approved.Value.Code,
            ["state"] = approved.Value.Request.State,
            ["iss"] = OAuthUris.Base(request)
        });
    }

    private static IResult Redirect(string uri, Dictionary<string, string?> values)
        => Results.Redirect(QueryHelpers.AddQueryString(uri, values));
}
