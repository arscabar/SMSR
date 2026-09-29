using System.IO;
using System.Text.Json;

namespace SMSR.App.Mvp;

internal sealed partial class GraphLspSession
{
    private bool _initialized;
    private bool _openClose;

    internal async Task InitializeAsync(string root, object? initializationOptions = null)
    {
        if (_initialized) throw new InvalidOperationException("LSP already initialized");
        _initialized = true;
        var result = await RequestAsync("initialize", new { processId = Environment.ProcessId,
            rootUri = new Uri(Path.GetFullPath(root)).AbsoluteUri, clientInfo = new { name = "SMSR" }, initializationOptions,
            capabilities = new {
                general = new { positionEncodings = new[] { "utf-16" } },
                textDocument = new { definition = new { dynamicRegistration = false, linkSupport = true } },
                workspace = new { configuration = true, applyEdit = false },
                window = new { workDoneProgress = true }
            }, trace = "off" });
        var caps = result.GetProperty("capabilities");
        if (caps.TryGetProperty("positionEncoding", out var encoding) && encoding.GetString() != "utf-16")
            throw new InvalidDataException("Unsupported LSP position encoding");
        if (!caps.TryGetProperty("definitionProvider", out var provider) ||
            provider.ValueKind is not (JsonValueKind.True or JsonValueKind.Object))
            throw new InvalidOperationException("Language server has no definition provider");
        if (caps.TryGetProperty("textDocumentSync", out var sync))
            _openClose = sync.ValueKind == JsonValueKind.Number ? sync.GetInt32() != 0 :
                sync.ValueKind == JsonValueKind.Object && sync.TryGetProperty("openClose", out var open) && open.GetBoolean();
        await NotifyAsync("initialized", new { });
    }

    internal async Task<JsonElement> DefinitionAsync(string uri, string languageId, string text, GraphLspPosition position)
    {
        if (!_initialized) throw new InvalidOperationException("LSP not initialized");
        GraphLspLocations.ValidatePosition(text, position);
        if (_openClose) await NotifyAsync("textDocument/didOpen", new { textDocument = new { uri, languageId, version = 1, text } });
        try { return await RequestAsync("textDocument/definition", new { textDocument = new { uri }, position }); }
        finally
        {
            if (_openClose && !Token.IsCancellationRequested && !_process.HasExited)
                await NotifyAsync("textDocument/didClose", new { textDocument = new { uri } });
        }
    }

    internal async Task ShutdownAsync()
    {
        await RequestAsync("shutdown", null);
        await NotifyAsync("exit", null);
        try { await _process.WaitForExitAsync(Token).WaitAsync(TimeSpan.FromSeconds(2), Token); }
        catch (TimeoutException)
        {
            // Only after shutdown was acknowledged: no pending analysis remains.
            Kill();
            await _process.WaitForExitAsync(Token).WaitAsync(TimeSpan.FromSeconds(5), Token);
        }
    }
}
