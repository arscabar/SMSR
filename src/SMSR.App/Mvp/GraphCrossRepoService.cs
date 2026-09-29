using System.IO;
using System.Text;

namespace SMSR.App.Mvp;

internal sealed class GraphCrossRepoService(EventStore store)
{
    public async Task RefreshAsync(CancellationToken ct = default)
    {
        var projects = await store.GetGraphProjectsAsync(ct);
        if (projects.Count > 100) throw new InvalidOperationException("저장소 간 연결은 색인 100개 이하에서 지원됩니다.");
        var indexed = new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.Ordinal);
        foreach (var project in projects)
            indexed[project.ProjectId] = await store.GetGraphFilesAsync(project.ProjectId, ct, project.Revision);
        var edges = new List<GraphCrossRepoEdge>();
        foreach (var source in projects)
        {
            if (projects.Count < 2) break;
            var files = await GraphFileScanner.ScanAsync(source.RootPath, ct);
            foreach (var file in files)
            {
                if (!indexed[source.ProjectId].TryGetValue(file.File.Path, out var hash) || hash != file.File.Hash) continue;
                foreach (var reference in file.References)
                {
                    var resolved = Resolve(source, file.File.Path, reference.RawPath, projects, indexed);
                    if (resolved is null) continue;
                    edges.Add(new(source.ProjectId, "file:" + file.File.Path, resolved.Value.ProjectId,
                        "file:" + resolved.Value.Path, file.File.Path, reference.Line));
                    if (edges.Count > 20000) throw new InvalidOperationException("저장소 간 명시적 연결은 20,000개 이하로 제한됩니다.");
                }
            }
        }
        await store.ReplaceCrossRepoEdgesAsync(edges, projects.ToDictionary(p => p.ProjectId, p => p.Revision), ct);
    }

    private static (string ProjectId, string Path)? Resolve((string ProjectId, string RootPath, int Revision) source,
        string sourcePath, string raw, IReadOnlyList<(string ProjectId, string RootPath, int Revision)> projects,
        Dictionary<string, IReadOnlyDictionary<string, string>> indexed)
    {
        var value = raw.Trim();
        if (value.StartsWith('<'))
        {
            var close = value.IndexOf('>');
            if (close < 0) return null;
            value = value[1..close];
        }
        if (value.Length == 0 || value.StartsWith('#') || value.StartsWith("//")) return null;
        if (Uri.TryCreate(value, UriKind.Absolute, out var uri) && !uri.IsFile) return null;
        var anchor = value.IndexOfAny(['#', '?']);
        if (anchor >= 0) value = value[..anchor];
        if (value.Length == 0) return null;
        try
        {
            value = Uri.UnescapeDataString(value).Normalize(NormalizationForm.FormC);
            var full = Path.GetFullPath(Path.IsPathFullyQualified(value) ? value
                : Path.Combine(source.RootPath, Path.GetDirectoryName(sourcePath) ?? "", value.Replace('/', Path.DirectorySeparatorChar)));
            foreach (var target in projects.OrderByDescending(item => item.RootPath.Length))
            {
                if (target.ProjectId == source.ProjectId) continue;
                var root = Path.GetFullPath(target.RootPath).TrimEnd(Path.DirectorySeparatorChar);
                if (!full.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) continue;
                var relative = Path.GetRelativePath(root, full).Replace('\\', '/').Normalize(NormalizationForm.FormC);
                var canonical = GraphLinkResolver.CanonicalPath(indexed[target.ProjectId].Keys, relative);
                if (canonical is not null) return (target.ProjectId, canonical);
            }
        }
        catch (Exception error) when (error is ArgumentException or UriFormatException or PathTooLongException) { }
        return null;
    }
}
