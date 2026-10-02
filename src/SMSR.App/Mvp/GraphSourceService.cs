using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace SMSR.App.Mvp;

internal sealed record GraphSourcePreview(string Path, int SelectedLine, int FirstLine,
    IReadOnlyList<string> Lines, int Revision);

internal sealed class GraphSourceService(EventStore store)
{
    public async Task<GraphSourcePreview> GetAsync(string projectId, string path, int line,
        CancellationToken ct = default)
    {
        if (line is < 1 or > 1_000_000) throw new ArgumentException("줄 번호가 올바르지 않습니다.");
        var source = await ReadAsync(projectId, path, ct);
        var lines = source.Text.Split('\n');
        var selected = Math.Min(line, lines.Length);
        var first = Math.Max(1, selected - 20);
        return new(path, selected, first, lines.Skip(first - 1).Take(41).Select(value => value.TrimEnd('\r')).ToArray(), source.Revision);
    }

    internal async Task<(string Text, string Hash, int Revision)> ReadAsync(string projectId, string path, CancellationToken ct)
    {
        if (EventValidation.ValidateWorkflowIds(projectId, "graph") is { } error)
            throw new ArgumentException(error, nameof(projectId));
        if (string.IsNullOrWhiteSpace(path) || path.Length > 1024)
            throw new ArgumentException("파일 경로 또는 줄 번호가 올바르지 않습니다.");
        if (GraphFilePolicy.Sensitive(path)) throw new KeyNotFoundException("비밀 파일은 열 수 없습니다.");
        var info = await store.GetGraphInfoAsync(projectId, ct)
            ?? throw new KeyNotFoundException("이 프로젝트의 관계 색인이 없습니다.");
        var files = await store.GetGraphFilesAsync(projectId, ct, info.Revision);
        if (!files.TryGetValue(path, out var indexedHash))
            throw new KeyNotFoundException("색인되지 않은 파일은 열 수 없습니다.");
        var root = Path.GetFullPath(info.RootPath).TrimEnd(Path.DirectorySeparatorChar);
        var absolute = Path.GetFullPath(Path.Combine(root, path.Replace('/', Path.DirectorySeparatorChar)));
        if (!absolute.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("저장소 밖의 파일은 열 수 없습니다.", nameof(path));
        for (var parent = Path.GetDirectoryName(absolute); parent is not null && parent.Length > root.Length;
             parent = Path.GetDirectoryName(parent))
            if (new DirectoryInfo(parent).Attributes.HasFlag(FileAttributes.ReparsePoint))
                throw new InvalidOperationException("연결된 폴더의 파일은 열 수 없습니다.");
        var file = new FileInfo(absolute);
        if (!file.Exists) throw new KeyNotFoundException("원본 파일이 없습니다. 색인을 갱신하세요.");
        if (file.Length > 2_000_000 || file.Attributes.HasFlag(FileAttributes.ReparsePoint))
            throw new InvalidOperationException("파일 크기 또는 연결 파일 제한으로 열 수 없습니다.");
        var bytes = await File.ReadAllBytesAsync(absolute, ct);
        if (bytes.Length > 2_000_000 || bytes.AsSpan(0, Math.Min(bytes.Length, 4096)).Contains((byte)0))
            throw new InvalidOperationException("큰 파일 또는 바이너리 파일은 열 수 없습니다.");
        if (!Convert.ToHexString(SHA256.HashData(bytes)).Equals(indexedHash, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("색인 이후 파일이 변경되었습니다. 색인을 갱신하세요.");
        return (GraphCodeIndexInput.Supported(path) ? GraphCodeText.Decode(bytes) : Encoding.UTF8.GetString(bytes), indexedHash, info.Revision);
    }
}
