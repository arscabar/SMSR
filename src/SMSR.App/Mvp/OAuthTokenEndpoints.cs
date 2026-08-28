using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace SMSR.App.Mvp;

internal static class OAuthTokenEndpoints
{
    public static void Map(WebApplication app, LocalOAuthStore store, OAuthFlowStore flows, OAuthAuditLog audit)
        => app.MapPost("/oauth/token", (HttpRequest request) => ExchangeAsync(request, store, flows, audit));

    private static async Task<IResult> ExchangeAsync(HttpRequest request, LocalOAuthStore store, OAuthFlowStore flows, OAuthAuditLog audit)
    {
        if (!request.HasFormContentType)
        {
            await audit.WriteAsync("token", "rejected_content_type");
            return Error("invalid_request");
        }
        var form = await request.ReadFormAsync();
        var clientId = form["client_id"].ToString();
        var expectedResource = OAuthUris.Resource(request);
        var resource = form["resource"].ToString();
        if (string.IsNullOrWhiteSpace(resource)) resource = expectedResource;
        if (store.FindClient(clientId) is null)
        {
            await audit.WriteAsync("token", "rejected_client");
            return Error("invalid_client");
        }
        if (!OAuthValidation.IsResource(resource, expectedResource))
        {
            await audit.WriteAsync("token", "rejected_resource");
            return Error("invalid_target");
        }

        OAuthTokenPair? pair;
        var grantType = form["grant_type"].ToString();
        if (grantType == "authorization_code")
        {
            var grant = flows.Exchange(form["code"].ToString(), clientId, form["redirect_uri"].ToString(),
                resource, form["code_verifier"].ToString());
            pair = grant is null ? null : store.Issue(clientId, resource, grant.Scope);
        }
        else if (grantType == "refresh_token")
        {
            pair = store.RotateRefresh(form["refresh_token"].ToString(), clientId, resource);
        }
        else
        {
            await audit.WriteAsync("token", "rejected_grant_type");
            return Error("unsupported_grant_type");
        }

        if (pair is null)
        {
            await audit.WriteAsync("token", "rejected_grant");
            return Error("invalid_grant");
        }
        await audit.WriteAsync("token", grantType == "refresh_token" ? "refresh_issued" : "access_issued");
        request.HttpContext.Response.Headers.CacheControl = "no-store";
        request.HttpContext.Response.Headers.Pragma = "no-cache";
        return Results.Json(new
        {
            access_token = pair.AccessToken,
            token_type = "Bearer",
            expires_in = pair.ExpiresIn,
            refresh_token = pair.RefreshToken,
            scope = pair.Scope
        });
    }

    private static IResult Error(string error)
        => Results.Json(new { error }, statusCode: StatusCodes.Status400BadRequest);
}
