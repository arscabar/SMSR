using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SMSR.App.Mvp;

internal static class GraphCSharpSelfCheck
{
    internal static async Task RunAsync(string root)
    {
        var folder = Path.Combine(root, "csharp-http");
        await using var host = await LocalServer.StartAsync(folder, 0);
        var store = new EventStore(Path.Combine(folder, "smsr.db"));
        var input = new Dictionary<string, string> {
            ["Library.cs"] = "public static class Lib { public static int Pick(int x) => x > 0 ? x : -x; public static string Pick(string x) => x; }",
            ["Entry.cs"] = "using Alias = Lib; public class Entry { int Run(int input) => Alias.Pick(input); } // SOURCE_MARKER_NOT_SAVED"
        };
        var files = new List<GraphFile>();
        var nodes = new List<GraphNode>();
        foreach (var (path, text) in input)
        {
            await File.WriteAllTextAsync(Path.Combine(root, path), text, new UTF8Encoding(false));
            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
            files.Add(new(path, hash, "code"));
            nodes.Add(new("file:" + path, path, "file", path, path, 1, hash));
        }
        await store.ApplyGraphScanAsync("csharp", new(root, files, nodes, [], [], input.Keys.ToArray(), [], []), false);
        using var client = new HttpClient();
        var request = new GraphCSharpRequest("csharp", input.Keys.ToArray());
        using var denied = await client.PostAsJsonAsync(host.Address + "/api/graph/csharp", request);
        if (denied.StatusCode != HttpStatusCode.Unauthorized) throw new Exception("C# origin check missing");
        client.DefaultRequestHeaders.Add("Origin", host.Address);
        using var response = await client.PostAsJsonAsync(host.Address + "/api/graph/csharp", request);
        response.EnsureSuccessStatusCode();
        var report = await response.Content.ReadFromJsonAsync<JsonElement>();
        if (report.GetProperty("analysisVersion").GetInt32() != 9 ||
            report.GetProperty("result").GetProperty("functions").GetArrayLength() != 3)
            throw new Exception("C# compiler flow not exposed over HTTP");
        if (report.GetProperty("result").GetProperty("status").GetString() != "BOUND_INPUT_BUNDLE" ||
            report.GetProperty("result").GetProperty("calls")[0].GetProperty("targetId").GetString() != "source:M:Lib.Pick(System.Int32)")
            throw new Exception("C# actual overload binding failed");
        await GraphCSharpStorageSelfCheck.RunAsync(root, store, client, host.Address, folder, request);
        GraphCSharpConnectionsSelfCheck.Verify(report.GetProperty("result"));
    }
}
