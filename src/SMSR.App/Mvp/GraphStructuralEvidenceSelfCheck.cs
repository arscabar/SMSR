namespace SMSR.App.Mvp;

internal static class GraphStructuralEvidenceSelfCheck
{
    internal static void Run()
    {
        var file = new GraphFile("plan.md", new string('B', 64), "document");
        var edge = new GraphEdge("a", "b", "LINKS", file.Path, 7, "RESOLVED", "EXTRACTED");
        var supplied = edge with { Evidence = new("Graphify", "0.9.73", file.Hash) };
        var actual = GraphStructuralEvidence.Attach([edge, supplied], [file]);
        if (actual[0].Evidence?.SourceHash != file.Hash || actual[0].SourceLine != 7
            || actual[0].Evidence?.Analyzer != "SMSR" || actual[1] != supplied)
            throw new Exception("Structural source evidence lost or overwritten");
        GraphKnowledgeValidation.Evidence(actual[0].Evidence);
        try { GraphStructuralEvidence.Attach([edge], []); }
        catch (KeyNotFoundException) { return; }
        throw new Exception("Unknown structural owner accepted");
    }
}
