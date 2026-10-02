using System.IO;
namespace SMSR.App.Mvp;

internal static class GraphVaultSafetySelfCheck
{
    internal static async Task RunAsync(EventStore store, string root)
    {
        var vault = root + "-vault-safety";
        try
        {
            var service = new GraphVaultService(store, new GraphRoleService(store));
            await service.ConnectAsync(new("batch", vault, Create: true, Confirm: true));
            var first = await service.SyncAsync("batch");
            var area = GraphVaultPaths.Area(vault, "batch");
            if (first.Written != 2 || first.Conflicts.Length != 0) throw new Exception("Vault initial notes failed");
            var second = await service.SyncAsync("batch");
            if (second.Written != 0 || second.Unchanged != 2) throw new Exception("Vault unchanged files rewritten");
            var owned = Directory.GetFiles(area, "a.py--*.md").Single();
            await File.AppendAllTextAsync(owned, "\nUSER NOTE\n");
            var text = await File.ReadAllTextAsync(owned);
            var third = await service.SyncAsync("batch");
            if (third.Conflicts.Length != 1 || await File.ReadAllTextAsync(owned) != text) throw new Exception("User note overwritten");
            await File.WriteAllTextAsync(Path.Combine(vault, "personal.md"), "User authored");
            await service.SyncAsync("batch");
            if (await File.ReadAllTextAsync(Path.Combine(vault, "personal.md")) != "User authored") throw new Exception("Foreign note changed");
            await File.WriteAllTextAsync(Path.Combine(area, ".smsr-manifest.json"), "{}");
            try { await service.SyncAsync("batch"); throw new Exception("Foreign manifest accepted"); }
            catch (InvalidOperationException) { }
        }
        finally { if (Directory.Exists(vault)) Directory.Delete(vault, true); }
    }
}
