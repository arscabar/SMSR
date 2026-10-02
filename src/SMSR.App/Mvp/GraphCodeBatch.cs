using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Encodings.Web;

namespace SMSR.App.Mvp;

internal sealed class GraphCodeBatch : IDisposable
{
    internal string Folder { get; } = Path.Combine(Path.GetTempPath(), "smsr-code-batch-" + Guid.NewGuid().ToString("N"));
    internal int Count { get; private set; }

    internal static async Task<GraphCodeBatch> CreateAsync(string root,
        IReadOnlyList<GraphSourceFile> sources, CancellationToken ct)
    {
        var batch = new GraphCodeBatch();
        Directory.CreateDirectory(batch.Folder);
        try
        {
            long total = 0;
            foreach (var chunk in sources.Where(item => GraphCodeIndexInput.AffectsCode(item.File.Path)).Chunk(8))
            {
                var files = await GraphCodeIndexInput.ReadAsync(root, chunk, ct);
                var raw = JsonSerializer.SerializeToUtf8Bytes(files, new JsonSerializerOptions(GraphWorker.Json)
                    { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
                total += raw.Length;
                if (raw.Length > 32 * 1024 * 1024 || total > 512L * 1024 * 1024)
                    throw new InvalidOperationException("대형 코드 배치가 512 MiB 직렬화 한도를 초과했습니다. 기존 색인은 유지됩니다.");
                await File.WriteAllBytesAsync(Path.Combine(batch.Folder, batch.Count++ + ".bin"),
                    ProtectedData.Protect(raw, null, DataProtectionScope.CurrentUser), ct);
            }
            return batch;
        }
        catch { batch.Dispose(); throw; }
    }

    internal static async Task VerifyAsync(string root, IReadOnlyList<GraphSourceFile> sources, CancellationToken ct)
    {
        foreach (var chunk in sources.Where(item => GraphCodeIndexInput.AffectsCode(item.File.Path)).Chunk(8))
            await GraphCodeIndexInput.ReadAsync(root, chunk, ct);
    }

    public void Dispose()
    {
        if (Directory.Exists(Folder)) Directory.Delete(Folder, true);
    }
}
