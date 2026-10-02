using System.IO;
using System.Diagnostics;

namespace SMSR.App.Mvp;

internal static class GraphCodeIndexSelfCheck
{
    internal static async Task RunAsync()
    {
        await GraphExplorerSelfCheck.RunAsync();
        var root = Path.Combine(Path.GetTempPath(), "smsr-graphify-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var start = new ProcessStartInfo("git") { WorkingDirectory = root, UseShellExecute = false, CreateNoWindow = true };
            start.ArgumentList.Add("init"); start.ArgumentList.Add("-q");
            using var git = Process.Start(start)!; await git.WaitForExitAsync();
            if (git.ExitCode != 0) throw new Exception("Graphify fixture Git init failed");
            await File.WriteAllTextAsync(Path.Combine(root, "a.py"), "from b import target\ndef entry():\n return target(1)\n");
            await File.WriteAllTextAsync(Path.Combine(root, "b.py"), "def target(x):\n return x\n");
            var store = new EventStore(Path.Combine(root, "test.db"));
            await store.InitializeAsync();
            var index = new GraphIndexService(store);
            var first = await index.IndexAsync("graphify-test", root);
            var query = new GraphQueryService(store);
            var target = (await query.SearchAsync("graphify-test", "target", kind: "code")).Nodes.Single(n => n.Kind == "symbol");
            var context = await query.ContextAsync("graphify-test", target.NodeId);
            if (!context.Incoming.Any(n => n.Edge.Relation == "CALLS")
                || !(await query.ContextAsync("graphify-test", "file:a.py")).Outgoing.Any(n => n.Edge.Relation == "CALLS" && n.Node.NodeId == "file:b.py"))
                throw new Exception("Graphify symbol/file call storage failed");
            if (!(await index.IndexAsync("graphify-test", root)).Unchanged) throw new Exception("Unchanged graph revised");
            await GraphCodeEncodingSelfCheck.RunAsync(root, store, index, first.Revision, target.NodeId);
            await File.WriteAllTextAsync(Path.Combine(root, "b.py"), "def renamed(x):\n return x\n");
            await index.IndexAsync("graphify-test", root);
            if ((await store.GetGraphNodeAsync("graphify-test", target.NodeId)) is not null
                || (await query.ContextAsync("graphify-test", "file:a.py")).Outgoing.Any(n => n.Edge.Relation == "CALLS"))
                throw new Exception("Obsolete Graphify symbol/call retained");
            File.Delete(Path.Combine(root, "b.py"));
            await index.IndexAsync("graphify-test", root);
            if ((await store.GetGraphHealthAsync("graphify-test"))!.DanglingEdges != 0)
                throw new Exception("Graphify dangling edge retained");
            await GraphRouteRefreshSelfCheck.RunAsync(root, index, store);
            await GraphCallRecoverySelfCheck.RunAsync(root, index, store);
            await GraphIncrementalSelfCheck.RunAsync();
            await GraphXamlIndexSelfCheck.RunAsync(root,index,query);
            await GraphProjectMembershipSelfCheck.RunAsync(root,index,store,query);
        }
        finally { Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools(); Directory.Delete(root, true); }
    }
}
