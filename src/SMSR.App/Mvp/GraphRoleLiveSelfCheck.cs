using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace SMSR.App.Mvp;

internal static class GraphRoleLiveSelfCheck
{
    internal static async Task RunAsync()
    {
        var root = Path.Combine(Path.GetTempPath(), "smsr-role-live-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            using var git = Process.Start(new ProcessStartInfo("git") { UseShellExecute = false, CreateNoWindow = true,
                ArgumentList = { "-C", root, "init", "-q" } })!;
            await git.WaitForExitAsync(); if (git.ExitCode != 0) throw new Exception("Live fixture Git init failed");
            foreach (var (path, code) in new[] {
                ("total.py", "def total(values):\n    return sum(values)\n"),
                ("Total.cs", "public static class Total { public static int Add(int a, int b) => a + b; }\n"),
                ("Total.java", "public class Total { public int add(int a, int b) { return a + b; } }\n") })
                await File.WriteAllTextAsync(Path.Combine(root, path), code);
            var store = new EventStore(Path.Combine(root, "smsr.db")); await store.InitializeAsync();
            await new GraphIndexService(store).IndexAsync("live", root);
            await using var host = await LocalServer.StartAsync(root, 0);
            using var client = new HttpClient(); client.DefaultRequestHeaders.Add("Origin", host.Address);
            var roles = new GraphRoleService(store);
            var reports = new List<GraphRoleReport>();
            foreach (var path in new[] { "total.py", "Total.cs", "Total.java" })
            {
                var id = "file:" + path;
                using var response = await client.PostAsJsonAsync(host.Address + "/api/graph/role-job", new GraphRoleJobRequest("live", id));
                response.EnsureSuccessStatusCode();
                var deadline = DateTimeOffset.UtcNow.AddMinutes(6);
                GraphRoleJob? job;
                do
                {
                    await Task.Delay(1000);
                    job = await client.GetFromJsonAsync<GraphRoleJob>(host.Address + "/api/graph/role-job?projectId=live&nodeId=" + Uri.EscapeDataString(id));
                } while (job?.Status is "QUEUED" or "RUNNING" && DateTimeOffset.UtcNow < deadline);
                if (job?.Status != "SUCCESS" || (await roles.ReadAsync("live", id)).Status != "CURRENT")
                    throw new Exception("Actual Codex role pipeline failed: " + path + " / " + job?.Status + " / " + job?.Error);
                reports.Add((await roles.ReadAsync("live", id)).Report!);
            }
            var before = reports[0].Fingerprint;
            await File.WriteAllTextAsync(Path.Combine(root, "total.py"), "def total(values):\n    return len(values)\n");
            if ((await new GraphRoleCoverageService(store, roles).ReadAsync("live")).Stale != 1)
                throw new Exception("Changed source was shown as current");
            await new GraphIndexService(store, new GraphRoleJobs(store, roles)).IndexAsync("live", root);
            var until = DateTimeOffset.UtcNow.AddMinutes(6);
            GraphRoleStatus? changed = null;
            do { await Task.Delay(1000); changed = await roles.ReadAsync("live", "file:total.py"); }
                while (changed.Status != "CURRENT" && DateTimeOffset.UtcNow < until);
            if (changed.Status != "CURRENT" || changed.Report!.Fingerprint == before
                || !changed.Report.Evidence.Any(e => e.Quote.Contains("return len(values)")))
                throw new Exception("Changed source automatic explanation refresh failed");
            reports.Add(changed.Report);
            await File.WriteAllTextAsync(Path.Combine(Environment.CurrentDirectory, "artifacts/role-live-v2-result.json"),
                JsonSerializer.Serialize(new { coverage = await new GraphRoleCoverageService(store, roles).ReadAsync("live"), reports }, GraphWorker.Json));
        }
        finally { Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools(); Directory.Delete(root, true); }
    }
}
