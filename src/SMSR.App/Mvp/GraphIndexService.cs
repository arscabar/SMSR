using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace SMSR.App.Mvp;

public sealed class GraphIndexService(EventStore store, GraphRoleJobs? roleJobs = null)
{
    private readonly SemaphoreSlim _gate = new(1, 1);

    public async Task<GraphFreshness> CheckFreshnessAsync(string projectId, CancellationToken ct = default)
    {
        if (EventValidation.ValidateWorkflowIds(projectId, "graph-index") is { } error)
            throw new ArgumentException(error, nameof(projectId));
        await _gate.WaitAsync(ct);
        try
        {
            var info = await store.GetGraphInfoAsync(projectId, ct)
                ?? throw new KeyNotFoundException("이 프로젝트의 관계 색인이 없습니다.");
            var old = await store.GetGraphFilesAsync(projectId, ct);
            var formatCurrent = await store.IsGraphFormatCurrentAsync(projectId, ct);
            // ponytail: explicit full hash scan is exact; add a metadata cache only if measured repositories need it.
            var current = (await GraphFileScanner.ScanAsync(info.RootPath, ct,
                    await store.GetGraphScopeAsync(projectId, ct)))
                .ToDictionary(item => item.File.Path, item => item.File.Hash, StringComparer.Ordinal);
            var previousPaths = old.Keys.ToHashSet(StringComparer.Ordinal);
            var added = current.Keys.Where(path => !previousPaths.Contains(path)).ToArray();
            var changed = current.Keys.Where(path => previousPaths.Contains(path) && old.TryGetValue(path, out var hash)
                && (!formatCurrent || hash != current[path])).ToArray();
            var removed = old.Keys.Where(path => !current.ContainsKey(path)).ToArray();
            var paths = added.Concat(changed).Concat(removed).Take(100).ToArray();
            var count = added.Length + changed.Length + removed.Length;
            return new(info.Revision, count > 0, added.Length, changed.Length, removed.Length, paths, count > paths.Length);
        }
        finally { _gate.Release(); }
    }

    public async Task<GraphIndexResult> IndexAsync(string projectId, string rootPath,
        bool allowLargeReduction = false, CancellationToken ct = default)
        => await IndexAsync(projectId, rootPath, null, allowLargeReduction, ct);

    public async Task<GraphIndexResult> IndexAsync(string projectId, string rootPath,
        IReadOnlyList<string>? folders, bool allowLargeReduction = false, CancellationToken ct = default,bool preserveCurrentScope=false)
    {
        if (EventValidation.ValidateWorkflowIds(projectId, "graph-index") is { } error)
            throw new ArgumentException(error, nameof(projectId));
        if (string.IsNullOrWhiteSpace(rootPath) || rootPath.Length > 1024)
            throw new ArgumentException("rootPath는 1~1,024자여야 합니다.", nameof(rootPath));
        await _gate.WaitAsync(ct);
        try
        {
            var root = Path.GetFullPath(rootPath).TrimEnd(Path.DirectorySeparatorChar);
            var old = await store.GetGraphFilesAsync(projectId, ct);
            var previous = await store.GetGraphInfoAsync(projectId, ct);
            var formatCurrent = await store.IsGraphFormatCurrentAsync(projectId, ct);
            if (previous is not null && !string.Equals(previous.RootPath, root, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("이 projectId는 다른 저장소에 연결돼 있습니다.");
            folders=preserveCurrentScope?await store.GetGraphScopeAsync(projectId,ct):folders??[];
            var sources = await GraphFileScanner.ScanAsync(root, ct, folders);
            if (sources.Count == 0) throw new InvalidOperationException("색인 가능한 파일이 없습니다. 기존 색인은 유지됩니다.");
            var files = sources.Select(item => item.File).ToArray();
            var indexed = files.Select(item => item.Path).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var previousPaths = old.Keys.ToHashSet(StringComparer.Ordinal);
            var currentPaths = files.Select(item => item.Path).ToHashSet(StringComparer.Ordinal);
            var changed = files.Where(item => !formatCurrent || !previousPaths.Contains(item.Path) || !old.TryGetValue(item.Path, out var hash) || hash != item.Hash)
                .Select(item => item.Path).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var removed = old.Keys.Where(path => !currentPaths.Contains(path)).ToArray();
            var membershipChanged = removed.Length > 0 || files.Any(item => !old.ContainsKey(item.Path));
            var reparsed = sources.Where(item => item.Markdown is not null && (membershipChanged || changed.Contains(item.File.Path)))
                .Select(item => item.File.Path).ToArray();
            var codeChanged = !formatCurrent || removed.Any(GraphCodeIndexInput.AffectsCode)
                || sources.Any(item => changed.Contains(item.File.Path) && GraphCodeIndexInput.AffectsCode(item.File.Path));
            var settingsChanged = removed.Any(GraphCodeIndexInput.IsConfiguration)
                || sources.Any(item => changed.Contains(item.File.Path) && GraphCodeIndexInput.IsConfiguration(item.File.Path));
            var code = codeChanged ? await GraphCodeIndex.ExtractAsync(root, sources, ct, old,
                !formatCurrent ? "색인 형식·분석 버전 변경" : settingsChanged ? "분석 설정·별칭·프로젝트 입력 변경" : null) : null;
            var reparsedCode = code?.ReparsedPaths ?? [];
            var reparsedProjects = sources.Where(item => item.File.Path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)
                || item.File.Path.EndsWith(".slnx", StringComparison.OrdinalIgnoreCase))
                .Where(item => settingsChanged || changed.Contains(item.File.Path)
                    || reparsedCode.Contains(item.File.Path,StringComparer.OrdinalIgnoreCase)).Select(item => item.File.Path).ToArray();
            var reprocessed = reparsed.Concat(reparsedCode).Concat(reparsedProjects).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            var nodes = files.Where(item => changed.Contains(item.Path))
                .Select(item => new GraphNode("file:" + item.Path, item.Path, item.Kind,
                    Path.GetFileName(item.Path), item.Path, 1, item.Hash)).ToList();
            var edges = new List<GraphEdge>();
            foreach (var source in sources.Where(item => changed.Contains(item.File.Path)
                || reprocessed.Contains(item.File.Path, StringComparer.OrdinalIgnoreCase)))
            {
                var occurrences = new Dictionary<string, int>(StringComparer.Ordinal);
                foreach (var route in source.Routes)
                {
                    var key = route.Kind + ":" + route.Method + ":" + route.Value;
                    occurrences.TryGetValue(key, out var count);
                    occurrences[key] = ++count;
                    var nodeId = "route:" + source.File.Path + ":"
                        + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key)))[..16] + ":" + count;
                    nodes.Add(new(nodeId, source.File.Path, route.Kind, route.Method + " " + route.Value,
                        source.File.Path, route.Line, source.File.Hash));
                    edges.Add(new("file:" + source.File.Path, nodeId, "DECLARES", source.File.Path,
                        route.Line, "RESOLVED", "EXTRACTED"));
                }
            }
            var issues = new List<GraphIssue>();
            foreach (var source in sources.Where(item => reparsed.Contains(item.File.Path, StringComparer.OrdinalIgnoreCase)))
            {
                var extracted = GraphMarkdown.Extract(root, source, indexed);
                nodes.AddRange(extracted.Nodes);
                edges.AddRange(extracted.Edges);
                issues.AddRange(extracted.Issues);
            }
            if (code is not null)
            {
                nodes.AddRange(code.Nodes);
                edges.AddRange(code.Edges);
                issues.AddRange(code.Issues);
            }
            foreach (var source in sources.Where(item => reparsedProjects.Contains(item.File.Path, StringComparer.OrdinalIgnoreCase)))
                foreach (var reference in source.References)
                {
                    var target = GraphLinkResolver.Resolve(root, source.File.Path,
                        reference.RawPath.Replace('\\', '/'), indexed).Path;
                    if (target is not null)
                        edges.Add(new("file:" + source.File.Path, "file:" + target, "PROJECT_REFERENCE",
                            source.File.Path, reference.Line, "RESOLVED", "EXTRACTED"));
                }
            var scan = new GraphScan(root, files, nodes, edges, issues, changed.ToArray(), reprocessed, removed,
                previous?.Revision ?? 0, folders, code?.Hyperedges);
            scan = scan with { Edges = GraphStructuralEvidence.Attach(edges, files) };
            var result = await store.ApplyGraphScanAsync(projectId, scan, allowLargeReduction, ct);
            result = result with { CodeAnalysis = code?.Analysis ?? new("UNCHANGED", 0,
                sources.Count(item => GraphCodeIndexInput.Supported(item.File.Path)), null, false) };
            var deepRevision = await store.RefreshGraphDeepAsync(projectId, result.Revision, ct);
            if (await store.GetGraphInfoAsync(projectId, ct) is { } refreshed)
                result = result with { Revision = deepRevision, NodeCount = refreshed.NodeCount,
                    EdgeCount = refreshed.EdgeCount, Unchanged = result.Unchanged && deepRevision == result.Revision };
            // Retry cross-repository refresh even when the local scan is unchanged after a prior failure.
            try { await new GraphCrossRepoService(store).RefreshAsync(ct); }
            catch (Exception cause) when (cause is not OperationCanceledException)
            { throw new InvalidOperationException("프로젝트 색인은 저장됐지만 저장소 간 연결 갱신에 실패했습니다: " + cause.Message, cause); }
            if (roleJobs is not null) await roleJobs.RefreshAsync(projectId, ct);
            await new GraphVaultService(store, new GraphRoleService(store)).DirtyAsync(projectId, ct);
            return result;
        }
        finally { _gate.Release(); }
    }

}
