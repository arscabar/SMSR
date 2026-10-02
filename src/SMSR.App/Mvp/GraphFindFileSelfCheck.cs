namespace SMSR.App.Mvp;

internal static class GraphFindFileSelfCheck
{
    internal static async Task RunAsync(EventStore store, string root)
    {
        var hash = new string('A', 64);
        var file = new GraphNode("file:main.java", "main.java", "code", "main.java", "main.java", 1, hash);
        var type = file with { NodeId = "symbol:type", Kind = "symbol", Label = "Switches",
            Details = new("class", "code", "Graphify", "test", file.NodeId) };
        var children = Enumerable.Range(0, 65).Select(i => type with { NodeId = "symbol:method" + i,
            Label = ".pick" + i + "()", Line = i + 2, Details = type.Details with { EntityKind = "method", OwnerNodeId = type.NodeId } }).ToArray();
        var doc = file with { NodeId = "file:notes.md", OwnerPath = "notes.md", SourcePath = "notes.md", Kind = "document", Label = "notes.md" };
        await store.ApplyGraphScanAsync("find-files", new(root, [new("main.java", hash, "code"),new("notes.md", hash, "document")],
            [file,type,..children,doc], [], [], ["main.java","notes.md"], [], []), false);
        var query = new GraphQueryService(store);
        if ((await query.FilesAsync("find-files", "pick", "code")).Nodes.Single().NodeId != file.NodeId
            || (await query.FilesAsync("find-files", "", "code")).Nodes.Count != 1
            || (await query.FilesAsync("find-files", "pick", "document")).Nodes.Count != 0)
            throw new Exception("File search lost symbol matches or leaked symbols");
        var first = await query.FilesAsync("find-files", "", limit: 1);
        var next = await query.FilesAsync("find-files", "", offset: 1, limit: 1);
        if (!first.Truncated || next.Truncated || first.Nodes.Single().NodeId == next.Nodes.Single().NodeId)
            throw new Exception("File pagination duplicate/missing");
        if ((await query.SymbolsAsync("find-files", file.NodeId)).Nodes.Single().NodeId != type.NodeId)
            throw new Exception("Symbol owner hierarchy flattened");
        var found = new List<GraphNode>();
        for (var offset = 0; offset < 65; offset += 30) found.AddRange((await query.SymbolsAsync("find-files", type.NodeId, offset, 1)).Nodes);
        if (found.Count != 65 || found.Select(n=>n.NodeId).Distinct().Count()!=65) throw new Exception("Symbol pagination mismatch");
        try { await query.SymbolsAsync("find-files", type.NodeId, expectedRevision: 99); throw new Exception("Mixed revision accepted"); }
        catch (InvalidOperationException) { }
        var part = file with { NodeId = "file:partial.java", OwnerPath = "partial.java", SourcePath = "partial.java" };
        var method = children[0] with { NodeId = "symbol:partial", OwnerPath = part.OwnerPath, SourcePath = part.SourcePath };
        await store.ApplyGraphScanAsync("find-partial", new(root, [new("main.java",hash,"code"),new("partial.java",hash,"code")],
            [file,type,part,method], [], [], ["main.java","partial.java"], [], []), false);
        var partial = await query.SymbolsAsync("find-partial", part.NodeId);
        if (partial.Nodes.Single().NodeId != method.NodeId || partial.Owners[type.NodeId] != "Switches")
            throw new Exception("Cross-file owner hid file-local method");
    }
}
