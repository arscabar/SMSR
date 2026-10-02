using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
namespace SMSR.App.Mvp;

internal static class GraphVaultEndpoints
{
    internal static void Map(WebApplication app)
    {
        app.MapGet("/api/graph/vault", (string projectId, GraphVaultService service, CancellationToken ct)
            => GraphExplorerEndpoints.Reply(() => service.StatusAsync(projectId, ct)));
        app.MapGet("/api/graph/vault-note", (string projectId, string nodeId, GraphVaultService service, CancellationToken ct)
            => Reply(async () => new { uri = await service.NoteUriAsync(projectId, nodeId, ct) }));
        app.MapPost("/api/graph/vault-connect", (GraphVaultConnect r, HttpRequest h, GraphVaultService service, CancellationToken ct)
            => !LocalServerEndpoints.SameOrigin(h) ? Task.FromResult<IResult>(Results.Unauthorized())
                : Reply(() => service.ConnectAsync(r, ct)));
        app.MapPost("/api/graph/vault-sync", (GraphRoleBatchControl r, HttpRequest h, GraphVaultService service, CancellationToken ct)
            => !LocalServerEndpoints.SameOrigin(h) ? Task.FromResult<IResult>(Results.Unauthorized())
                : Reply(() => service.SyncAsync(r.ProjectId, ct)));
        app.MapPost("/api/graph/vault-pick", async (HttpRequest h) =>
        {
            if (!LocalServerEndpoints.SameOrigin(h)) return Results.Unauthorized();
            var path = await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var picker = new Microsoft.Win32.OpenFolderDialog { Title = "Obsidian 보관함 폴더 선택" };
                return picker.ShowDialog() == true ? picker.FolderName : "";
            });
            return Results.Ok(new { path });
        });
    }
    private static async Task<IResult> Reply<T>(Func<Task<T>> action)
    {
        try { return await GraphExplorerEndpoints.Reply(action); }
        catch (Exception e) when (e is System.IO.IOException or UnauthorizedAccessException or System.Text.Json.JsonException)
        { return Results.Conflict(new { error = "보관함 파일을 처리할 수 없습니다. 경로·권한·노트 소유 목록을 확인하세요." }); }
    }
}
