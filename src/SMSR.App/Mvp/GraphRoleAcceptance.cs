using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using SMSR.App.Services;

namespace SMSR.App.Mvp;

internal static class GraphRoleAcceptance
{
    internal static async Task RunAsync()
    {
        var root = Path.Combine(Path.GetTempPath(), "smsr-role-check-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var store = new EventStore(Path.Combine(root, "smsr.db")); await store.InitializeAsync();
            await GraphFindFileSelfCheck.RunAsync(store, root);
            await GraphRoleSelfCheck.RunAsync(store, root);
            await GraphRoleContextSelfCheck.RunAsync(store, root);
            await GraphRoleBatchSelfCheck.RunAsync(store, root);
            await GraphVaultPathSelfCheck.RunAsync(store, root);
            GraphVaultNoteSelfCheck.Run();
            await GraphVaultSafetySelfCheck.RunAsync(store, root);
            await GraphVaultLifecycleSelfCheck.RunAsync(store, root);
            Directory.CreateDirectory(Path.Combine(root, "docs"));
            const string text = "# Evidence\nCurrent source-backed document.\n", path = "docs/a.md";
            await File.WriteAllTextAsync(Path.Combine(root, path), text);
            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
            var node = new GraphNode("file:" + path, path, "document", "Evidence", path, 1, hash);
            await store.ApplyGraphScanAsync("sample", new(root, [new(path, hash, "document")],
                [node], [], [], [path], [], []), false);
            await using var host = await LocalServer.StartAsync(root, 0);
            using var client = new HttpClient();
            await GraphRoleHttpSelfCheck.RunAsync(client, host.Address, new McpHttpGateway(host.Address, root));
            await GraphVaultHttpSelfCheck.RunAsync(client, host.Address, root);
        }
        finally { Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools(); Directory.Delete(root, true); }
    }
}
