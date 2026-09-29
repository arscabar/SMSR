using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SMSR.App.Mvp;

public sealed partial class GraphAdvancedService
{
    private async Task<float[][]> EmbedAsync(string[] texts, bool full, CancellationToken ct)
    {
        if (texts.Length == 0) return [];
        var result = await worker.RunAsync(new { operation = "embed", texts, full }, ct);
        var vectors = result.GetProperty("vectors").Deserialize<float[][]>()!;
        if (vectors.Length != texts.Length || vectors.Any(v => v.Length != 384 || v.Any(x => !float.IsFinite(x))))
            throw new InvalidOperationException("벡터 모델 응답이 올바르지 않습니다.");
        return vectors;
    }

    private async Task<float[][]> EmbedCachedAsync(string project, string kind, string scope, int revision,
        string[] texts, bool full, CancellationToken ct)
    {
        var keys = texts.Select(text => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)))).ToArray();
        var version = VectorModel + (full ? ":full-v1" : ":metadata-v1");
        var saved = await store.GetGraphDerivedAsync(project, kind, scope, ct);
        var cache = saved is null ? null : JsonSerializer.Deserialize<GraphVectorCache>(saved);
        var vectors = cache?.Model == version ? cache.Vectors : new Dictionary<string, float[]>();
        var missing = keys.Select((key, i) => (key, text: texts[i])).DistinctBy(x => x.key)
            .Where(x => !vectors.ContainsKey(x.key)).ToArray();
        foreach (var batch in missing.Chunk(128))
        {
            var values = await EmbedAsync(batch.Select(x => x.text).ToArray(), full, ct);
            for (var i = 0; i < batch.Length; i++) vectors[batch[i].key] = values[i];
        }
        var live = keys.ToHashSet(StringComparer.Ordinal);
        await store.SaveGraphDerivedAsync(project, kind, scope, revision,
            JsonSerializer.Serialize(new GraphVectorCache(version, vectors.Where(pair => live.Contains(pair.Key)).ToDictionary())), ct);
        return keys.Select(key => vectors[key]).ToArray();
    }
}
