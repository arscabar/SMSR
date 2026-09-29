namespace SMSR.CSharpAnalysis;

internal static class DefinitionSelfCheck
{
    internal static void Run()
    {
        var result = Analyzer.Run(new([new("Defs.cs", """
            class C {
                int Kill(int x) {
                    x = 1;
                    x = 2;
                    return x;
                }
                int Merge(bool c, int x) {
                    if (c) x = 1;
                    else x = 2;
                    return x;
                }
                int Loop(int x) {
                    while (x < 9) x++;
                    return x;
                }
                int Order(int x) {
                    x += (x = 2);
                    return x;
                }
                int Dead(int x) {
                    return x;
                    x = 5;
                }
                int Nested(int x) {
                    return (x = x + 1) + x;
                }
                int EndlessArm(bool c, int x) { if (c) while (true) { x = 1; } return x; }
            }
            """)]));
        var check = SemanticSelfCheck.Require;
        check(result.Status == "BOUND_INPUT_BUNDLE", "definitions fixture invalid");
        DefinitionFlow Get(string name) => result.Functions.Single(f => f.SymbolId.Contains("C." + name + "(")).Definitions;
        foreach (var f in result.Functions) check(f.Definitions.Status == "MAY_REACHING_DEFINITIONS", "definitions not computed");
        var kill = Get("Kill");
        check(kill.Links.Length == 1 && kill.Sites[kill.Links[0].Definition].Source.Start.Line == 3,
            "overwritten definitions reached return");
        var merge = Get("Merge");
        var read = merge.Sites.Last(s => s.Kind == "READ").Id;
        check(merge.Links.Count(l => l.Read == read) == 2 && merge.Links.Where(l => l.Read == read)
            .All(l => merge.Sites[l.Definition].Kind == "WRITE"), "merge did not union both branches");
        var loop = Get("Loop");
        check(loop.Sites.Where(s => s.Kind == "READ").All(s => loop.Links.Count(l => l.Read == s.Id) == 2), "loop fixed point lost backedge");
        var order = Get("Order");
        check(order.Links.Length == 2 && order.Sites[order.Links[0].Definition].Kind == "ENTRY" &&
            order.Links[1].Definition == order.Sites.Last(s => s.Kind == "WRITE").Id, "compound evaluation order wrong");
        check(Get("Dead").Sites.All(s => s.Kind != "WRITE"), "unreachable definition included");
        var nested = Get("Nested");
        check(nested.Sites[nested.Links.Last().Definition].Kind == "WRITE", "nested assignment order lost");
        var endless = Get("EndlessArm");
        check(endless.Sites[endless.Links.Single(l=>l.Read==endless.Sites.Last().Id).Definition].Kind=="ENTRY",
            "constant-impossible loop exit carried definition");
        DefinitionBoundarySelfCheck.Run();
    }
}
