using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace SMSR.App.Mvp;

internal sealed record GraphSourceFile(GraphFile File, string? Markdown,
    IReadOnlyList<GraphRouteDeclaration> Routes, IReadOnlyList<GraphExplicitReference> References);

internal static class GraphFileScanner
{
    private static readonly HashSet<string> Extensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".md", ".markdown", ".mdx", ".txt", ".cs", ".fs", ".py", ".js", ".jsx", ".ts", ".tsx",
        ".go", ".rs", ".java", ".kt", ".swift", ".c", ".h", ".cpp", ".hpp", ".rb", ".php",
        ".html", ".css", ".scss", ".sql", ".sh", ".ps1", ".xaml", ".xml", ".yaml", ".yml",
        ".toml", ".gradle", ".sln", ".slnx", ".csproj", ".json", ".scala", ".lua",
        ".png", ".jpg", ".jpeg", ".gif", ".webp", ".svg", ".bmp", ".avif",
        ".mp4", ".webm", ".ogv", ".mp3", ".wav", ".ogg", ".m4a", ".pdf", ".docx", ".xlsx"
    };
    private static readonly HashSet<string> HiddenDirectories = new(StringComparer.OrdinalIgnoreCase)
        { ".git", "bin", "obj", "node_modules", "dist", ".venv", ".cache", "graphify-out", "secrets" };

    public static async Task<IReadOnlyList<string>> FoldersAsync(string rootPath, CancellationToken ct)
    {
        var paths = await PathsAsync(rootPath, ct);
        return paths.Where(Allowed).Select(path => path.Replace('\\', '/').Split('/'))
            .Where(parts => parts.Length > 1).Select(parts => parts[0])
            .Distinct(StringComparer.Ordinal).OrderBy(name => name, StringComparer.Ordinal).ToArray();
    }

    public static async Task<IReadOnlyList<GraphSourceFile>> ScanAsync(string rootPath, CancellationToken ct,
        IReadOnlyList<string>? folders = null)
    {
        var root = Path.GetFullPath(rootPath).TrimEnd(Path.DirectorySeparatorChar);
        var paths = await PathsAsync(root, ct);
        if (folders is not null && folders.Count > 0)
        {
            var available = (await FoldersAsync(root, ct)).ToHashSet(StringComparer.Ordinal);
            if (folders.Count > 100 || folders.Any(folder => !available.Contains(folder)))
                throw new ArgumentException("프로젝트의 목록에서 색인할 폴더를 선택하세요.", nameof(folders));
            var selected = folders.ToHashSet(StringComparer.Ordinal);
            paths = paths.Where(path => selected.Contains(path.Replace('\\', '/').Split('/')[0])).ToArray();
        }
        paths = paths.Where(path => Allowed(path.Replace('\\', '/'))).ToArray();
        if (paths.Length > 50_000) throw new InvalidOperationException("색인 가능한 파일 50,000개 한도를 초과했습니다. 폴더를 선택하세요. 기존 색인은 유지됩니다.");
        var files = new List<GraphSourceFile>();
        var canonicalPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var name in paths)
        {
            ct.ThrowIfCancellationRequested();
            var relative = name.Replace('\\', '/');
            if (!Allowed(relative)) continue;
            if (!canonicalPaths.Add(relative.Normalize(NormalizationForm.FormC)))
                throw new InvalidOperationException("대소문자 또는 Unicode 정규화가 충돌하는 파일 경로가 있습니다. 기존 색인은 유지됩니다.");
            var absolute = Path.GetFullPath(Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar)));
            if (!absolute.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) || !File.Exists(absolute)) continue;
            var linkedParent = false;
            for (var parent = Path.GetDirectoryName(absolute); parent is not null && parent.Length > root.Length;
                 parent = Path.GetDirectoryName(parent))
                if (new DirectoryInfo(parent).Attributes.HasFlag(FileAttributes.ReparsePoint)) { linkedParent = true; break; }
            if (linkedParent) continue;
            var info = new FileInfo(absolute);
            var mediaKind = IsImage(relative) ? "image" : IsVideo(relative) ? "video" : IsAudio(relative) ? "audio"
                : GraphDocumentService.Binary(relative) ? "document" : null;
            if (info.Length > (mediaKind is null ? 2_000_000 : mediaKind is "image" or "document" ? 20_000_000 : 100_000_000)
                || info.Attributes.HasFlag(FileAttributes.ReparsePoint)) continue;
            if (mediaKind is not null)
            {
                await using var stream = File.OpenRead(absolute);
                var mediaHash = Convert.ToHexString(await SHA256.HashDataAsync(stream, ct));
                files.Add(new(new(relative, mediaHash, mediaKind), null, [], []));
                continue;
            }
            var bytes = await File.ReadAllBytesAsync(absolute, ct);
            if (bytes.Length > 2_000_000 || bytes.AsSpan(0, Math.Min(bytes.Length, 4096)).Contains((byte)0)) continue;
            var kind = IsMarkdown(relative) ? "document" : "code";
            var hash = Convert.ToHexString(SHA256.HashData(bytes));
            var markdown = kind == "document" ? Encoding.UTF8.GetString(bytes) : null;
            files.Add(new(new(relative, hash, kind), markdown,
                GraphRouteExtractor.Extract(relative, bytes), GraphExplicitReferenceExtractor.Extract(relative, bytes, markdown)));
        }
        return files;
    }

    private static async Task<string[]> PathsAsync(string root, CancellationToken ct)
    {
        if (!Directory.Exists(root)) throw new ArgumentException("색인할 폴더가 없습니다.", nameof(root));
        var gitRoot = (await GitAsync(root, ct, "rev-parse", "--show-toplevel")).Trim();
        if (!string.Equals(Path.GetFullPath(gitRoot).TrimEnd(Path.DirectorySeparatorChar), root, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Git 저장소의 최상위 폴더를 지정하세요.", nameof(root));
        var listing = await GitAsync(root, ct, "ls-files", "--cached", "--others", "--exclude-standard", "-z");
        return listing.Split('\0', StringSplitOptions.RemoveEmptyEntries).Distinct(StringComparer.Ordinal).ToArray();
    }

    private static bool IsMarkdown(string path) => Path.GetExtension(path).ToLowerInvariant() is ".md" or ".markdown" or ".mdx";
    private static bool IsImage(string path) => Path.GetExtension(path).ToLowerInvariant() is ".png" or ".jpg" or ".jpeg" or ".gif" or ".webp" or ".svg" or ".bmp" or ".avif";
    private static bool IsVideo(string path) => Path.GetExtension(path).ToLowerInvariant() is ".mp4" or ".webm" or ".ogv";
    private static bool IsAudio(string path) => Path.GetExtension(path).ToLowerInvariant() is ".mp3" or ".wav" or ".ogg" or ".m4a";

    internal static bool Allowed(string path)
    {
        if (path.Length > 1024) return false;
        var parts = path.Split('/');
        if (parts.Any(HiddenDirectories.Contains)
            || !(Extensions.Contains(Path.GetExtension(path)) || GraphCodeFormats.All.Contains(Path.GetExtension(path)))) return false;
        return !GraphFilePolicy.Sensitive(path);
    }

    private static async Task<string> GitAsync(string root, CancellationToken ct, params string[] arguments)
    {
        var start = new ProcessStartInfo("git") { RedirectStandardOutput = true, RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8, StandardErrorEncoding = Encoding.UTF8,
            UseShellExecute = false, CreateNoWindow = true };
        start.ArgumentList.Add("-C"); start.ArgumentList.Add(root);
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Git을 실행할 수 없습니다.");
        var output = process.StandardOutput.ReadToEndAsync(ct);
        var error = process.StandardError.ReadToEndAsync(ct);
        await process.WaitForExitAsync(ct);
        if (process.ExitCode != 0) throw new InvalidOperationException("Git 저장소 파일 목록을 읽지 못했습니다: " + await error);
        return await output;
    }
}
