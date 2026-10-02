namespace SMSR.App.Mvp;

internal sealed class GraphRoleEvidenceCollector(EventStore store, string projectId, int revision)
{
    private readonly Dictionary<string, (string Text, string Hash, int Revision)> _sources = [];
    internal readonly List<GraphRoleEvidence> Evidence = [];
    internal readonly List<string> Unavailable = [];
    internal bool Truncated;
    internal async Task<string?> ReadAsync(string path, CancellationToken ct)
    {
        if (_sources.TryGetValue(path, out var cached)) return cached.Text;
        try
        {
            if (_sources.Count >= 8) { Truncated = true; return null; }
            var data = await new GraphSourceService(store).ReadAsync(projectId, path, ct);
            if (data.Revision != revision) throw new InvalidOperationException("색인 변경");
            _sources[path] = data; return data.Text;
        }
        catch (Exception e) when (e is ArgumentException or InvalidOperationException or KeyNotFoundException or System.IO.IOException)
        { if (!Unavailable.Contains(path)) Unavailable.Add(path); return null; }
    }
    internal async Task AddAsync(string path, int line, int length, GraphEdge? edge, CancellationToken ct)
    {
        if (Evidence.Count >= 13) { Truncated = true; return; }
        if (await ReadAsync(path, ct) is not { } text) return;
        var data = _sources[path];
        if (edge?.Evidence is { } extracted && !extracted.SourceHash.Equals(data.Hash, StringComparison.OrdinalIgnoreCase))
        { Unavailable.Add(path); return; }
        var lines = text.Replace("\r\n", "\n").Split('\n'); var first = Math.Max(1, line - 3);
        var selected = lines.Skip(first - 1).Take(length).ToArray();
        var quote = string.Join('\n', selected);
        if (quote.Length > 6000 || Evidence.Sum(e => e.Quote.Length) + quote.Length > 24000)
        { Truncated = true; return; }
        if (string.IsNullOrWhiteSpace(quote) || GraphDocumentRedaction.Excluded(quote))
        { Unavailable.Add(path); return; }
        if (edge is null && (first > 1 || lines.Length > first - 1 + length)) Truncated = true;
        if (Evidence.Any(e => e.Path == path && e.Line == first && e.Edge == edge)) return;
        Evidence.Add(new("e" + Evidence.Count, path, first, quote, data.Hash, edge));
    }
}
