using System.IO;
namespace SMSR.App.Mvp;

public sealed partial class GraphVaultService
{
    public async Task<string> NoteUriAsync(string projectId, string nodeId, CancellationToken ct = default)
    {
        var binding = (await StatusAsync(projectId, ct)).Binding ?? throw new KeyNotFoundException("보관함 연결이 필요합니다.");
        var node = await store.GetGraphNodeAsync(projectId, nodeId, ct) ?? throw new KeyNotFoundException("항목이 없습니다.");
        node = await store.GetGraphNodeAsync(projectId, "file:" + node.SourcePath, ct) ?? node;
        var area = GraphVaultPaths.Validate(GraphVaultPaths.Area(binding.VaultPath, projectId));
        var manifest = await OwnedAsync(projectId, area, ct); var name = GraphVaultPaths.Name(node);
        if (!manifest.Files.TryGetValue(name, out var owned) || owned.NodeId != node.NodeId)
            throw new KeyNotFoundException("대응 노트가 없습니다. 보관함을 동기화하세요.");
        var path = GraphVaultPaths.Inside(area, name);
        if (!File.Exists(path)) throw new KeyNotFoundException("노트가 이동·삭제되었습니다. 동기화하세요.");
        return "obsidian://open?path=" + Uri.EscapeDataString(path);
    }
}
