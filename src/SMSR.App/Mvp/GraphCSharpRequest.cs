using System.Security.Cryptography;
using System.Text.Json;

namespace SMSR.App.Mvp;

internal sealed record GraphCSharpRequest(string ProjectId, string[] Paths,
    string LanguageVersion = "CSharp12", string[]? Defines = null)
{
    internal GraphCSharpRequest Normalize()
    {
        if (Paths is null || Paths.Length is < 1 or > 500 || Paths.Any(p =>
            string.IsNullOrWhiteSpace(p) || p.Length > 1024 || p.Any(char.IsControl) || p.Contains('\\') || p.Contains(':') ||
            !p.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) || p.Split('/').Any(x => x is "" or "." or "..")))
            throw new ArgumentException("색인된 C# 상대 경로를 1~500개 지정하세요.");
        if (Paths.Distinct(StringComparer.OrdinalIgnoreCase).Count() != Paths.Length)
            throw new ArgumentException("중복 경로를 제거하세요.");
        if (string.IsNullOrWhiteSpace(LanguageVersion) || LanguageVersion.Length > 32 ||
            Defines?.Length > 128 || Defines?.Any(d => string.IsNullOrWhiteSpace(d) || d.Length > 128) == true)
            throw new ArgumentException("C# 버전·조건부 심벌 입력이 올바르지 않습니다.");
        return this with { Paths = Paths.OrderBy(p => p, StringComparer.Ordinal).ToArray(),
            Defines = (Defines ?? []).Distinct(StringComparer.Ordinal).OrderBy(d => d, StringComparer.Ordinal).ToArray() };
    }
    internal string Key => Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(
        new { Paths, LanguageVersion, Defines }, GraphWorker.Json)));
}
