using System.Diagnostics;
using System.IO;
using SMSR.App.Mvp;

namespace SMSR.App.Services;

internal static class GitAutoIndexRunner
{
    public static async Task RunAsync()
    {
        var start = new ProcessStartInfo("git") { UseShellExecute = false, RedirectStandardOutput = true,
            RedirectStandardError = true, CreateNoWindow = true,
            StandardOutputEncoding = System.Text.Encoding.UTF8, StandardErrorEncoding = System.Text.Encoding.UTF8 };
        start.ArgumentList.Add("rev-parse"); start.ArgumentList.Add("--show-toplevel");
        using var process = Process.Start(start);
        if (process is null) return;
        var root = (await process.StandardOutput.ReadToEndAsync()).Trim();
        await process.WaitForExitAsync();
        if (process.ExitCode != 0 || root.Length == 0) return;
        var database = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SMSR", "smsr.db");
        if (!File.Exists(database)) return;
        var store = new EventStore(database);
        var projectId = await store.FindGraphProjectByRootAsync(root);
        if (projectId is not null) await new GraphIndexService(store).IndexAsync(projectId, root);
    }
}
