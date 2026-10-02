using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace SMSR.App.Mvp;

internal static class GraphCodeIndexInput
{
    internal static readonly HashSet<string> Extensions = GraphCodeFormats.All;

    internal static bool Supported(string path) => Extensions.Contains(Path.GetExtension(path))
        && !GraphCodeFormats.FileOnly.Contains(Path.GetExtension(path));
    internal static bool IsConfiguration(string path) => Path.GetExtension(path).ToLowerInvariant()
        is ".json" or ".xml" or ".csproj" or ".fsproj" or ".vbproj" or ".sln" or ".slnx" or ".gradle";
    internal static bool AffectsCode(string path) => Extensions.Contains(Path.GetExtension(path)) || IsConfiguration(path);

    internal static async Task<object[]> ReadAsync(string root, IReadOnlyList<GraphSourceFile> sources,
        CancellationToken ct)
    {
        var files = new List<object>();
        long size = 0;
        foreach (var source in sources.Where(item => AffectsCode(item.File.Path)))
        {
            ct.ThrowIfCancellationRequested();
            var path = Path.GetFullPath(Path.Combine(root, source.File.Path.Replace('/', Path.DirectorySeparatorChar)));
            if (!path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("색인 범위 밖 코드 경로입니다.");
            for (var directory = new DirectoryInfo(Path.GetDirectoryName(path)!); directory.FullName.Length > root.Length;
                directory = directory.Parent!)
                if (directory.Attributes.HasFlag(FileAttributes.ReparsePoint))
                    throw new InvalidOperationException("색인 도중 연결 폴더로 변경됐습니다.");
            var info = new FileInfo(path);
            if (info.Length > 2_000_000 || info.Attributes.HasFlag(FileAttributes.ReparsePoint))
                throw new InvalidOperationException("색인 도중 코드 크기 또는 연결 파일이 변경됐습니다.");
            var bytes = await File.ReadAllBytesAsync(path, ct);
            if (bytes.Length > 2_000_000 || Convert.ToHexString(SHA256.HashData(bytes)) != source.File.Hash)
                throw new InvalidOperationException("색인 중 코드가 변경됐습니다. 기존 색인은 유지됩니다.");
            size += bytes.Length;
            if (size > 28 * 1024 * 1024)
                throw new InvalidOperationException("코드 관계 분석 범위가 28 MiB를 초과했습니다. 색인 폴더를 줄이세요.");
            var text = GraphCodeText.Decode(bytes);
            files.Add(new { path = source.File.Path, text, hash = source.File.Hash,
                contentHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text))) });
        }
        return files.ToArray();
    }
}
