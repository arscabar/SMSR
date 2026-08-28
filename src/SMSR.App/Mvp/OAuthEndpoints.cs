using Microsoft.AspNetCore.Builder;

namespace SMSR.App.Mvp;

internal static class OAuthEndpoints
{
    public static void Map(WebApplication app, LocalOAuthStore store, OAuthFlowStore flows, OAuthAuditLog audit)
    {
        OAuthDiscoveryEndpoints.Map(app);
        OAuthRegistrationEndpoints.Map(app, store, audit);
        OAuthAuthorizationEndpoints.Map(app, store, flows, audit);
        OAuthTokenEndpoints.Map(app, store, flows, audit);
    }
}
