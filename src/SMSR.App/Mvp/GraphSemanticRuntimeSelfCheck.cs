using System.Text.Json;
namespace SMSR.App.Mvp;
internal static class GraphSemanticRuntimeSelfCheck
{
    internal static async Task RunAsync(EventStore store,GraphSemanticService service,GraphSemanticReport saved)
    {
        if(saved.ExtractionFingerprint?.Length!=64)throw new Exception("Runtime provenance missing");
        async Task Write(GraphSemanticReport report)=>await store.SaveGraphDerivedAsync("documents","document-semantic",saved.Path,saved.Revision,JsonSerializer.Serialize(report,GraphWorker.Json));
        await Write(saved with{ExtractionFingerprint=new('0',64)});
        if(!(await service.ReadAsync("documents",saved.Path)).Stale)throw new Exception("Changed extraction runtime remained current");
        await Write(saved with{Nodes=saved.Nodes.Select(n=>n with{Quote="absent evidence"}).ToArray()});
        if(!(await service.ReadAsync("documents",saved.Path)).Stale)throw new Exception("Changed evidence remained current");
        await Write(saved with{ExtractionFingerprint=null});
        var fresh=await service.SubmitAsync(new("documents",saved.Path,saved.SourceHash,saved.Revision,saved.Model,saved.ContractVersion,saved.Nodes,saved.Edges,saved.Groups));
        if(fresh.Revision<=saved.Revision||fresh.ExtractionFingerprint!=saved.ExtractionFingerprint||(await service.ReadAsync("documents",saved.Path)).Stale)
            throw new Exception("Legacy runtime cache was reused instead of refreshed");
    }
}
