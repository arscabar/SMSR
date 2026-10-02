using System.Text.Json;

namespace SMSR.App.Mvp;

internal static class GraphDeepInvalidationSelfCheck
{
    internal static async Task RunAsync(EventStore store, string root, GraphNode[] nodes, JsonElement saved)
    {
        var doc = new GraphNode("file:Note.md", "Note.md", "document", "Note.md", "Note.md", 1, "note");
        var scan = new GraphScan(root, [new("A.cs", "hash", "code"), new("Note.md", "note", "document")],
            [doc], [], [], ["Note.md"], [], [], 2);
        var next = await store.ApplyGraphScanAsync("deep", scan, false);
        var revision = await store.RefreshGraphDeepAsync("deep", next.Revision);
        if (revision != 4 || !await store.IsGraphDeepReportCurrentAsync("deep", "csharp", "key", saved, default))
            throw new Exception("Unrelated document revision invalidated valid deep inputs");
        var config = new GraphNode("file:A.csproj", "A.csproj", "document", "A.csproj", "A.csproj", 1, "config");
        next = await store.ApplyGraphScanAsync("deep", new(root,
            [new("A.cs", "hash", "code"), new("Note.md", "note", "document"), new("A.csproj", "config", "document")],
            [config], [], [], ["A.csproj"], [], [], revision), false);
        revision = await store.RefreshGraphDeepAsync("deep", next.Revision);
        if ((await store.GetGraphDeepStatusAsync("deep")).Single().Status != "CONFIGURATION_CHANGED" ||
            await store.IsGraphDeepReportCurrentAsync("deep", "csharp", "key", saved, default))
            throw new Exception("Changed analysis configuration stayed valid");
        next = await store.ApplyGraphScanAsync("deep", new(root,
            [new("A.cs", "new", "code"), new("Note.md", "note", "document"), new("A.csproj", "config", "document")],
            nodes.Select(n => n with { Hash = "new" }).ToArray(), [], [], ["A.cs"], [], [], revision), false);
        await store.RefreshGraphDeepAsync("deep", next.Revision);
        if ((await store.GetGraphDeepStatusAsync("deep")).Single().Status != "INPUT_CHANGED_OR_REMOVED")
            throw new Exception("Changed source analysis stayed valid");
        await store.DeleteProjectAsync("deep");
        if ((await store.GetGraphDeepStatusAsync("deep")).Count != 0) throw new Exception("Deleted project retained deep metadata");
    }
}
