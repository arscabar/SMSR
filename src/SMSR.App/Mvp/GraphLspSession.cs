using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace SMSR.App.Mvp;

// Internal only: command/arguments must come from trusted application code, never a repository or API.
// ponytail: one sequential session per analysis; shared multiplexing needs measured demand.
internal sealed partial class GraphLspSession : IAsyncDisposable
{
    private readonly Process _process;
    private readonly GraphLspJob _job;
    private readonly Task _stderr;
    private readonly CancellationTokenSource _deadline;
    private readonly CancellationTokenRegistration _cancel;
    private int _nextId;
    private bool _disposed;
    private readonly object _killGate = new();
    private bool _killed;
    internal int ProcessId => _process.Id;
    internal CancellationToken Token => _deadline.Token;

    internal GraphLspSession(string executable, IEnumerable<string> arguments, string root,
        CancellationToken ct, TimeSpan? timeout = null)
    {
        if (!Path.IsPathFullyQualified(executable) || !File.Exists(executable) ||
            !Path.IsPathFullyQualified(root) || !Directory.Exists(root))
            throw new ArgumentException("LSP requires trusted absolute executable and root paths");
        ct.ThrowIfCancellationRequested();
        var start = new ProcessStartInfo(executable) { WorkingDirectory = root,
            UseShellExecute = false, CreateNoWindow = true, RedirectStandardInput = true,
            RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        foreach(var key in new[]{"JAVA_TOOL_OPTIONS","JDK_JAVA_OPTIONS","_JAVA_OPTIONS","CLASSPATH",
            "CLIENT_HOST","CLIENT_PORT","CLIENT_PIPE","SERVER_HOST","SERVER_PORT"}) start.Environment.Remove(key);
        _deadline = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _deadline.CancelAfter(timeout ?? TimeSpan.FromMinutes(2));
        _job = new GraphLspJob();
        Process? process = null;
        try
        {
            process = Process.Start(start) ?? throw new IOException("LSP start failed");
            _job.Assign(process);
            _process = process;
        }
        catch
        {
            _job.Dispose();
            try { process?.Kill(true); } catch (InvalidOperationException) { }
            process?.Dispose();
            _deadline.Dispose();
            throw;
        }
        _cancel = Token.Register(Kill);
        _stderr = _process.StandardError.BaseStream.CopyToAsync(Stream.Null);
    }

    private void Kill()
    {
        lock (_killGate)
        {
            if (_killed) return;
            _killed = true;
            _job.Terminate();
            try { if (!_process.HasExited) _process.Kill(entireProcessTree: true); }
            catch (InvalidOperationException) { }
        }
    }

    private Task SendAsync(object value) => GraphLspWire.WriteAsync(_process.StandardInput.BaseStream, value, Token);

    internal Task NotifyAsync(string method, object? parameters) =>
        SendAsync(new { jsonrpc = "2.0", method, @params = parameters });

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        Kill();
        try { await _process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(5)); }
        finally
        {
            _cancel.Dispose();
            _deadline.Dispose();
            _process.StandardError.Close();
            try { await _stderr.WaitAsync(TimeSpan.FromSeconds(1)); }
            catch (Exception e) when (e is IOException or ObjectDisposedException or TimeoutException) { }
            _process.Dispose();
            try { await _job.WaitForEmptyAsync(); }
            finally { _job.Dispose(); }
        }
    }
}
