using System.IO;
using System.Security.Cryptography;
using System.Text;
namespace SMSR.App.Mvp;

internal static class GraphRoleBatchSelfCheck
{
    internal static async Task RunAsync(EventStore store, string root)
    {
        const string path = "batch/a.py", body = "def run():\n return True\n";
        Directory.CreateDirectory(Path.Combine(root, "batch"));
        await File.WriteAllTextAsync(Path.Combine(root, path), body);
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(body)));
        var node = new GraphNode("file:" + path, path, "code", "a.py", path, 1, hash);
        await store.ApplyGraphScanAsync("batch", new(root, [new(path, hash, "code")], [node], [], [], [path], [], []), false);
        var roles = new GraphRoleService(store); var jobs = new GraphRoleJobs(store, roles);
        var batch = new GraphRoleBatchService(store, roles, jobs);
        var preview = await batch.PreviewAsync("batch", ["batch"]);
        if (preview.Eligible != 1 || preview.Excluded != 0) throw new Exception("Batch scope preview failed");
        await Reject(() => batch.PreviewAsync("batch", ["../"]));
        await Reject(() => batch.StartAsync(new("batch", [], preview.Revision)));
        await Reject(() => batch.StartAsync(new("batch", [], preview.Revision + 1, true)));
        var state = await batch.StartAsync(new("batch", ["batch"], preview.Revision, true));
        if (state.Pending != 1 || (await new GraphRoleBatchService(store, roles, jobs).StatusAsync("batch")).Pending != 1)
            throw new Exception("Batch persistence failed");
        await Reject(() => batch.StartAsync(new("batch", [], preview.Revision, true)));
        await GraphRoleBatchControlSelfCheck.RunAsync(node, jobs, batch);
        await GraphRoleBatchRefreshSelfCheck.RunAsync(store, root, node, roles, jobs, batch);
    }
    private static async Task Reject<T>(Func<Task<T>> action)
    {
        try { await action(); } catch (Exception e) when (e is ArgumentException or InvalidOperationException) { return; }
        throw new Exception("Invalid batch accepted");
    }
}
