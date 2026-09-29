using System.IO;

namespace SMSR.App.Mvp;

internal static class GraphJdtCleanupSelfCheck
{
    internal static async Task RunAsync()
    {
        var root=Path.Combine(Path.GetTempPath(),"smsr-jdt-query-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var held=new FileStream(Path.Combine(root,"held"),FileMode.Create,FileAccess.ReadWrite,FileShare.None);
        var release=Task.Run(async()=>{await Task.Delay(100);await held.DisposeAsync();});
        await GraphJdtProject.DeleteAsync(root);await release;
        if(Directory.Exists(root)) throw new Exception("JDT temporary lock cleanup failed");
        try { await GraphJdtProject.DeleteAsync(Path.GetTempPath());throw new Exception("Broad cleanup accepted"); }
        catch(InvalidOperationException) { }
    }
}
