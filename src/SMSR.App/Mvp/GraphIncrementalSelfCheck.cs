using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace SMSR.App.Mvp;

internal static class GraphIncrementalSelfCheck
{
    internal static async Task RunAsync()
    {
        var root = Path.Combine(Path.GetTempPath(), "smsr-incremental-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var start = new ProcessStartInfo("git") { WorkingDirectory = root, UseShellExecute = false, CreateNoWindow = true };
            start.ArgumentList.Add("init"); start.ArgumentList.Add("-q");
            using var git = Process.Start(start)!; await git.WaitForExitAsync();
            if (git.ExitCode != 0) throw new Exception("Incremental fixture Git init failed");
            await File.WriteAllTextAsync(Path.Combine(root, "Base.cs"), "namespace Lib; public class Base { public void Run() {} }");
            await File.WriteAllTextAsync(Path.Combine(root, "Alone.cs"), "namespace Other; public class Alone { public void Silent() {} }");
            var child = Path.Combine(root, "Child.cs");
            const string caller = "namespace App; using Lib; public class Child { public void Go() { Base worker = new Base(); worker.Run();";
            await File.WriteAllTextAsync(child, caller + " } }");
            var store = new EventStore(Path.Combine(root, "fixture.db")); await store.InitializeAsync();
            var index = new GraphIndexService(store); await index.IndexAsync("incremental", root);
            await File.WriteAllTextAsync(child, caller + " int count = 2; } }");
            var partial = await index.IndexAsync("incremental", root);
            if (partial.CodeAnalysis is not { Mode: "INCREMENTAL", AffectedFiles: 1, ReusedFiles: 2 })
                throw new Exception("Ordinary C# did not reuse unchanged resolver context");
            await index.IndexAsync("full", root);
            if (await Snapshot(store, "incremental") != await Snapshot(store, "full"))
                throw new Exception("Partial database nodes/edges differ from full index");
            File.Delete(Path.Combine(root, "Base.cs"));
            await index.IndexAsync("incremental", root); await index.IndexAsync("full", root);
            if (await Snapshot(store, "incremental") != await Snapshot(store, "full")
                || (await store.GetGraphHealthAsync("incremental"))!.DanglingEdges != 0)
                throw new Exception("Deleted callee left obsolete incremental facts");
        }
        finally { Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools(); Directory.Delete(root, true); }
    }

    private static async Task<string> Snapshot(EventStore store, string project)
    {
        var nodes = await store.SearchGraphNodesAsync(project, "", 100);
        var edges = await store.GetGraphAdjacentAsync(project, nodes.Select(n => n.NodeId).ToArray(), false);
        return JsonSerializer.Serialize(new { nodes = nodes.OrderBy(n => n.NodeId),
            edges = edges.Select(e => e with { Evidence = e.Evidence is null ? null : e.Evidence with { CapturedAt = null } })
                .OrderBy(e => e.SourceId).ThenBy(e => e.TargetId).ThenBy(e => e.Relation)
                .ThenBy(e => e.OwnerPath).ThenBy(e => e.SourceLine) }, GraphWorker.Json);
    }
}
