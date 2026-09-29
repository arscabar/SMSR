using System.IO;
using System.Text;
using System.Text.Json;

namespace SMSR.App.Mvp;

internal static class GraphLspWire
{
    internal const int MaxBytes = 4 * 1024 * 1024;

    internal static async Task WriteAsync(Stream stream, object message, CancellationToken ct)
    {
        var body = JsonSerializer.SerializeToUtf8Bytes(message, GraphWorker.Json);
        if (body.Length > MaxBytes) throw new InvalidDataException("LSP frame too large");
        await stream.WriteAsync(Encoding.ASCII.GetBytes($"Content-Length: {body.Length}\r\n\r\n"), ct);
        await stream.WriteAsync(body, ct);
        await stream.FlushAsync(ct);
    }

    internal static async Task<JsonElement> ReadAsync(Stream stream, CancellationToken ct)
    {
        var header = new List<byte>();
        var one = new byte[1];
        while (true)
        {
            await stream.ReadExactlyAsync(one, ct);
            if (one[0] > 127 || header.Count >= 8192) throw new InvalidDataException("Invalid LSP header");
            header.Add(one[0]);
            if (header.Count >= 4 && header.TakeLast(4).SequenceEqual(new byte[] { 13, 10, 13, 10 })) break;
        }
        int? length = null;
        foreach (var line in Encoding.ASCII.GetString(header.ToArray()).Split("\r\n", StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = line.Split(':', 2);
            if (parts.Length != 2) throw new InvalidDataException("Invalid LSP header");
            if (parts[0].Equals("Content-Length", StringComparison.OrdinalIgnoreCase))
            {
                if (length is not null || !int.TryParse(parts[1].Trim(), out var size) || size is < 1 or > MaxBytes)
                    throw new InvalidDataException("Invalid LSP length");
                length = size;
            }
            if (parts[0].Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
                foreach (var parameter in parts[1].Split(';').Skip(1))
                {
                    var pair = parameter.Trim().Split('=', 2);
                    if (pair[0].Equals("charset", StringComparison.OrdinalIgnoreCase) &&
                        (pair.Length != 2 || pair[1].Trim().ToLowerInvariant() is not ("utf-8" or "utf8")))
                        throw new InvalidDataException("Unsupported LSP charset");
                }
        }
        var body = new byte[length ?? throw new InvalidDataException("Missing LSP length")];
        await stream.ReadExactlyAsync(body, ct);
        return JsonSerializer.Deserialize<JsonElement>(body);
    }
}
