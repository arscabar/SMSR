using System.Diagnostics;
using System.IO;
using System.IO.Compression;

namespace SMSR.App.Mvp;

internal static class GraphDocumentSelfCheck
{
    internal static async Task RunAsync()
    {
        var root = Path.Combine(Path.GetTempPath(), "smsr-document-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            using (var git = Process.Start(new ProcessStartInfo("git") { ArgumentList = { "-C", root, "init", "-q" }, UseShellExecute = false, CreateNoWindow = true })!)
            { await git.WaitForExitAsync(); if (git.ExitCode != 0) throw new Exception("Git fixture failed"); }
            Directory.CreateDirectory(Path.Combine(root, "src"));
            await File.WriteAllTextAsync(Path.Combine(root, "src/service.py"), "def save():\n return True\n");
            await File.WriteAllTextAsync(Path.Combine(root, "plan.md"), "이벤트 이력을 저장한다.\n검증 근거가 필요하다.\nsrc/service.py는 이벤트 저장을 담당한다.\n오류 시 이전 결과를 유지한다.\n");
            await File.WriteAllTextAsync(Path.Combine(root, "visible.html"), "<p>문서 내용</p><script>hidden</script>");
            using (var zip = ZipFile.Open(Path.Combine(root, "plan.docx"), ZipArchiveMode.Create))
            using (var text = new StreamWriter(zip.CreateEntry("word/document.xml").Open()))
                await text.WriteAsync("<document><body><p><t>Office requirement</t></p></body></document>");
            var store = new EventStore(Path.Combine(root, "smsr.db")); await store.InitializeAsync();
            var index = new GraphIndexService(store); await index.IndexAsync("documents", root);
            var documents = new GraphDocumentService(store, new GraphWorker());
            if ((await documents.ReadAsync("documents", "plan.docx")).Blocks.Single().Location != "paragraph:1"
                || (await documents.ReadAsync("documents", "visible.html")).Blocks.Any(b => b.Text.Contains("hidden")))
                throw new Exception("Binary document indexing/HTML active-content exclusion failed");
            await GraphSemanticSelfCheck.RunAsync(root, store, index, documents);
            await GraphSemanticLifecycleSelfCheck.RunAsync(root, store, index, documents);
            await GraphSemanticAgentSelfCheck.RunAsync(root, store, index, documents);
            await GraphDocumentHttpSelfCheck.RunAsync(root);
            await GraphOverviewSelfCheck.RunAsync(root,store);
            await GraphQuestionSelfCheck.RunAsync(root,store);
            await GraphRemainingSelfCheck.RunAsync(root,store);
            await GraphMediaAnalysisSelfCheck.RunAsync(root,store,index);
            await GraphSkillFlowSelfCheck.RunAsync(root);
            await GraphMediaAgentSelfCheck.RunAsync(root,store,index);
            await GraphSpeechAgentSelfCheck.RunAsync(root,store,index);
        }
        finally { Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools(); Directory.Delete(root, true); }
    }
}
