using System.IO;
using System.Text;

namespace SMSR.App.Mvp;

internal static class GraphProcessOutput
{
    internal static async Task<string> ReadAsync(Stream stream, CancellationToken ct, int maxBytes = 16 * 1024 * 1024)
    {
        using var buffer = new MemoryStream();
        var chunk = new byte[8192];
        int read;
        while ((read = await stream.ReadAsync(chunk, ct)) != 0)
        {
            if (buffer.Length + read > maxBytes)
                throw new InvalidOperationException($"로컬 분석 출력이 {maxBytes / 1024 / 1024} MiB를 초과했습니다.");
            buffer.Write(chunk, 0, read);
        }
        return new UTF8Encoding(false, true).GetString(buffer.ToArray());
    }
}
