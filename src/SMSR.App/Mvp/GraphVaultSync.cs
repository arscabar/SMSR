using System.IO;
using System.Text;
namespace SMSR.App.Mvp;

public sealed partial class GraphVaultService
{
    public async Task<GraphVaultResult> SyncAsync(string projectId, CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            var binding = (await StatusAsync(projectId, ct)).Binding ?? throw new KeyNotFoundException("보관함을 먼저 연결하세요.");
            var vault = GraphVaultPaths.Validate(binding.VaultPath);
            if (!Directory.Exists(vault)) throw new InvalidOperationException("보관함 폴더가 없어 동기화하지 않았습니다.");
            var area = GraphVaultPaths.Validate(GraphVaultPaths.Area(vault, projectId));
            Directory.CreateDirectory(area);
            var revision = (await store.GetGraphInfoAsync(projectId, ct))!.Revision;
            var manifest = await OwnedAsync(projectId, area, ct); var notes = await NotesAsync(projectId, ct);
            var conflicts = new List<string>(); var written = 0; var unchanged = 0; var retired = 0;
            foreach (var (name, content) in notes)
            {
                ct.ThrowIfCancellationRequested();
                var result = GraphVaultWrite.Note(area, name, content.Text, manifest.Files.GetValueOrDefault(name));
                if (result < 0) { conflicts.Add(name); continue; }
                manifest.Files[name] = new(GraphVaultWrite.Hash(new UTF8Encoding(false).GetBytes(content.Text)), content.NodeId);
                if (result == 0) unchanged++; else written++;
                if (result > 0) await SaveOwnedAsync(projectId, area, manifest, ct);
            }
            foreach (var (name, entry) in manifest.Files.ToArray().Where(p => !notes.ContainsKey(p.Key)))
            {
                var text = $"# 색인에서 제외된 항목\n\n이 원문은 삭제·이동·범위 변경으로 현재 색인에 없습니다. 노트는 자동 삭제하지 않습니다.\n\n이전 항목: {GraphVaultNote.Text(entry.NodeId)}\n";
                var result = GraphVaultWrite.Note(area, name, text, entry);
                if (result < 0) { conflicts.Add(name); continue; }
                manifest.Files[name] = entry with { Hash = GraphVaultWrite.Hash(new UTF8Encoding(false).GetBytes(text)) }; retired++;
                if (result > 0) await SaveOwnedAsync(projectId, area, manifest, ct);
            }
            await SaveOwnedAsync(projectId, area, manifest, ct);
            if ((await store.GetGraphInfoAsync(projectId, ct))!.Revision != revision)
                throw new InvalidOperationException("동기화 중 색인이 변경되었습니다. 완료로 표시하지 않고 다시 확인합니다.");
            var resultInfo = new GraphVaultResult((await store.GetGraphInfoAsync(projectId, ct))!.Revision,
                written, unchanged, conflicts.ToArray(), retired);
            await SaveAsync(projectId, "vault-result", resultInfo, ct); await SaveAsync(projectId, "vault-error", "", ct);
            return resultInfo;
        }
        finally { _gate.Release(); }
    }
}
