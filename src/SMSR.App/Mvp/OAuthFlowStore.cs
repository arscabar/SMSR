using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace SMSR.App.Mvp;

internal sealed class OAuthFlowStore
{
    private readonly ConcurrentDictionary<string, OAuthAuthorizationRequest> _pending = new();
    private readonly Dictionary<string, OAuthAuthorizationCode> _codes = [];
    private readonly object _gate = new();

    public string Add(OAuthAuthorizationRequest request)
    {
        var id = RandomValue(24);
        _pending[id] = request;
        return id;
    }

    public OAuthAuthorizationRequest? Deny(string id)
        => _pending.TryRemove(id, out var request) && request.ExpiresAt > DateTimeOffset.UtcNow ? request : null;

    public (OAuthAuthorizationRequest Request, string Code)? Approve(string id)
    {
        if (!_pending.TryRemove(id, out var request) || request.ExpiresAt <= DateTimeOffset.UtcNow) return null;
        var code = RandomValue(32);
        lock (_gate) _codes[code] = new(request.ClientId, request.RedirectUri, request.CodeChallenge,
            request.Scope, request.Resource, DateTimeOffset.UtcNow.AddMinutes(5));
        return (request, code);
    }

    public OAuthAuthorizationCode? Exchange(string code, string clientId, string redirectUri, string resource, string verifier)
    {
        lock (_gate)
        {
            if (!_codes.TryGetValue(code, out var grant) || grant.ExpiresAt <= DateTimeOffset.UtcNow
                || grant.ClientId != clientId || grant.RedirectUri != redirectUri || grant.Resource != resource
                || !VerifyChallenge(verifier, grant.CodeChallenge)) return null;
            _codes.Remove(code);
            return grant;
        }
    }

    private static bool VerifyChallenge(string verifier, string challenge)
    {
        var actual = Convert.ToBase64String(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        return CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(actual), Encoding.ASCII.GetBytes(challenge));
    }

    private static string RandomValue(int bytes)
        => Convert.ToBase64String(RandomNumberGenerator.GetBytes(bytes)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
