using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace SMSR.App.Mvp;

internal static class GraphLspSelfCheck
{
    internal static GraphLspSession Start(string root, string mode, CancellationToken ct = default,
        TimeSpan? timeout = null) => new(GraphWorker.PythonPath,
        [Path.Combine(AppContext.BaseDirectory, "GraphRuntime", "test_lsp_server.py"), mode], root, ct, timeout);

    internal static async Task RunAsync()
    {
        var root = Path.Combine(Path.GetTempPath(), "smsr-lsp-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var source = "# 한글😀\r\ntarget()\r\n";
            await File.WriteAllTextAsync(Path.Combine(root, "한글 file.py"), source, new UTF8Encoding(false));
            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(source)));
            var store = new EventStore(Path.Combine(root, "test.db"));
            await store.InitializeAsync();
            var file = new GraphFile("한글 file.py", hash, "code");
            var node = new GraphNode("file:" + file.Path, file.Path, "file", file.Path, file.Path, 1, hash);
            await store.ApplyGraphScanAsync("test", new(root, [file], [node], [], [], [file.Path], [], []), false);
            foreach(var mode in new[]{"normal","profile"})
            {
                await using var session = Start(root, mode);
                var result = await new GraphLspDefinitions(store).QueryAsync(session, "test", file.Path, "python", new(1, 0),
                    mode=="profile"?new {fixtureProfile=true}:null);
                if (result.Candidates.Count != 1 || result.Excluded != 1 || result.Candidates[0].Hash != hash ||
                    result.Candidates[0].Path != file.Path || result.Candidates[0].Range.End.Character != 6)
                    throw new Exception("LSP definition/LocationLink normalization failed");
            }
            GraphLspBoundarySelfCheck.Run(root, file.Path, source);
            await GraphLspExitSelfCheck.RunAsync(root);
            await GraphLspFailureSelfCheck.RunAsync(root);
            await File.AppendAllTextAsync(Path.Combine(root, file.Path), "# changed");
            await using var stale = Start(root, "normal");
            try
            {
                await new GraphLspDefinitions(store).QueryAsync(stale, "test", file.Path, "python", new(1, 0));
                throw new Exception("LSP accepted stale indexed source");
            }
            catch (InvalidOperationException) { }
        }
        finally
        {
            try { Directory.Delete(root, true); }
            catch (IOException) { } // Preserve the primary failure; only this GUID fixture remains.
        }
    }
}
