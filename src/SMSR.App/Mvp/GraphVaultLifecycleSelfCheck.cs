using System.IO;
using System.Security.Cryptography;
using System.Text;
namespace SMSR.App.Mvp;

internal static class GraphVaultLifecycleSelfCheck
{
    internal static async Task RunAsync(EventStore store, string root)
    {
        var vault = root + "-vault-cycle";
        try
        {
            const string project = "vault-cycle", path = "cycle.py", body = "def run():\n return True\n";
            await File.WriteAllTextAsync(Path.Combine(root, path), body);
            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(body)));
            var node = new GraphNode("file:" + path, path, "code", path, path, 1, hash);
            await store.ApplyGraphScanAsync(project, new(root, [new(path, hash, "code")], [node], [], [], [path], [], []), false);
            var roles = new GraphRoleService(store); var service = new GraphVaultService(store, roles);
            await service.ConnectAsync(new(project, vault, AutoSync: true, Create: true, Confirm: true));
            await service.SyncAsync(project);
            var context = await roles.ContextAsync(project, node.NodeId);
            await roles.SubmitAsync(new(project, node.NodeId, context.Revision, context.Fingerprint, "test",
                [new("ROLE", "True를 반환하는 함수", "EXTRACTED", ["e0"], "return True")]));
            service.SourceAddress = () => "";
            await service.AutoSyncAsync(project, default);
            if (!string.IsNullOrEmpty((await service.StatusAsync(project)).Error))
                throw new Exception("Server binding delay incorrectly persisted an auto-sync failure");
            service.SourceAddress = () => "http://127.0.0.1:54815";
            await service.AutoSyncAsync(project, default);
            var last = (await service.StatusAsync(project)).LastSync;
            if (last?.Written != 1 || last.Unchanged != 1) throw new Exception("Role update did not incrementally sync");
            if (!File.ReadAllText(Path.Combine(GraphVaultPaths.Area(vault, project), GraphVaultPaths.Name(node)))
                .Contains("http://127.0.0.1:54815/graph/source?")) throw new Exception("Delayed binding lost its source address");
            if (!(await service.NoteUriAsync(project, node.NodeId)).StartsWith("obsidian://open?path=")) throw new Exception("Note URI missing");
            var renamed = node with { NodeId = "file:moved.py", OwnerPath = "moved.py", SourcePath = "moved.py", Label = "moved.py" };
            await File.WriteAllTextAsync(Path.Combine(root, "moved.py"), body);
            await store.ApplyGraphScanAsync(project, new(root, [new("moved.py", hash, "code")], [renamed], [], [], ["moved.py"], [], [path]), false);
            await service.DirtyAsync(project, default); await service.AutoSyncAsync(project, default);
            if ((await service.StatusAsync(project)).LastSync?.Retired != 1) throw new Exception("Moved source note not marked retired");
            var area = GraphVaultPaths.Area(vault, project);
            if (!File.Exists(Path.Combine(area, GraphVaultPaths.Name(node)))) throw new Exception("Retired note deleted");
        }
        finally { if (Directory.Exists(vault)) Directory.Delete(vault, true); }
    }
}
