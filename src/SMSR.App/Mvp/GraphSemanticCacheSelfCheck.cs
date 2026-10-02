using System.IO;
namespace SMSR.App.Mvp;
internal static class GraphSemanticCacheSelfCheck
{
    internal static async Task RunAsync(string root,GraphIndexService index,GraphDocumentService docs,
        GraphSemanticService service,GraphSemanticRequest request,GraphSemanticReport saved,string source)
    {
        var changed=await service.SubmitAsync(request with {ExpectedRevision=saved.Revision,Model="different-host-fixture"});
        if(changed.Revision<=saved.Revision||changed.Model==saved.Model)throw new Exception("Changed analyzer reused old cache");
        File.Delete(Path.Combine(root,request.Path));await index.IndexAsync(request.ProjectId,root,allowLargeReduction:true);
        if(!(await service.ReadAsync(request.ProjectId,request.Path)).Stale)throw new Exception("Deleted document cache current");
        File.Copy(source,Path.Combine(root,request.Path));await index.IndexAsync(request.ProjectId,root);
        var restored=await docs.ReadAsync(request.ProjectId,request.Path);
        var fresh=await service.SubmitAsync(request with {ExpectedRevision=restored.Revision});
        if(fresh.Revision<=restored.Revision)throw new Exception("Reverted file reused missing semantic facts");
    }
}
