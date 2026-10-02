using System.IO;
using Microsoft.Data.Sqlite;
namespace SMSR.App.Mvp;
internal static class GraphWatchBusySelfCheck
{
    internal static async Task RunAsync(string root,EventStore store)
    {
        var revision=(await store.GetGraphInfoAsync("documents"))!.Revision;
        using var service=new GraphWatchService(store,new GraphIndexService(store));
        await using(var connection=new SqliteConnection(new SqliteConnectionStringBuilder{DataSource=Path.Combine(root,"smsr.db"),Pooling=false}.ToString()))
        {
            await connection.OpenAsync();using var command=connection.CreateCommand();command.CommandText="BEGIN IMMEDIATE;";await command.ExecuteNonQueryAsync();
            try{await service.SetAsync("documents",true);throw new Exception("Locked watch configuration unexpectedly saved");}
            catch(SqliteException error)when(error.SqliteErrorCode==5){}
            finally{command.CommandText="ROLLBACK;";await command.ExecuteNonQueryAsync();}
        }
        if(service.Get("documents").Enabled||(await store.GetGraphInfoAsync("documents"))!.Revision!=revision)
            throw new Exception("Failed watch configuration retained a live watcher or changed index");
        await service.SetAsync("documents",true);if(!service.Get("documents").Enabled)throw new Exception("Watch did not recover after database unlock");
        await service.SetAsync("documents",false);
    }
}
