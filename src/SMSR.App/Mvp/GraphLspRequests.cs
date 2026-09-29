using System.IO;
using System.Text.Json;

namespace SMSR.App.Mvp;

internal sealed partial class GraphLspSession
{
    internal async Task<JsonElement> RequestAsync(string method, object? parameters)
    {
        var id = ++_nextId;
        try
        {
            await SendAsync(new { jsonrpc = "2.0", id, method, @params = parameters });
            var bytes = 0L;
            for (var count = 0; count < 4096; count++)
            {
                var message = await GraphLspWire.ReadAsync(_process.StandardOutput.BaseStream, Token);
                bytes += message.GetRawText().Length;
                if (bytes > 16 * 1024 * 1024 || message.ValueKind != JsonValueKind.Object ||
                    !message.TryGetProperty("jsonrpc", out var version) || version.GetString() != "2.0")
                    throw new InvalidDataException("Invalid LSP response");
                if (message.TryGetProperty("method", out var name))
                {
                    if (message.TryGetProperty("id", out var serverId))
                        await ReplyAsync(serverId, name.GetString(), message);
                    continue; // Notifications are not logged or retained.
                }
                if (!message.TryGetProperty("id", out var responseId) ||
                    !responseId.TryGetInt32(out var number) || number != id)
                    throw new InvalidDataException("Unexpected LSP response id");
                if (message.TryGetProperty("error", out _))
                    throw new InvalidOperationException("Language server rejected request");
                return message.GetProperty("result").Clone();
            }
            throw new InvalidDataException("LSP message limit exceeded");
        }
        catch { Kill(); throw; }
    }

    private Task ReplyAsync(JsonElement id, string? method, JsonElement request)
    {
        object? result;
        switch (method)
        {
            case "workspace/configuration":
                var count = request.GetProperty("params").GetProperty("items").GetArrayLength();
                if (count > 1024) throw new InvalidDataException("LSP configuration limit exceeded");
                result = new object?[count]; break;
            case "window/workDoneProgress/create": result = null; break;
            case "workspace/applyEdit": result = new { applied = false, failureReason = "SMSR is read-only" }; break;
            default:
                return SendAsync(new { jsonrpc = "2.0", id,
                    error = new { code = -32601, message = "Method not supported" } });
        }
        return SendAsync(new { jsonrpc = "2.0", id, result });
    }
}
