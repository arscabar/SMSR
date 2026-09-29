using System.IO;
using System.Text.Json;

namespace SMSR.App.Mvp;

// Integration seam deliberately not exposed to HTTP/MCP until real server profiles are verified.
internal sealed class GraphLspDefinitions(EventStore store)
{
    internal async Task<(IReadOnlyList<GraphLspDefinition> Candidates, int Excluded)> QueryAsync(
        GraphLspSession session, string projectId, string path, string languageId, GraphLspPosition position,
        object? initializationOptions = null)
    {
        var ct = session.Token;
        var info = await store.GetGraphInfoAsync(projectId, ct) ?? throw new KeyNotFoundException("Graph not indexed");
        var sourceService = new GraphSourceService(store);
        var source = await sourceService.ReadAsync(projectId, path, ct);
        if (source.Revision != info.Revision) throw new InvalidOperationException("Graph changed");
        GraphLspLocations.ValidatePosition(source.Text, position);
        await session.InitializeAsync(info.RootPath, initializationOptions);
        var uri = new Uri(Path.GetFullPath(Path.Combine(info.RootPath, path))).AbsoluteUri;
        var response = await session.DefinitionAsync(uri, languageId, source.Text, position);
        var items = response.ValueKind switch {
            JsonValueKind.Null => [],
            JsonValueKind.Object => new[] { response },
            JsonValueKind.Array when response.GetArrayLength() <= 200 => response.EnumerateArray().ToArray(),
            _ => throw new InvalidDataException("Invalid or excessive LSP definitions")
        };
        var files = await store.GetGraphFilesAsync(projectId, ct, info.Revision);
        var hits = new HashSet<GraphLspDefinition>();
        var excluded = 0;
        foreach (var item in items)
        {
            var candidate = GraphLspLocations.Parse(item, info.RootPath, files.Keys);
            if (candidate is null) { excluded++; continue; }
            var (target, range) = candidate.Value;
            var current = await sourceService.ReadAsync(projectId, target, ct);
            if (current.Revision != info.Revision) throw new InvalidOperationException("Graph changed");
            GraphLspLocations.ValidatePosition(current.Text, range.Start);
            GraphLspLocations.ValidatePosition(current.Text, range.End);
            hits.Add(new(target, range, current.Hash));
        }
        await session.ShutdownAsync();
        foreach (var hit in hits)
            if ((await sourceService.ReadAsync(projectId, hit.Path, ct)).Hash != hit.Hash)
                throw new InvalidOperationException("Definition source changed during LSP query");
        if ((await sourceService.ReadAsync(projectId, path, ct)).Hash != source.Hash ||
            (await store.GetGraphInfoAsync(projectId, ct))?.Revision != info.Revision)
            throw new InvalidOperationException("Source or graph changed during LSP query");
        // Definition candidates are not proof of dispatch, alias resolution or safe data flow.
        return (hits.ToArray(), excluded);
    }
}
