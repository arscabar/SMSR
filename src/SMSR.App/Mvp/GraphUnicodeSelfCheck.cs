using System.IO;
using System.Text;

namespace SMSR.App.Mvp;

internal static class GraphUnicodeSelfCheck
{
    public static async Task RunAsync(EventStore store, string root, GraphIndexService index)
    {
        var decomposed = "한글.md".Normalize(NormalizationForm.FormD);
        await File.WriteAllTextAsync(Path.Combine(root, decomposed), "# Unicode\n");
        await File.WriteAllTextAsync(Path.Combine(root, "unicode.md"), "[한글](한글.md)\n");
        var before = await index.IndexAsync("case", root);
        var query = new GraphQueryService(store);
        var path = await query.PathAsync("case", "file:unicode.md", "file:" + decomposed);
        var source = await new GraphSourceService(store).GetAsync("case", decomposed, 1);
        if (!path.Found || source.Lines[0] != "# Unicode") throw new Exception("Unicode 원본 경로 보존 실패");
        await File.WriteAllTextAsync(Path.Combine(root, "한글.md"), "# collision\n");
        var rejected = false;
        try { await index.IndexAsync("case", root); }
        catch (InvalidOperationException error) when (error.Message.Contains("정규화")) { rejected = true; }
        if (!rejected || (await store.GetGraphInfoAsync("case"))!.Revision != before.Revision)
            throw new Exception("Unicode 충돌 차단·기존 색인 보존 실패");
        File.Delete(Path.Combine(root, "한글.md"));
    }
}
