using System.Diagnostics;
using System.IO;
using System.Text.Json;
namespace SMSR.App.Mvp;

internal static class GraphQualityPreview
{
    internal static async Task RunAsync(string reportPath)
    {
        reportPath = Path.GetFullPath(reportPath);
        var area = Path.GetDirectoryName(reportPath)!;
        var root = Path.Combine(area, "source"); var data = Path.Combine(area, "data");
        if (Directory.Exists(root) || Directory.Exists(data)) throw new IOException("Use a new acceptance directory.");
        Directory.CreateDirectory(root); Directory.CreateDirectory(data);
        var sources = new[] { "src/SMSR.App/Mvp/GraphVaultSync.cs", "src/SMSR.App/Mvp/GraphRoleBatchService.cs",
            "src/SMSR.App/WebAssets/graph-explorer-related.js", "src/SMSR.App/GraphRuntime/graphify_cache_io.py" };
        GraphQualitySources.Copy(root, sources);
        using (var git = Process.Start(new ProcessStartInfo("git") { UseShellExecute = false,
            CreateNoWindow = true, ArgumentList = { "-C", root, "init", "-q" } })!)
        { await git.WaitForExitAsync(); if (git.ExitCode != 0) throw new IOException("Fixture Git init failed."); }
        var store = new EventStore(Path.Combine(data, "smsr.db")); await store.InitializeAsync();
        await new GraphIndexService(store).IndexAsync("QualityTest", root);
        await GraphClientPreview.SeedAsync(store);
        await using var host = new SMSR.App.Services.LocalServerHost(data, 0);
        await host.StartAsync();
        var window = await GraphClientPreview.OpenAsync(host);
        await File.WriteAllTextAsync(reportPath, JsonSerializer.Serialize(new { address = host.Address,
            projectId = "QualityTest", root, data, vault = Path.Combine(area, "vault"), sources,
            pid = Environment.ProcessId }, GraphWorker.Json));
        var roles = new GraphRoleService(store); var jobs = new GraphRoleJobs(store, roles);
        var until = DateTimeOffset.UtcNow.AddMinutes(45);
        while (DateTimeOffset.UtcNow < until && !File.Exists(reportPath + ".stop"))
        {
            var output = new List<object>();
            foreach (var source in sources)
            {
                var id = "file:" + source;
                output.Add(new { source, context = await roles.ContextAsync("QualityTest", id),
                    result = await roles.ReadAsync("QualityTest", id), job = await jobs.ReadAsync("QualityTest", id) });
            }
            await File.WriteAllTextAsync(reportPath + ".results.json", JsonSerializer.Serialize(output, GraphWorker.Json));
            await Task.Delay(2000);
        }
        window.Close();
    }
}
