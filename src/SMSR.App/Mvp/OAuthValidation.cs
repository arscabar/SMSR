namespace SMSR.App.Mvp;

internal static class OAuthValidation
{
    public static bool IsAllowedRedirect(string value)
        => TryLoopback(value, out var uri) && string.IsNullOrEmpty(uri.Fragment);

    public static bool MatchesRedirect(string requested, IEnumerable<string> registered)
    {
        if (registered.Contains(requested, StringComparer.Ordinal)) return true;
        if (!TryLoopback(requested, out var actual)) return false;
        return registered.Any(value => TryLoopback(value, out var saved)
            && saved.Scheme.Equals(actual.Scheme, StringComparison.OrdinalIgnoreCase)
            && saved.Host.Equals(actual.Host, StringComparison.OrdinalIgnoreCase)
            && saved.AbsolutePath == actual.AbsolutePath
            && saved.Query == actual.Query);
    }

    public static bool HasScope(string value)
        => value.Split(' ', StringSplitOptions.RemoveEmptyEntries).Contains(OAuthUris.Scope, StringComparer.Ordinal);

    public static bool IsResource(string value, string expected)
        => Uri.TryCreate(value, UriKind.Absolute, out var resource)
            && Uri.TryCreate(expected, UriKind.Absolute, out var target)
            && Uri.Compare(resource, target, UriComponents.HttpRequestUrl, UriFormat.SafeUnescaped, StringComparison.OrdinalIgnoreCase) == 0;

    private static bool TryLoopback(string value, out Uri uri)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out uri!)) return false;
        return (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
            && (uri.Host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase)
                || uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
                || uri.Host.Equals("::1", StringComparison.OrdinalIgnoreCase));
    }
}
