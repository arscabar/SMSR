using System.IO;
namespace SMSR.App.Mvp;

internal static class GraphVaultPathSelfCheck
{
    internal static async Task RunAsync(EventStore store, string root)
    {
        var service = new GraphVaultService(store, new GraphRoleService(store));
        var vault = root + "-vault";
        try
        {
            var state = await service.ConnectAsync(new("batch", vault, Create: true, Confirm: true));
            if (state.Binding?.VaultPath != vault) throw new Exception("Vault binding missing");
            if ((await new GraphVaultService(store, new GraphRoleService(store)).StatusAsync("batch")).Binding?.VaultPath != vault)
                throw new Exception("Vault binding persistence failed");
            foreach (var path in new[] { root, "relative", Path.GetPathRoot(root)! })
                try { await service.ConnectAsync(new("batch", path, Confirm: true)); throw new Exception("Unsafe vault accepted"); }
                catch (Exception e) when (e is ArgumentException or InvalidOperationException) { }
            try { GraphVaultPaths.Inside(vault, "../user.md"); throw new Exception("Manifest traversal accepted"); }
            catch (InvalidOperationException) { }
        }
        finally { if (Directory.Exists(vault)) Directory.Delete(vault, true); }
    }
}
