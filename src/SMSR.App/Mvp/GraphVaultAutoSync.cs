namespace SMSR.App.Mvp;

public sealed partial class GraphVaultService
{
    internal async Task DirtyAsync(string projectId, CancellationToken ct)
    {
        if ((await StatusAsync(projectId, ct)).Binding is null) return;
        await _gate.WaitAsync(ct);
        try { await SaveAsync(projectId, "vault-dirty", Guid.NewGuid().ToString("N"), ct); }
        finally { _gate.Release(); }
    }
    internal async Task AutoSyncAsync(string projectId, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(SourceAddress())) return; // Binding is not ready; keep the dirty ticket for the next pass.
        if ((await StatusAsync(projectId, ct)).Binding?.AutoSync != true) return;
        var ticket = await store.GetGraphDerivedAsync(projectId, "vault-dirty", "", ct);
        if (ticket is null || ticket == "\"\"" || ticket == await store.GetGraphDerivedAsync(projectId, "vault-auto-failed", "", ct)) return;
        try
        {
            await SyncAsync(projectId, ct);
            await _gate.WaitAsync(ct);
            try
            {
                if (ticket == await store.GetGraphDerivedAsync(projectId, "vault-dirty", "", ct))
                    await SaveAsync(projectId, "vault-dirty", "", ct);
            }
            finally { _gate.Release(); }
        }
        catch (Exception e) when (e is ArgumentException or InvalidOperationException or KeyNotFoundException or System.IO.IOException
            or UnauthorizedAccessException or System.Text.Json.JsonException)
        {
            await SaveAsync(projectId, "vault-error", "자동 동기화 실패: 보관함 경로·수정 충돌·권한·색인을 확인한 후 다시 동기화하세요.", ct);
            await store.SaveGraphDerivedAsync(projectId, "vault-auto-failed", "", (await store.GetGraphInfoAsync(projectId, ct))!.Revision, ticket, ct);
        }
    }
}
