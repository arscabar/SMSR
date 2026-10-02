using System.IO;

namespace SMSR.App.Mvp;

internal static class GraphSensitiveFileSelfCheck
{
    internal static async Task RunAsync(EventStore store, string root, GraphIndexService index)
    {
        foreach (var name in new[] { "password.txt", "tokens.json", "API_KEY.yaml" })
            await File.WriteAllTextAsync(Path.Combine(root, name), "fixture-only");
        await File.WriteAllTextAsync(Path.Combine(root, "Token.cs"), "class Token {}");
        await index.IndexAsync("case", root);
        var files = await store.GetGraphFilesAsync("case");
        if (files.Keys.Any(GraphFilePolicy.Sensitive) || !files.ContainsKey("Token.cs"))
            throw new Exception("Sensitive data file exclusion/normal source preservation failed");
        var file = new GraphFile("password.txt", "A", "code");
        var node = new GraphNode("file:password.txt", file.Path, file.Kind, file.Path, file.Path, 1, file.Hash);
        await store.ApplyGraphScanAsync("legacy-sensitive", new(root, [file], [node], [], [], [file.Path], [], []), false);
        try
        {
            await new GraphSourceService(store).GetAsync("legacy-sensitive", file.Path, 1);
            throw new Exception("Legacy indexed secret preview accepted");
        }
        catch (KeyNotFoundException) { }
    }
}
