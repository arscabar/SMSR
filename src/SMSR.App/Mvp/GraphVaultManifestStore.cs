using System.IO;
using System.Text.Json;
namespace SMSR.App.Mvp;

public sealed partial class GraphVaultService
{
    private async Task<GraphVaultManifest> OwnedAsync(string projectId, string area, CancellationToken ct)
    {
        var raw = await store.GetGraphDerivedAsync(projectId, "vault-owned", GraphVaultPaths.Key(area), ct);
        var marker = GraphVaultPaths.Validate(Path.Combine(area, ".smsr-manifest.json"));
        if (File.Exists(marker) && (raw is null || new FileInfo(marker).Length > 32_000_000
            || await File.ReadAllTextAsync(marker, ct) != raw))
            throw new InvalidOperationException("보관함 소유 목록이 변경되었습니다. 사용자 수정은 덮어쓰지 않습니다.");
        var manifest = raw is null ? new(projectId, new Dictionary<string, GraphVaultEntry>())
            : JsonSerializer.Deserialize<GraphVaultManifest>(raw, GraphWorker.Json)
                ?? throw new InvalidOperationException("노트 소유 목록이 손상되었습니다.");
        if (manifest.ProjectId != projectId || manifest.Files.Count > 50001) throw new InvalidOperationException("노트 소유 프로젝트가 다릅니다.");
        foreach (var (name, entry) in manifest.Files)
        {
            GraphVaultPaths.Inside(area, name);
            if (entry.Hash.Length != 64 || entry.Hash.Any(c => !Uri.IsHexDigit(c))) throw new InvalidOperationException("노트 소유 지문이 올바르지 않습니다.");
        }
        return manifest;
    }
    private async Task SaveOwnedAsync(string projectId, string area, GraphVaultManifest manifest, CancellationToken ct)
    {
        await store.SaveGraphDerivedAsync(projectId, "vault-owned", GraphVaultPaths.Key(area),
            (await store.GetGraphInfoAsync(projectId, ct))!.Revision, JsonSerializer.Serialize(manifest, GraphWorker.Json), ct);
        // The on-disk manifest is informational; user-editable data is never ownership authority.
        var marker = GraphVaultPaths.Validate(Path.Combine(area, ".smsr-manifest.json"));
        var temp = GraphVaultPaths.Validate(Path.Combine(area, ".smsr-manifest-" + Guid.NewGuid().ToString("N") + ".tmp"));
        try
        {
            await File.WriteAllTextAsync(temp, JsonSerializer.Serialize(manifest, GraphWorker.Json), ct);
            File.Move(temp, marker, true);
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }
}
