namespace SMSR.App.Mvp;

internal static class GraphStructuralEvidence
{
    internal static GraphEdge[] Attach(IEnumerable<GraphEdge> edges, IEnumerable<GraphFile> files)
    {
        var hashes = files.ToDictionary(file => file.Path, file => file.Hash, StringComparer.Ordinal);
        return edges.Select(edge => edge.Evidence is not null ? edge : edge with
        {
            Evidence = new GraphSourceEvidence("SMSR", EventStore.GraphFormat, hashes[edge.OwnerPath])
        }).ToArray();
    }
}
