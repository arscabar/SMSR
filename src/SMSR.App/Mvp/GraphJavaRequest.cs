using System.Security.Cryptography;
using System.Text.Json;

namespace SMSR.App.Mvp;

internal sealed record GraphJavaRequest(string ProjectId, string[] Paths)
{
    internal GraphJavaRequest Normalize()
    {
        if(Paths is null || Paths.Length is <1 or >500 || Paths.Any(p=>
            string.IsNullOrWhiteSpace(p) || p.Length>1024 || p.Any(char.IsControl) || p.Contains('\\') || p.Contains(':') ||
            !p.EndsWith(".java",StringComparison.Ordinal) || p.Split('/').Any(x=>x is "" or "." or "..")) ||
            Paths.Distinct(StringComparer.OrdinalIgnoreCase).Count()!=Paths.Length)
            throw new ArgumentException("중복 없는 색인 Java 상대 경로를 1~500개 지정하세요.");
        return this with { Paths=Paths.OrderBy(p=>p,StringComparer.Ordinal).ToArray() };
    }
    internal string Key=>Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(Paths)));
}
