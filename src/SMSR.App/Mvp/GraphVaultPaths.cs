using System.IO;
using System.Security.Cryptography;
using System.Text;
namespace SMSR.App.Mvp;

internal static class GraphVaultPaths
{
    internal static string Key(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant()[..16];
    internal static string Area(string vault, string project) => Path.Combine(vault, "SMSR-generated", Key(project));
    internal static string Name(GraphNode node)
    {
        var title = Path.GetFileName(node.SourcePath);
        var safe = string.Concat(title.Select(c => char.IsLetterOrDigit(c) || c is '.' or '-' or '_' ? c : '-'));
        return (safe.Length > 64 ? safe[..64] : safe) + "--" + Key(node.NodeId) + ".md";
    }
    internal static string Validate(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || path.Length > 1024 || !Path.IsPathFullyQualified(path)
            || path.StartsWith("\\\\") || path.StartsWith("//")) throw new ArgumentException("로컬 보관함의 절대 경로를 선택하세요.");
        var full = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar);
        if (full.Length <= Path.GetPathRoot(full)!.Length) throw new ArgumentException("드라이브 전체를 보관함으로 연결할 수 없습니다.");
        for (var current = full; current is not null; current = Path.GetDirectoryName(current))
            if ((Directory.Exists(current) || File.Exists(current)) && File.GetAttributes(current).HasFlag(FileAttributes.ReparsePoint))
                throw new InvalidOperationException("연결된 폴더·파일은 보관함에 사용할 수 없습니다.");
        return full;
    }
    internal static string Inside(string area, string name)
    {
        if (name != Path.GetFileName(name) || !name.EndsWith(".md", StringComparison.Ordinal)
            || name.Contains(':') || name.Contains('\\') || name.Contains('/')) throw new InvalidOperationException("노트 소유 경로가 올바르지 않습니다.");
        return Validate(Path.Combine(area, name));
    }
}
