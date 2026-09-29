using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SMSR.App.Mvp;

internal static class GraphAdvancedSelfCheck
{
    public static async Task RunAsync()
    {
        var root = Path.Combine(Path.GetTempPath(), "smsr-advanced-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var source = "def login():\n    x = input()\n    eval(x)\n";
            await File.WriteAllTextAsync(Path.Combine(root, "login.py"), source, new UTF8Encoding(false));
            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(source)));
            var store = new EventStore(Path.Combine(root, "test.db"));
            await store.InitializeAsync();
            var file = new GraphFile("login.py", hash, "code");
            var node = new GraphNode("file:login.py", "login.py", "file", "user login authentication", "login.py", 1, hash);
            await store.ApplyGraphScanAsync("test", new(root, [file], [node], [], [], [file.Path], [], []), false);
            var service = new GraphAdvancedService(store, new GraphWorker());
            var cypher = await service.CypherAsync("test", "MATCH(n:Node) RETURN count(n)");
            if (cypher.GetProperty("rows")[0][0].GetInt32() != 1) throw new Exception("Cypher projection failed");
            try { await service.CypherAsync("test", "CREATE (n:Node {id:'bad'})"); throw new Exception("Unsafe query accepted"); }
            catch (InvalidOperationException) { }
            var report = await service.AnalyzeAsync("test", "login.py");
            if (report.GetProperty("result").GetProperty("findings").GetArrayLength() == 0) throw new Exception("Taint path missing");
            if (report.GetProperty("result").GetProperty("calls").EnumerateArray().Any(call =>
                call.GetProperty("selection").ValueKind != JsonValueKind.Object ||
                call.GetProperty("resolution").GetString() != "UNRESOLVED"))
                throw new Exception("Syntax call positions or precision contract missing");
            var saved = JsonSerializer.Serialize(await service.AnalysisAsync("test", "login.py"));
            if (!saved.Contains("false")) throw new Exception("Analysis freshness failed");
            var search = await service.SemanticAsync("test", "사용자 로그인");
            if (search.Hits.Single().Node.NodeId != node.NodeId) throw new Exception("Vector ranking failed");
            await File.AppendAllTextAsync(Path.Combine(root, "login.py"), "# changed");
            var stale = JsonSerializer.SerializeToElement(await service.AnalysisAsync("test", "login.py"));
            if (!stale.GetProperty("stale").GetBoolean()) throw new Exception("Current source drift missing");
            try { await service.AnalyzeAsync("test", "login.py"); throw new Exception("Stale source accepted"); }
            catch (InvalidOperationException) { }
            await GraphBodySelfCheck.RunAsync(store, root, service);
            await GraphBodyEndpointSelfCheck.RunAsync(root);
            await GraphCSharpSelfCheck.RunAsync(root);
            await GraphJavaSelfCheck.RunAsync(root);
            await GraphTypeScriptSelfCheck.RunAsync(root);
            await GraphPythonSelfCheck.RunAsync(root);
            await store.DeleteProjectAsync("test");
            if (await store.GetGraphDerivedAsync("test", "analysis", "login.py") is not null) throw new Exception("Derived data deletion failed");
        }
        finally { Directory.Delete(root, true); }
    }
}
