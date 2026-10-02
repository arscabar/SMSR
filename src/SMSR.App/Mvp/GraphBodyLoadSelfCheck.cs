using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace SMSR.App.Mvp;

internal static class GraphBodyLoadSelfCheck
{
    internal static async Task<object> RunAsync(EventStore store, GraphAdvancedService service)
    {
        var root = Path.Combine(Path.GetTempPath(), "smsr-body-load-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var files = new List<GraphFile>();
            for (var i = 0; i < 200; i++)
            {
                var text = i == 173 ? "온라인 결제 승인과 카드 환불 절차를 안내합니다. 결제 실패 시 주문을 취소합니다."
                    : $"Garden plant {i}: banana seeds, water and soil. Growing vegetables in a sunny garden.";
                var path = $"item{i:D3}.txt";
                await File.WriteAllTextAsync(Path.Combine(root, path), text, new UTF8Encoding(false));
                files.Add(new(path, Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text))), "document"));
            }
            await store.ApplyGraphScanAsync("body-load", new(root, files,
                files.Select(f => new GraphNode("file:" + f.Path, f.Path, f.Kind, f.Path, f.Path, 1, f.Hash)).ToArray(),
                [], [], files.Select(f => f.Path).ToArray(), [], []), false);
            var times = new List<double>();
            for (var i = 0; i < 2; i++)
            {
                var timer = Stopwatch.StartNew();
                var result = await service.BodySemanticAsync("body-load", "payment authorization and refund");
                times.Add(timer.Elapsed.TotalMilliseconds);
                if (result.Files != 200 || result.Chunks != 200 || result.Hits[0].Path != "item173.txt")
                    throw new Exception("Body load ranking/count mismatch");
            }
            var cache = (await store.GetGraphDerivedAsync("body-load", "body-vectors", ""))!;
            if (cache.Contains("Garden") || cache.Contains("payment") || cache.Contains("환불"))
                throw new Exception("Raw body/query persisted in vector cache");
            return new { files = 200, chunks = 200, coldMs = times[0], warmMs = times[1], rawTextPersisted = false };
        }
        finally { await store.DeleteProjectAsync("body-load"); Directory.Delete(root, true); }
    }
}
