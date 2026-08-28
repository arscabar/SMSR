namespace SMSR.App.Mvp;

internal sealed record OAuthClient(string ClientId, string[] RedirectUris, string ClientName, long IssuedAt);

internal sealed record OAuthTokenRecord(
    string Hash,
    string ClientId,
    string Audience,
    string Scope,
    DateTimeOffset ExpiresAt);

internal sealed record OAuthTokenPair(string AccessToken, string RefreshToken, int ExpiresIn, string Scope);

internal sealed record OAuthAuthorizationRequest(
    string ClientId,
    string RedirectUri,
    string State,
    string CodeChallenge,
    string Scope,
    string Resource,
    DateTimeOffset ExpiresAt);

internal sealed record OAuthAuthorizationCode(
    string ClientId,
    string RedirectUri,
    string CodeChallenge,
    string Scope,
    string Resource,
    DateTimeOffset ExpiresAt);

internal sealed class OAuthPersistedState
{
    public List<OAuthClient> Clients { get; init; } = [];
    public List<OAuthTokenRecord> AccessTokens { get; init; } = [];
    public List<OAuthTokenRecord> RefreshTokens { get; init; } = [];
}
