using System.Text.RegularExpressions;

namespace SMSR.App.Mvp;

internal static class GraphDocumentRedaction
{
    private static readonly Regex Values = new("(?i)(?:api[_-]?key|password|secret|token)\\s*[:=]\\s*[\\\"']?[^\\s\\\"']{8,}",
        RegexOptions.Compiled, TimeSpan.FromMilliseconds(200));
    internal static bool Excluded(string value) => GraphBodyChunks.Excluded(value) || Values.IsMatch(value);
}
