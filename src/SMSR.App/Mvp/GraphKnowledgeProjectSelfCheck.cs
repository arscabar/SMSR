using System.Diagnostics;
using System.IO;
using System.Text.Json;
using Microsoft.Data.Sqlite;
namespace SMSR.App.Mvp;
internal static class GraphKnowledgeProjectSelfCheck
{
    internal static async Task RunAsync(string output)
    {
        var root=Path.Combine(Path.GetTempPath(),"smsr-knowledge-project-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);
        try
        {
            var sourcePath=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"SMSR","smsr.db");
            using(var source=new SqliteConnection(new SqliteConnectionStringBuilder{DataSource=sourcePath,Mode=SqliteOpenMode.ReadOnly}.ToString()))
            using(var copy=new SqliteConnection($"Data Source={Path.Combine(root,"smsr.db")}")){source.Open();copy.Open();source.BackupDatabase(copy);}
            var store=new EventStore(Path.Combine(root,"smsr.db"));await store.InitializeAsync();
            var reports=new List<object>();var overview=new GraphOverviewService(store,new GraphWorker());
            foreach(var project in new[]{"SMSR","AO3.5_Main"})
            {
                var info=await store.GetGraphInfoAsync(project)??throw new Exception("Actual project graph missing");
                var timer=Stopwatch.StartNew();var data=await new GraphExportService(store,new GraphQueryService(store)).GetAsync(project);
                if(data.Nodes.Count!=info.NodeCount||data.Edges.Count!=info.EdgeCount)throw new Exception("Actual full export incomplete");
                var exportMs=timer.Elapsed.TotalMilliseconds;var bytes=JsonSerializer.SerializeToUtf8Bytes(data,GraphWorker.Json).Length;
                var query=new GraphQueryService(store);var times=new List<double>();
                foreach(var node in data.Nodes.Take(20)){timer.Restart();var scope=await query.ExportScopeAsync(project,node.NodeId,1,"both");if(scope.Revision!=info.Revision)throw new Exception("Scope revision changed");times.Add(timer.Elapsed.TotalMilliseconds);}
                var report=new GraphReportService(store,overview);timer.Restart();GraphReport result;
                using var limit=new CancellationTokenSource(TimeSpan.FromMinutes(9));
                do{result=await report.GetAsync(project,limit.Token);if(result.Status=="ANALYSIS_PENDING")await Task.Delay(1000,limit.Token);}while(result.Status=="ANALYSIS_PENDING");
                if(result.Status!="READY"||result.Revision!=info.Revision)throw new Exception("Actual project report failed");
                reports.Add(new{project,info.Revision,nodes=data.Nodes.Count,edges=data.Edges.Count,exportBytes=bytes,exportMs,scopeSamples=times.Count,scopeP95Ms=times.Order().ElementAt(18),reportMs=timer.Elapsed.TotalMilliseconds,groups=result.Overview!.TotalGroups,hostPeakBytes=Process.GetCurrentProcess().PeakWorkingSet64});
            }
            await File.WriteAllTextAsync(output,JsonSerializer.Serialize(new{reports,limitation="Production DB read-only snapshot. No reindex/source writes. Stored revision, not proof of current source. Host memory excludes workers."},GraphWorker.Json));
        }
        finally{SqliteConnection.ClearAllPools();Directory.Delete(root,true);}
    }
}
