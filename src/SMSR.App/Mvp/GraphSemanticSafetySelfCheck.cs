namespace SMSR.App.Mvp;

internal static class GraphSemanticSafetySelfCheck
{
    internal static async Task RunAsync(GraphSemanticService service, EventStore store, GraphSemanticRequest r)
    {
        var bad = new[] { r with { Nodes = [r.Nodes[0] with { Quote = "不存在" }] },
            r with { Edges = [r.Edges[0] with { TargetId = "missing" }] },
            r with { Edges = [r.Edges[0] with { Quote = "이벤트 이력을 저장한다." }] },
            r with { Nodes = [] }, r with { Nodes = [null!] },
            r with { Nodes = [r.Nodes[0], r.Nodes[0]] },
            r with { Nodes = [r.Nodes[0] with { Label = "api_key=unsafe-test-value" }] },
            r with { Groups = [r.Groups![0] with { Members = [new("events"),new("checks"),new("absent")] }] },
            r with { SourceHash = new('0',64) }, r with { ExpectedRevision = 0 },
            r with { ContractVersion = "unknown" } };
        foreach (var request in bad)
        {
            try { await service.SubmitAsync(request); throw new Exception("Unsafe semantic submission accepted"); }
            catch (ArgumentException) { }
            if ((await store.GetGraphInfoAsync("documents"))?.Revision != r.ExpectedRevision)
                throw new Exception("Rejected semantic input changed graph");
        }
    }
}
