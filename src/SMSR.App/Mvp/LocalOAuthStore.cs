using System.Security.Cryptography;
using System.Text;

namespace SMSR.App.Mvp;

public sealed class LocalOAuthStore
{
    private readonly object _gate = new();
    private readonly OAuthProtectedFile _file;
    private readonly OAuthPersistedState _state;

    public event EventHandler? AuthorizationChanged;

    public bool HasActiveAuthorization
    {
        get
        {
            lock (_gate) return _state.RefreshTokens.Any(token => token.ExpiresAt > DateTimeOffset.UtcNow);
        }
    }

    public LocalOAuthStore(string path)
    {
        _file = new(path);
        _state = _file.Load();
        Prune();
    }

    internal OAuthClient Register(string[] redirectUris, string clientName)
    {
        lock (_gate)
        {
            var existing = _state.Clients.FirstOrDefault(client => client.ClientName == clientName
                && client.RedirectUris.SequenceEqual(redirectUris, StringComparer.Ordinal));
            if (existing is not null) return existing;
            var client = new OAuthClient(RandomValue(24), redirectUris, clientName, DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            _state.Clients.Add(client);
            if (_state.Clients.Count > 50) _state.Clients.RemoveAt(0);
            _file.Save(_state);
            return client;
        }
    }

    internal OAuthClient? FindClient(string clientId)
    {
        lock (_gate) return _state.Clients.FirstOrDefault(client => client.ClientId == clientId);
    }

    internal bool ValidateAccess(string token, string audience)
    {
        lock (_gate)
        {
            Prune();
            return FindToken(_state.AccessTokens, token) is { } saved
                && saved.ExpiresAt > DateTimeOffset.UtcNow && saved.Audience == audience;
        }
    }

    internal OAuthTokenPair Issue(string clientId, string audience, string scope)
    {
        lock (_gate) return IssueCore(clientId, audience, scope);
    }

    internal OAuthTokenPair? RotateRefresh(string token, string clientId, string audience)
    {
        lock (_gate)
        {
            Prune();
            var saved = FindToken(_state.RefreshTokens, token);
            if (saved is null || saved.ExpiresAt <= DateTimeOffset.UtcNow || saved.ClientId != clientId || saved.Audience != audience)
                return null;
            _state.RefreshTokens.Remove(saved);
            return IssueCore(clientId, audience, saved.Scope);
        }
    }

    private OAuthTokenPair IssueCore(string clientId, string audience, string scope)
    {
        Prune();
        var access = RandomValue(32);
        var refresh = RandomValue(48);
        _state.AccessTokens.Add(new(Hash(access), clientId, audience, scope, DateTimeOffset.UtcNow.AddMinutes(15)));
        _state.RefreshTokens.Add(new(Hash(refresh), clientId, audience, scope, DateTimeOffset.UtcNow.AddDays(30)));
        _file.Save(_state);
        AuthorizationChanged?.Invoke(this, EventArgs.Empty);
        return new(access, refresh, 900, scope);
    }

    private void Prune()
    {
        var now = DateTimeOffset.UtcNow;
        _state.AccessTokens.RemoveAll(token => token.ExpiresAt <= now);
        _state.RefreshTokens.RemoveAll(token => token.ExpiresAt <= now);
    }

    private static OAuthTokenRecord? FindToken(IEnumerable<OAuthTokenRecord> records, string token)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return records.FirstOrDefault(record => CryptographicOperations.FixedTimeEquals(hash, Convert.FromHexString(record.Hash)));
    }

    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    private static string RandomValue(int bytes)
        => Convert.ToBase64String(RandomNumberGenerator.GetBytes(bytes)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
