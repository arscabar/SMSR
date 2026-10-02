using System.Text.Json;

namespace SMSR.App.Mvp;

internal static class GraphSemanticCache
{
    internal static async Task<GraphSemanticReport?> FindAsync(EventStore store, GraphSemanticRequest request,
        Dictionary<string, string> dependencies,string? extractionFingerprint, CancellationToken ct)
    {
        var info = await store.GetGraphInfoAsync(request.ProjectId, ct);
        if (info?.Revision != request.ExpectedRevision) throw new InvalidOperationException("의미 캐시 확인 중 색인이 변경되었습니다.");
        var raw = await store.GetGraphDerivedAsync(request.ProjectId, "document-semantic", request.Path, ct);
        if (raw is null) return null;
        var saved = JsonSerializer.Deserialize<GraphSemanticReport>(raw, GraphWorker.Json)!;
        bool Same<T>(T a, T b) => JsonSerializer.Serialize(a, GraphWorker.Json) == JsonSerializer.Serialize(b, GraphWorker.Json);
        var present = await store.GetGraphNodesAsync(request.ProjectId,
            saved.Nodes.Select(n => Identity(request.Path, n.Id)).ToArray(), ct, info.Revision);
        return present.Count == saved.Nodes.Length && saved.SourceHash == request.SourceHash && saved.ExtractionFingerprint==extractionFingerprint && saved.Model == request.Model && saved.ContractVersion == request.ContractVersion
            && Same(saved.Nodes, request.Nodes) && Same(saved.Edges, request.Edges) && Same(saved.Groups ?? [], request.Groups ?? [])
            && saved.Dependencies.Count == dependencies.Count && dependencies.All(p => saved.Dependencies.TryGetValue(p.Key, out var hash) && hash == p.Value)
            ? saved : null;
    }
    internal static string Identity(string path, string id) => "semantic:" + Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
        System.Text.Encoding.UTF8.GetBytes(path + "\0" + id)));
}
