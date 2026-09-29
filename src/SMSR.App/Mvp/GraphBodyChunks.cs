using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace SMSR.App.Mvp;

internal sealed record GraphBodyChunk(string Path, int Line, int EndLine, string Hash, string Text);
internal static class GraphBodyChunks
{
    // Conservative whole-file exclusion; not a general-purpose secret/DLP detector.
    private static readonly Regex Sensitive = new(
        "(?im)(-----BEGIN [A-Z ]*PRIVATE KEY|smds_[a-z0-9_-]{12,}|sk-[a-z0-9_-]{16,}|gh[pousr]_[a-z0-9]{20,}|AKIA[A-Z0-9]{16}|Bearer\\s+[a-z0-9._~-]{12,}|(?:password|passwd|secret|api[_-]?key|access[_-]?token)\\s*[\\\"']?\\s*[:=]\\s*[\\\"'][^\\\"'\\r\\n]{4,}[\\\"'])",
        RegexOptions.IgnoreCase | RegexOptions.Compiled, TimeSpan.FromMilliseconds(200));

    public static bool Excluded(string text) => Sensitive.IsMatch(text);

    public static IEnumerable<GraphBodyChunk> Split(string path, string text)
    {
        var line = 1;
        for (var start = 0; start < text.Length;)
        {
            var length = Math.Min(1000, text.Length - start);
            if (start + length < text.Length && char.IsHighSurrogate(text[start + length - 1])) length--;
            var part = text.Substring(start, length);
            var breaks = part.Count(c => c == '\n');
            if (!string.IsNullOrWhiteSpace(part))
                yield return new(path, line, line + breaks,
                    Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(part))), part);
            line += breaks;
            start += length;
        }
    }
}
