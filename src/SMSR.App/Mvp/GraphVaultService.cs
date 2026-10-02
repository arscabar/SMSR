using System.IO;
using System.Text.Json;
namespace SMSR.App.Mvp;

public sealed partial class GraphVaultService(EventStore store, GraphRoleService roles)
{
    // ponytail: process-wide vault lock; use per-vault locks if concurrent sync throughput requires it.
    private static readonly SemaphoreSlim _gate = new(1, 1);
    internal Func<string> SourceAddress { get; set; } = () => $"http://127.0.0.1:{LocalServer.Port}";
    public async Task<GraphVaultStatus> StatusAsync(string projectId, CancellationToken ct = default)
    {
        if (EventValidation.ValidateWorkflowIds(projectId, "vault") is { } error) throw new ArgumentException(error);
        var raw = await store.GetGraphDerivedAsync(projectId, "vault-binding", "", ct);
        var result = await store.GetGraphDerivedAsync(projectId, "vault-result", "", ct);
        var failure = await store.GetGraphDerivedAsync(projectId, "vault-error", "", ct);
        return new(raw is null ? null : JsonSerializer.Deserialize<GraphVaultBinding>(raw, GraphWorker.Json),
            result is null ? null : JsonSerializer.Deserialize<GraphVaultResult>(result, GraphWorker.Json),
            failure is null ? null : JsonSerializer.Deserialize<string>(failure, GraphWorker.Json));
    }
    public async Task<GraphVaultStatus> ConnectAsync(GraphVaultConnect request, CancellationToken ct = default)
    {
        if (!request.Confirm) throw new ArgumentException("생성 영역 쓰기와 사용자 노트 보호 확인이 필요합니다.");
        await _gate.WaitAsync(ct);
        try
        {
            var info = await store.GetGraphInfoAsync(request.ProjectId, ct) ?? throw new KeyNotFoundException("색인이 없습니다.");
            var path = GraphVaultPaths.Validate(request.VaultPath);
            var root = Path.GetFullPath(info.RootPath).TrimEnd(Path.DirectorySeparatorChar);
            if (path.Equals(root, StringComparison.OrdinalIgnoreCase) || path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("색인 저장소 밖에 보관함을 연결하세요.");
            if (!Directory.Exists(path))
            {
                if (!request.Create || File.Exists(path)) throw new ArgumentException("보관함 폴더가 없습니다.");
                Directory.CreateDirectory(path);
            }
            await SaveAsync(request.ProjectId, "vault-binding", new GraphVaultBinding(path, request.AutoSync), ct);
            return await StatusAsync(request.ProjectId, ct);
        }
        finally { _gate.Release(); }
    }
    private async Task SaveAsync<T>(string projectId, string kind, T value, CancellationToken ct)
        => await store.SaveGraphDerivedAsync(projectId, kind, "", (await store.GetGraphInfoAsync(projectId, ct))!.Revision,
            JsonSerializer.Serialize(value, GraphWorker.Json), ct);
}
