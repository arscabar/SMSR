using System.IO;

namespace SMSR.App.Mvp;

internal sealed record GraphIndexOptions(string RootPath, IReadOnlyList<string> Folders,
    IReadOnlyList<string> SelectedFolders, bool Indexed);

internal static class GraphIndexOptionsService
{
    public static async Task<GraphIndexOptions?> GetAsync(EventStore store, string projectId, CancellationToken ct)
    {
        if (EventValidation.ValidateWorkflowIds(projectId, "graph-index") is { } error)
            throw new ArgumentException(error, nameof(projectId));
        var existing = await store.GetGraphInfoAsync(projectId, ct);
        if (existing is not null)
            return new(existing.RootPath, await GraphFileScanner.FoldersAsync(existing.RootPath, ct),
                await store.GetGraphScopeAsync(projectId, ct), true);
        if (!string.Equals(Path.GetFileName(projectId), projectId, StringComparison.Ordinal)
            || projectId is "." or "..") return null;
        var found = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var start in new[] { AppContext.BaseDirectory, Environment.CurrentDirectory })
        {
            for (var directory = new DirectoryInfo(Path.GetFullPath(start)); directory is not null;
                 directory = directory.Parent)
            {
                var candidate = string.Equals(directory.Name, projectId, StringComparison.OrdinalIgnoreCase)
                    ? directory.FullName : Path.Combine(directory.FullName, projectId);
                if (!Directory.Exists(candidate) || found.ContainsKey(candidate)) continue;
                try { found.Add(candidate, await GraphFileScanner.FoldersAsync(candidate, ct)); }
                catch (Exception cause) when (cause is ArgumentException or InvalidOperationException or IOException)
                { /* A same-named folder is not necessarily a Git project. */ }
            }
        }
        return found.Count == 1 ? new(found.Single().Key, found.Single().Value, [], false) : null;
    }
}
