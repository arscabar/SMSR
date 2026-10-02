using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace SMSR.App.Mvp;

internal static class GraphRoleContextSelfCheck
{
    internal static async Task RunAsync(EventStore store, string root)
    {
        var sources = new[] {
            ("Main.cs", "namespace Demo;\npublic partial class Service\n{\n public string Run() => Save() + Read();\n}\n", "Run"),
            ("Save.cs", "namespace Demo;\npublic partial class Service\n{\n public string Save() => \"persist\";\n}\n", "Save"),
            ("Read.cs", "namespace Demo;\npublic partial class Service\n{\n public string Read() => \"current\";\n}\n", ".Read()"),
            ("Other.cs", "namespace Other;\npublic partial class Service\n{\n public string Read() => \"wrong-namespace\";\n}\n", "Read") };
        var files = new List<GraphFile>(); var nodes = new List<GraphNode>();
        foreach (var (path, text, method) in sources)
        {
            await File.WriteAllTextAsync(Path.Combine(root, path), text);
            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
            files.Add(new(path, hash, "code"));
            nodes.Add(new("file:" + path, path, "code", path, path, 1, hash));
            nodes.Add(new("class:" + path, path, "symbol", "Service", path, 2, hash,
                new("class", "code", "fixture", "1", "file:" + path, 5)));
            nodes.Add(new("method:" + path, path, "symbol", method, path, 4, hash,
                new("method", "code", "fixture", "1", "class:" + path, 4)));
        }
        var edge = new GraphEdge("method:Main.cs", "method:Save.cs", "CALLS", "Main.cs", 4, "RESOLVED", "EXTRACTED");
        await store.ApplyGraphScanAsync("role-context", new(root, files, nodes, [edge], [], files.Select(f => f.Path).ToArray(), [], []), false);
        var roles = new GraphRoleService(store);
        var context = await roles.ContextAsync("role-context", "file:Main.cs");
        if (!context.Evidence.Any(e => e.Path == "Save.cs" && e.Edge is null && e.Quote.Contains("persist"))
            || !context.Evidence.Any(e => e.Path == "Read.cs" && e.Edge is null && e.Quote.Contains("current"))
            || context.Evidence.Any(e => e.Path == "Other.cs"))
            throw new Exception("Related implementation missing or partial names merged across namespaces");
        await File.AppendAllTextAsync(Path.Combine(root, "Read.cs"), "// changed");
        var stale = await roles.ContextAsync("role-context", "file:Main.cs");
        if (!stale.Unavailable.Contains("Read.cs") || stale.Fingerprint == context.Fingerprint)
            throw new Exception("Partial helper freshness was not fingerprinted");
    }
}
