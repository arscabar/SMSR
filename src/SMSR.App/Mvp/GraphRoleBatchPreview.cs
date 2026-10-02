using System.IO;

namespace SMSR.App.Mvp;

public sealed partial class GraphRoleBatchService(EventStore store, GraphRoleService roles, GraphRoleJobs jobs)
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    public async Task<GraphRoleBatchPreview> PreviewAsync(string projectId, string[] folders, CancellationToken ct = default)
    {
        if (EventValidation.ValidateWorkflowIds(projectId, "roles") is { } error || folders is null
            || folders.Length > 200 || folders.Any(f => f is null))
            throw new ArgumentException("설명 범위가 올바르지 않습니다.");
        folders = folders.Select(f => f.Replace('\\', '/').TrimEnd('/')).Distinct(StringComparer.Ordinal).ToArray();
        if (folders.Any(f => string.IsNullOrWhiteSpace(f) || f.StartsWith('/') || f.Contains(':')
            || f.Split('/').Any(p => p is "." or ".." or ""))) throw new ArgumentException("프로젝트 상대 폴더만 선택하세요.");
        var info = await store.GetGraphInfoAsync(projectId, ct) ?? throw new KeyNotFoundException("색인이 없습니다.");
        var files = new List<GraphRoleBatchFile>();
        foreach (var (node, saved) in await store.GraphRoleFilesAsync(projectId, info.Revision, ct))
        {
            if (folders.Length > 0 && !folders.Any(f => node.SourcePath.StartsWith(f + "/", StringComparison.Ordinal))) continue;
            try
            {
                var context = await roles.ContextAsync(projectId, node.NodeId, ct);
                var current = saved && (await roles.ReadAsync(projectId, node.NodeId, ct)).Status == "CURRENT";
                files.Add(new(node.NodeId, node.SourcePath, current ? "CURRENT" : "READY", null, context.Fingerprint));
            }
            catch (Exception e) when (e is ArgumentException or InvalidOperationException or KeyNotFoundException or IOException)
            { files.Add(new(node.NodeId, node.SourcePath, "EXCLUDED", "현재 원문·관계 근거를 확인할 수 없음", null)); }
        }
        if ((await store.GetGraphInfoAsync(projectId, ct))?.Revision != info.Revision)
            throw new InvalidOperationException("색인이 변경되었습니다. 범위를 다시 확인하세요.");
        return new(info.Revision, folders, files.ToArray());
    }
}
