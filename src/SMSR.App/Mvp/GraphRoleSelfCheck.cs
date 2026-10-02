using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace SMSR.App.Mvp;

internal static class GraphRoleSelfCheck
{
    internal static async Task RunAsync(EventStore store, string root)
    {
        const string text = "def save():\n return True\ndef audit():\n return save()\n";
        const string path = "role.py", id = "file:role.py";
        await File.WriteAllTextAsync(Path.Combine(root, path), text);
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
        var file = new GraphNode(id, path, "code", path, path, 1, hash);
        var save = file with { NodeId = "symbol:role.py:save", Kind = "symbol", Label = "save" };
        var audit = save with { NodeId = "symbol:role.py:audit", Label = "audit", Line = 3 };
        var call = new GraphEdge(audit.NodeId, save.NodeId, "CALLS", path, 4, "RESOLVED", "EXTRACTED");
        var candidate = new GraphEdge(save.NodeId, audit.NodeId, "CALLS", path, 2, "AMBIGUOUS", "INFERRED");
        var scan = new GraphScan(root, [new(path, hash, "code")], [file, save, audit], [call, candidate], [], [path], [], []);
        await store.ApplyGraphScanAsync("roles", scan, false);
        var service = new GraphRoleService(store);
        if ((await service.ReadAsync("roles", id)).Status != "MISSING") throw new Exception("Missing role fabricated");
        var context = await service.ContextAsync("roles", id);
        if (context.Links.Single().Edge.SourceId != audit.NodeId) throw new Exception("File symbol relation missing");
        var request = Request(context);
        GraphRoleRuleSelfCheck.Run(context);
        await service.SubmitAsync(request);
        if ((await new GraphRoleService(store).ReadAsync("roles", id)).Status != "CURRENT") throw new Exception("Role persistence failed");
        await GraphRoleVersionSelfCheck.RunAsync(store, service, context);
        await GraphRoleJobsSelfCheck.RunAsync(store, service, context);
        await Reject(() => service.SubmitAsync(request with { Fingerprint = "wrong" }));
        await Reject(() => service.SubmitAsync(request with { Claims = [request.Claims[0] with { SupportingQuote = "invented" }] }));
        await Reject(() => service.SubmitAsync(request with { Claims = [request.Claims[0], new("RELATION", "audit가 save를 호출한다.", "EXTRACTED", ["e0"], "return save()")] }));
        await store.ApplyGraphScanAsync("roles", scan with { Edges = [] }, false);
        if ((await service.ReadAsync("roles", id)).Status != "STALE") throw new Exception("Changed edges accepted");
        var jobs = new GraphRoleJobs(store, service); await jobs.RefreshAsync("roles");
        var refreshed = await jobs.ReadAsync("roles", id);
        if (refreshed?.Status != "QUEUED" || refreshed.Fingerprint == context.Fingerprint) throw new Exception("Changed role not requeued");
        var coverage = await new GraphRoleCoverageService(store, service).ReadAsync("roles", "STALE");
        if (coverage.Stale != 1 || coverage.Files.Single().Node.NodeId != id) throw new Exception("Stale role coverage mismatch");
        await jobs.FinishAsync("roles", id, refreshed.RequestId, null);
        await File.AppendAllTextAsync(Path.Combine(root, path), "# changed");
        await Reject(() => service.ContextAsync("roles", id));
    }

    internal static GraphRoleRequest Request(GraphRoleContext context) => new("roles", context.Node.NodeId,
        context.Revision, context.Fingerprint, "host-agent-test", [new("ROLE",
            "save는 True를 반환하고 audit는 save를 호출하는 작은 예제입니다. 저장 부작용은 구현하지 않습니다.",
            "EXTRACTED", ["e0"], "return True")]);

    private static async Task Reject<T>(Func<Task<T>> action)
    {
        try { await action(); } catch (Exception e) when (e is ArgumentException or InvalidOperationException) { return; }
        throw new Exception("Invalid/stale role accepted");
    }
}
