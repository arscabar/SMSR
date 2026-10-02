using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
namespace SMSR.App.Mvp;
internal static class GraphFeaturePreview
{
    internal static async Task RunAsync(string reportPath)
    {
        var root=Path.Combine(Path.GetTempPath(),"smsr-feature-preview-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);
        try
        {
            using(var git=Process.Start(new ProcessStartInfo("git"){ArgumentList={"-C",root,"init","-q"},UseShellExecute=false,CreateNoWindow=true})!){await git.WaitForExitAsync();if(git.ExitCode!=0)throw new Exception("Preview Git init failed");}
            Directory.CreateDirectory(Path.Combine(root,"src"));
            await File.WriteAllTextAsync(Path.Combine(root,"src/service.py"),"def save():\n return True\ndef audit():\n return save()\n");
            await File.WriteAllTextAsync(Path.Combine(root,"plan.md"),"이벤트 이력을 저장한다.\n검증 근거가 필요하다.\nsrc/service.py는 이벤트 저장을 담당한다.\n오류 시 이전 결과를 유지한다.\n");
            File.Copy(Path.Combine(Environment.CurrentDirectory,"docs/test-data/media-semantic-flow.png"),Path.Combine(root,"flow.png"));
            foreach(var file in new[]{"scan.pdf","speech.mp4","english.wav"})File.Copy(Path.Combine(Environment.CurrentDirectory,"artifacts/media-acceptance",file),Path.Combine(root,file));
            using(var zip=ZipFile.Open(Path.Combine(root,"plan.docx"),ZipArchiveMode.Create))using(var text=new StreamWriter(zip.CreateEntry("word/document.xml").Open()))await text.WriteAsync("<document><body><p><t>Office evidence preview</t></p></body></document>");
            var store=new EventStore(Path.Combine(root,"smsr.db"));await store.InitializeAsync();await new GraphIndexService(store).IndexAsync("documents",root);
            var worker=new GraphWorker();var docs=new GraphDocumentService(store,worker);var semantic=new GraphSemanticService(store,docs);
            await semantic.SubmitAsync(GraphSemanticFixture.Request(await docs.ReadAsync("documents","plan.md")));
            await GraphOverviewScaleFixture.CreateAsync(store,root);
            await GraphVisualPreviewFixture.CreateAsync(store,root);
            await GraphRolePreviewFixture.CreateAsync(store);
            await using var host=await LocalServer.StartAsync(root,0);
            await File.WriteAllTextAsync(reportPath,JsonSerializer.Serialize(new{address=host.Address,projectId="documents",root,pid=Environment.ProcessId},GraphWorker.Json));
            var until=DateTimeOffset.UtcNow.AddMinutes(30);
            while(DateTimeOffset.UtcNow<until&&!File.Exists(reportPath+".stop"))await Task.Delay(1000);
        }
        finally{Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();Directory.Delete(root,true);}
    }
}
