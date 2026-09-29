using System.IO;
using System.Text;

namespace SMSR.App.Mvp;

internal static class GraphCrossRepoRetrySelfCheck
{
    public static async Task RunAsync(EventStore store, string root, string otherRoot)
    {
        var nfc = Path.Combine(otherRoot, "충돌.md");
        var nfd = Path.Combine(otherRoot, "충돌.md".Normalize(NormalizationForm.FormD));
        await File.WriteAllTextAsync(nfc, "# A");
        await File.WriteAllTextAsync(nfd, "# B");
        await File.WriteAllTextAsync(Path.Combine(root, "retry.md"), $"[target](../{Path.GetFileName(otherRoot)}/target.md)");
        var index = new GraphIndexService(store);
        try
        {
            await index.IndexAsync("sample", root);
            throw new Exception("저장소 간 충돌 실패를 감지하지 못함");
        }
        catch (InvalidOperationException error) when (error.Message.Contains("저장됐지만")) { }
        var query = new GraphQueryService(store);
        if (!(await query.CrossRepoAsync("sample")).Stale) throw new Exception("실패한 연결이 최신으로 표시됨");
        File.Delete(nfc);
        File.Delete(nfd);
        var retried = await index.IndexAsync("sample", root);
        var map = await query.CrossRepoAsync("sample");
        if (!retried.Unchanged || map.Stale || !map.Edges.Any(edge => edge.OwnerPath == "retry.md"))
            throw new Exception("변경 없는 색인에서 연결 재시도 실패");
    }
}
