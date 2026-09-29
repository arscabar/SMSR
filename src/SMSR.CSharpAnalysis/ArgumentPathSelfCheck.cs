namespace SMSR.CSharpAnalysis;

internal static class ArgumentPathSelfCheck
{
    internal static void Verify(AnalysisOutput result)
    {
        var check=SemanticSelfCheck.Require;
        foreach(var summary in result.ReturnSummaries)
        {
            var function=result.Functions.Single(f=>f.SymbolId==summary.SymbolId);
            var graph=function.Definitions.Values;
            var valid=graph.Links.Concat(summary.Calls.SelectMany(c=>c.Links)).ToHashSet();
            foreach(var origin in summary.Arguments.Origins)
                foreach(var dependency in origin.Dependencies)
                {
                    var id=dependency.EntryValue; var seen=new HashSet<int>{id};
                    check(graph.Nodes[id].Kind=="ENTRY" && graph.Nodes[id].SymbolId==
                        function.Boundary.Parameters.Single(p=>p.Ordinal==dependency.ParameterOrdinal).Id,"wrong witness origin");
                    foreach(var edge in dependency.Path)
                    {
                        check(edge.Source==id && valid.Contains(edge) && seen.Add(edge.Target),"invalid/cyclic witness edge");
                        id=edge.Target;
                    }
                    check(id==origin.Value,"witness did not reach argument");
                }
        }
        var caller=result.Functions.Single(f=>f.SymbolId.Contains("C.Two("));
        var context=new ReturnContext(caller);
        var work=100_000; var evidence=0;
        try { ArgumentOrigins.Read(context,result.Connections,ref work,ref evidence); throw new Exception("evidence limit ignored"); }
        catch(ArgumentException) { }
    }
}
