using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SMSR.App.Mvp;

internal static class GraphBodySelfCheck
{
    public static async Task RunAsync(EventStore store, string root, GraphAdvancedService service)
    {
        var sources = new Dictionary<string, string> {
            ["a.txt"] = "Banana pancakes and delicious recipes for breakfast.",
            ["b.txt"] = "온라인 결제 승인과 카드 환불 절차를 안내합니다. 결제 실패 시 주문을 취소합니다.",
            ["c.txt"] = "api_key = \"sk-abcdefghijklmnopqrstuvwxyz\""
        };
        async Task Save()
        {
            var files = new List<GraphFile>();
            foreach (var (path, text) in sources)
            {
                await File.WriteAllTextAsync(Path.Combine(root, path), text, new UTF8Encoding(false));
                files.Add(new(path, Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text))), "code"));
            }
            await store.ApplyGraphScanAsync("body", new(root, files,
                files.Select(f => new GraphNode("file:" + f.Path, f.Path, f.Kind, f.Path, f.Path, 1, f.Hash)).ToArray(),
                [], [], files.Select(f => f.Path).ToArray(), [], []), false);
        }
        await Save();
        var result = await service.BodySemanticAsync("body", "payment authorization and refund");
        if (result.Hits[0].Path != "b.txt" || result.ExcludedFiles != 1 || result.Hits.Any(h => h.Path == "c.txt"))
            throw new Exception("본문 의미 순위·민감 파일 제외 실패");
        var cache = (await store.GetGraphDerivedAsync("body", "body-vectors", ""))!;
        if (cache.Contains("Banana") || cache.Contains("payment") || cache.Contains("sk-"))
            throw new Exception("본문·질문 원문 저장 금지 실패");
        var scoped = await service.BodySemanticAsync("body", "refund", "a.txt");
        if (scoped.Hits.Any(h => h.Path != "a.txt")) throw new Exception("본문 검색 범위 실패");
        await File.AppendAllTextAsync(Path.Combine(root, "b.txt"), "changed");
        try { await service.BodySemanticAsync("body", "refund"); throw new Exception("오래된 본문 허용"); }
        catch (InvalidOperationException) { }
        sources["b.txt"] = "Coffee beans and roasting temperatures.";
        await Save();
        await service.BodySemanticAsync("body", "coffee");
        var after = (await store.GetGraphDerivedAsync("body", "body-vectors", ""))!;
        var oldKeys = JsonSerializer.Deserialize<GraphVectorCache>(cache)!.Vectors.Keys;
        if (oldKeys.All(JsonSerializer.Deserialize<GraphVectorCache>(after)!.Vectors.ContainsKey))
            throw new Exception("변경된 본문 벡터 정리 실패");
        await store.DeleteProjectAsync("body");
        if (await store.GetGraphDerivedAsync("body", "body-vectors", "") is not null)
            throw new Exception("본문 파생 데이터 삭제 실패");
    }
}
