using System.IO;

namespace SMSR.App.Mvp;

internal static class GraphCallRecoverySelfCheck
{
    internal static async Task RunAsync(string root, GraphIndexService index, EventStore store)
    {
        var attributes = string.Join("\n", Enumerable.Range(0, 5).Select(i => $$"""
            #if DEBUG
            [System.ComponentModel.Browsable(true)]
            [System.ComponentModel.DisplayName("P{{i}}")]
            #else
            [System.ComponentModel.Browsable(false)]
            #endif
            public string P{{i}} { get; set; }
            """));
        await File.WriteAllTextAsync(Path.Combine(root, "RecoveryTarget.cs"),
            "namespace Recovery { public class Target {\n" + attributes + "\npublic void ResetIResult() {} }}");
        await File.WriteAllTextAsync(Path.Combine(root, "RecoveryCaller.cs"),
            "using Recovery; class Caller { void Go(Target inst, object unknown) { inst.ResetIResult(); unknown.Absent(); } }");
        await index.IndexAsync("graphify-test", root);
        var query = new GraphQueryService(store);
        var target = (await query.SearchAsync("graphify-test", "ResetIResult", kind: "code"))
            .Nodes.Single(n => n.Kind == "symbol");
        var calls = await query.RelationsAsync("graphify-test", target.NodeId, "incoming", "CALLS");
        if (calls.Total != 1 || calls.Items[0].Edge.OwnerPath != "RecoveryCaller.cs")
            throw new Exception("Index did not automatically resolve conditional-attribute receiver call");
        var issues = await store.GraphIssuesAsync("graphify-test", "RecoveryCaller.cs");
        if (!issues.Items.Any(i => i.Relation == "CALLS" && i.Reason.Contains("unknown.Absent")))
            throw new Exception("Unmatched call site was silently lost");
        if (!(await index.IndexAsync("graphify-test", root)).Unchanged)
            throw new Exception("Unchanged analysis revised graph");
    }
}
