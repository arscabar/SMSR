using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
namespace SMSR.App.Mvp;

internal static class GraphVaultHttpSelfCheck
{
    internal static async Task RunAsync(HttpClient client, string address, string root)
    {
        foreach (var module in new[] { "role-batch", "batch-preview", "sync-http", "vault" })
            if (!(await client.GetAsync(address + "/assets/graph-explorer-" + module + ".js")).IsSuccessStatusCode)
                throw new Exception("Knowledge module unavailable: " + module);
        var vault = root + "-http-vault";
        try
        {
            foreach (var (route, payload) in new (string, object)[] {
                ("role-batch-preview", new GraphRoleBatchRequest("sample", [], 0)),
                ("role-batch", new GraphRoleBatchRequest("sample", [], 0, true)),
                ("role-batch-control", new GraphRoleBatchControl("sample", "pause")),
                ("vault-connect", new GraphVaultConnect("sample", vault, false, true, true)),
                ("vault-sync", new GraphRoleBatchControl("sample", "sync")) })
            {
                using var denied = await client.PostAsJsonAsync(address + "/api/graph/" + route, payload);
                if ((int)denied.StatusCode != 401) throw new Exception("Knowledge write origin guard missing: " + route);
            }
            async Task<T> Post<T>(string route, object value)
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, address + "/api/graph/" + route)
                    { Content = JsonContent.Create(value) };
                request.Headers.Add("Origin", address);
                using var response = await client.SendAsync(request); response.EnsureSuccessStatusCode();
                return (await response.Content.ReadFromJsonAsync<T>())!;
            }
            var preview = await Post<GraphRoleBatchPreview>("role-batch-preview", new GraphRoleBatchRequest("sample", [], 0));
            if (preview.Files.Length != 0) throw new Exception("Document incorrectly offered as code batch");
            await Post<GraphVaultStatus>("vault-connect", new GraphVaultConnect("sample", vault, false, true, true));
            var sync = await Post<GraphVaultResult>("vault-sync", new GraphRoleBatchControl("sample", "sync"));
            if (sync.Written != 2 || sync.Conflicts.Length != 0) throw new Exception("Vault HTTP synchronization failed");
            var notes = Directory.GetFiles(GraphVaultPaths.Area(vault, "sample"), "*.md")
                .Where(path => Path.GetFileName(path) != "index.md").Select(File.ReadAllText).ToArray();
            if (notes.Any(note => !note.Contains(address + "/graph/source?")))
                throw new Exception("Vault source link does not use the running server address");
        }
        finally { if (Directory.Exists(vault)) Directory.Delete(vault, true); }
    }
}
