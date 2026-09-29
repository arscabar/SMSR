using System.Diagnostics;

namespace SMSR.App.Mvp;

internal static class GraphLspExitSelfCheck
{
    internal static async Task RunAsync(string root)
    {
        await using var session=GraphLspSelfCheck.Start(root,"slow-exit");
        await session.InitializeAsync(root);
        var child=(await session.RequestAsync("smsr/testChild",null)).GetInt32();
        var timer=Stopwatch.StartNew();
        await session.ShutdownAsync();
        if(timer.Elapsed>TimeSpan.FromSeconds(8)) throw new Exception("LSP exit grace period unbounded");
        foreach(var id in new[]{session.ProcessId,child})
            try
            {
                using var process=Process.GetProcessById(id);
                if(!process.HasExited) throw new Exception("LSP shutdown left process running");
            }
            catch(ArgumentException) { }
    }
}
