using System.Diagnostics;
using System.IO;
using System.Text;

namespace SMSR.App.Mvp;

internal static class GraphLspFailureSelfCheck
{
    internal static async Task RunAsync(string root)
    {
        foreach (var header in new[] { "Content-Length: 1\r\nContent-Length: 1", "Content-Length: -1",
            "Content-Length: 999999999", "Content-Length: 2\r\nContent-Type: application/vscode-jsonrpc; charset=ascii",
            "Content-Type: application/vscode-jsonrpc", new string('a', 8193) })
        {
            using var stream = new MemoryStream(Encoding.ASCII.GetBytes(header + "\r\n\r\n{}"));
            try { await GraphLspWire.ReadAsync(stream, default); throw new Exception("Invalid LSP frame accepted"); }
            catch (InvalidDataException) { }
        }
        foreach (var mode in new[] { "oversize", "encoding", "wrong-id", "error" })
        {
            await using var session = GraphLspSelfCheck.Start(root, mode);
            try { await session.InitializeAsync(root); throw new Exception("Unsafe server accepted"); }
            catch (InvalidDataException) { }
            catch (InvalidOperationException error) when (mode == "error")
            {
                if (error.ToString().Contains("FIXTURE_SECRET")) throw new Exception("Raw LSP error leaked");
            }
        }
        using (var truncated = new MemoryStream(Encoding.ASCII.GetBytes("Content-Length: 4\r\n\r\n{}")))
            try { await GraphLspWire.ReadAsync(truncated, default); throw new Exception("Short frame accepted"); }
            catch (EndOfStreamException) { }
        foreach (var externalCancel in new[] { false, true })
        {
            using var cancellation = new CancellationTokenSource();
            int child;
            int parent;
            await using (var session = GraphLspSelfCheck.Start(root, "normal", cancellation.Token,
                externalCancel ? TimeSpan.FromSeconds(10) : TimeSpan.FromSeconds(2)))
            {
                parent = session.ProcessId;
                await session.InitializeAsync(root);
                child = (await session.RequestAsync("smsr/testChild", null)).GetInt32();
                if (externalCancel) cancellation.CancelAfter(200);
                try { await session.RequestAsync("smsr/hang", null); throw new Exception("LSP deadline ignored"); }
                catch (OperationCanceledException) { }
                catch (EndOfStreamException) when (session.Token.IsCancellationRequested) { }
                catch (IOException) when (session.Token.IsCancellationRequested) { }
            }
            foreach (var id in new[] { parent, child })
                try
                {
                    using var process = Process.GetProcessById(id);
                    await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(5));
                }
                catch (ArgumentException) { } // Already reaped.
        }
    }
}
