using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace SMSR.App.Mvp;

internal static class GraphCSharpFileRelations
{
    internal static async Task<IReadOnlyList<GraphEdge>> ExtractAsync(string root,
        IReadOnlyList<GraphSourceFile> sources, CancellationToken ct)
    {
        var code = sources.Where(source => source.File.Path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)).ToArray();
        if (code.Length == 0) return [];
        var projects = sources.Where(source => source.File.Path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
            .Select(source => Path.GetDirectoryName(source.File.Path.Replace('/', Path.DirectorySeparatorChar)) ?? "")
            .Distinct(StringComparer.OrdinalIgnoreCase).OrderByDescending(path => path.Length).ToArray();
        var groups = code.GroupBy(source => projects.FirstOrDefault(project => source.File.Path.StartsWith(
            project.Replace(Path.DirectorySeparatorChar, '/') + "/", StringComparison.OrdinalIgnoreCase)) ?? "");
        var worker = new GraphWorker();
        var edges = new List<GraphEdge>();
        foreach (var group in groups)
        {
            var members = group.ToArray();
            if (members.Length > 1000) throw new InvalidOperationException("C# 프로젝트당 1,000개를 넘는 파일은 정적 호출 색인을 지원하지 않습니다.");
            var files = new List<object>();
            long bytes = 0;
            foreach (var source in members)
            {
                ct.ThrowIfCancellationRequested();
                var path = Path.Combine(root, source.File.Path.Replace('/', Path.DirectorySeparatorChar));
                var content = await File.ReadAllBytesAsync(path, ct);
                if (Convert.ToHexString(SHA256.HashData(content)) != source.File.Hash)
                    throw new InvalidOperationException("색인 중 코드가 변경됐습니다. 다시 시도하세요.");
                bytes += content.Length;
                if (bytes > 16 * 1024 * 1024)
                    throw new InvalidOperationException("C# 프로젝트 소스가 16 MiB를 넘어 정적 호출 색인을 지원하지 않습니다.");
                files.Add(new { path = source.File.Path, text = Encoding.UTF8.GetString(content) });
            }
            var result = await worker.RunCSharpIndexAsync(new { files, languageVersion = "CSharp12" }, ct);
            foreach (var call in result.EnumerateArray())
            {
                var source = call.GetProperty("source").GetString()!;
                var target = call.GetProperty("target").GetString()!;
                edges.Add(new("file:" + source, "file:" + target, "CALLS", source,
                    call.GetProperty("line").GetInt32(), "STATIC_TARGET_ONLY", "EXTRACTED"));
            }
        }
        return edges;
    }
}
