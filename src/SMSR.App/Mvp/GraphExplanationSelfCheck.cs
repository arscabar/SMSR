using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace SMSR.App.Mvp;

internal static class GraphExplanationSelfCheck
{
    internal static async Task RunAsync(EventStore store, string root)
    {
        var text = "def target():\n return 1\n";
        await File.WriteAllTextAsync(Path.Combine(root, "explain.py"), text);
        var file = new GraphFile("explain.py", Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text))), "code");
        var node = new GraphNode("file:explain.py", file.Path, "code", file.Path, file.Path, 1, file.Hash);
        var issues = Enumerable.Range(1, 23).Select(i => new GraphIssue(file.Path, i, "PARSE_ERROR", "recovery" + i, [])).ToArray();
        await store.ApplyGraphScanAsync("explain", new(root, [file], [node], [], issues, [file.Path], [], []), false);
        var query = new GraphQueryService(store);
        var fresh = await query.ExplainAsync("explain", node.NodeId);
        if (fresh.SourceStatus != "CURRENT" || fresh.Diagnostics.Total != 23 || fresh.Diagnostics.Items.Length != 20)
            throw new Exception("Explanation source/diagnostics mismatch");
        if ((await store.GraphIssuesAsync("explain", file.Path, 20)).Items.Length != 3)
            throw new Exception("Diagnostics second page missing");
        await File.AppendAllTextAsync(Path.Combine(root, file.Path), "# changed");
        if ((await query.ExplainAsync("explain", node.NodeId)).SourceStatus != "STALE_OR_UNAVAILABLE")
            throw new Exception("Stale explanation source accepted");
    }
}
