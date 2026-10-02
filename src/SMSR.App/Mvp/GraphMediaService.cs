using System.IO;
using System.Security.Cryptography;

namespace SMSR.App.Mvp;

internal sealed class GraphMediaService(EventStore store)
{
    internal async Task<(FileStream Stream, string Mime)> OpenAsync(string projectId, string path, CancellationToken ct)
    {
        if (EventValidation.ValidateWorkflowIds(projectId, "graph") is { } error)
            throw new ArgumentException(error, nameof(projectId));
        if (string.IsNullOrWhiteSpace(path) || path.Length > 1024 || path.Contains('\\') || path.Any(char.IsControl)
            || !GraphMediaTypes.TryGet(path, out var type))
            throw new ArgumentException("미리보기 형식 또는 경로가 올바르지 않습니다.", nameof(path));
        if (GraphFilePolicy.Sensitive(path)) throw new KeyNotFoundException("비밀 파일은 열 수 없습니다.");
        var info = await store.GetGraphInfoAsync(projectId, ct)
            ?? throw new KeyNotFoundException("관계 색인이 없습니다.");
        var node = await store.GetGraphNodeAsync(projectId, "file:" + path, ct, info.Revision);
        if (node is null || node.Kind != type.Kind || node.SourcePath != path)
            throw new KeyNotFoundException("색인되지 않은 미디어입니다.");
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
        if (file.Attributes.HasFlag(FileAttributes.ReparsePoint) || file.Length > (type.Kind == "image" ? 20_000_000 : 100_000_000))
            throw new InvalidOperationException("미디어 크기 또는 연결 파일 제한으로 열 수 없습니다.");
        var stream = new FileStream(absolute, FileMode.Open, FileAccess.Read, FileShare.Read, 81920,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        try
        {
            // ponytail: full hash per request preserves index integrity; cache only if range traffic measures poorly.
            var hash = Convert.ToHexString(await SHA256.HashDataAsync(stream, ct));
            if (!hash.Equals(node.Hash, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("색인 이후 파일이 변경되었습니다. 색인을 갱신하세요.");
            stream.Position = 0;
            return (stream, type.Mime);
        }
        catch { await stream.DisposeAsync(); throw; }
    }
}
