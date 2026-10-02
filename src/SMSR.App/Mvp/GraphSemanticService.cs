using System.Text.Json;

namespace SMSR.App.Mvp;

public sealed partial class GraphSemanticService(EventStore store, GraphDocumentService documents)
{
    public async Task<GraphSemanticStatus> ReadAsync(string projectId, string path, CancellationToken ct = default)
    {
        var saved = await store.GetGraphDerivedAsync(projectId, "document-semantic", path, ct)
            ?? throw new KeyNotFoundException("저장된 문서 의미 분석이 없습니다.");
        var report = JsonSerializer.Deserialize<GraphSemanticReport>(saved, GraphWorker.Json)!;
        var files = await store.GetGraphFilesAsync(projectId, ct);
        var stale = report.ContractVersion != "semantic-v1" || report.Dependencies.Any(p => !files.TryGetValue(p.Key, out var hash) || hash != p.Value);
        var present = await store.GetGraphNodesAsync(projectId, report.Nodes.Select(n => GraphSemanticCache.Identity(path, n.Id)).ToArray(), ct);
        stale |= present.Count != report.Nodes.Length;
        try
        {
            foreach (var dependency in report.Dependencies)
                if (dependency.Key == path || GraphDocumentService.Binary(dependency.Key))
                {
                    var doc=await documents.ReadAsync(projectId,dependency.Key,ct);
                    stale |= doc.SourceHash!=dependency.Value;
                    if(dependency.Key==path)
                    {
                        stale |= report.ExtractionFingerprint!=doc.ExtractionFingerprint;
                        foreach(var node in report.Nodes)GraphSemanticValidation.Evidence(doc,node.Location,node.Quote);
                        foreach(var edge in report.Edges)GraphSemanticValidation.Evidence(doc,edge.Location,edge.Quote);
                        foreach(var group in report.Groups??[])GraphSemanticValidation.Evidence(doc,group.Location,group.Quote);
                    }
                }
                else await new GraphSourceService(store).ReadAsync(projectId, dependency.Key, ct);
        }
        catch (Exception e) when (e is System.IO.IOException or InvalidOperationException or KeyNotFoundException or ArgumentException) { stale = true; }
        return new(report, stale, stale ? "STALE_RETRY_REQUIRED" : "CURRENT");
    }

}
