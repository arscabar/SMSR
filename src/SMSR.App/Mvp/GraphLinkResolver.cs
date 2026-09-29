using System.IO;
using System.Text;

namespace SMSR.App.Mvp;

internal sealed record GraphLinkResult(string? Path, string? Reason, IReadOnlyList<string> Candidates);

internal static class GraphLinkResolver
{
    public static GraphLinkResult Resolve(string root, string source, string raw, IReadOnlySet<string> indexed)
    {
        var value = raw.Trim();
        if (value.StartsWith('<'))
        {
            var close = value.IndexOf('>');
            if (close < 0) return new(null, "지원하지 않는 링크 형식", []);
            value = value[1..close];
        }
        if (value.Length == 0 || value.StartsWith('#') || value.StartsWith("//")) return new(null, null, []);
        if (value.Contains('\\') || value.Contains('"')) return new(null, "지원하지 않는 링크 형식", []);
        if (!value.StartsWith('/') && Uri.TryCreate(value, UriKind.Absolute, out _)) return new(null, null, []);
        var anchor = value.IndexOfAny(['#', '?']);
        if (anchor >= 0) value = value[..anchor];
        if (value.Length == 0) return new(null, null, []);
        try
        {
            value = Uri.UnescapeDataString(value).Normalize(NormalizationForm.FormC);
            var full = value.StartsWith('/')
                ? Path.GetFullPath(Path.Combine(root, value.TrimStart('/').Replace('/', Path.DirectorySeparatorChar)))
                : Path.GetFullPath(Path.Combine(root, Path.GetDirectoryName(source) ?? "", value.Replace('/', Path.DirectorySeparatorChar)));
            if (!full.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                return new(null, "저장소 범위 밖 참조", []);
            var relative = Path.GetRelativePath(root, full).Replace('\\', '/').Normalize(NormalizationForm.FormC);
            if (indexed is HashSet<string> paths && paths.TryGetValue(relative, out var canonical))
                return new(canonical, null, []);
            var normalized = CanonicalPath(indexed, relative);
            if (normalized is not null) return new(normalized, null, []);
            var name = Path.GetFileName(relative);
            // ponytail: filename matches are hints only; add symbol-aware ranking after an approved index exists.
            var candidates = indexed.Where(path => Path.GetFileName(path).Equals(name, StringComparison.OrdinalIgnoreCase))
                .Order(StringComparer.OrdinalIgnoreCase).Take(5).ToArray();
            return new(null, candidates.Length > 1 ? "경로 불일치 · 같은 이름의 후보 여러 개"
                : "대상 파일을 확인할 수 없음", candidates);
        }
        catch (Exception error) when (error is ArgumentException or UriFormatException or PathTooLongException)
        {
            return new(null, "지원하지 않는 링크 형식", []);
        }
    }

    internal static string? CanonicalPath(IEnumerable<string> indexed, string relative)
        => indexed.FirstOrDefault(path => path.Normalize(NormalizationForm.FormC)
            .Equals(relative.Normalize(NormalizationForm.FormC), StringComparison.OrdinalIgnoreCase));
}
