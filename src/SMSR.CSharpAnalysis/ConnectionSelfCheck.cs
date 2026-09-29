namespace SMSR.CSharpAnalysis;

internal static class ConnectionSelfCheck
{
    internal static void Run()
    {
        var output = Analyzer.Run(new([
            new("Library.cs", """
                public static class Lib {
                    public static T Identity<T>(T value) => value;
                    public static int Choose(int left, int right = 7) { if (left > 0) return left; return right; }
                    public static int Sum(params int[] values) => values.Length;
                    public static int Extension(this int value, int add) => value + add;
                }
                """),
            new("Entry.cs", """
                class Entry {
                    int Run(int input) {
                        var a = Lib.Identity(input);
                        var b = Lib.Choose(right: 2, left: a);
                        var c = Lib.Choose(b);
                        var d = Lib.Sum(c, 4, 5);
                        return d.Extension(6);
                    }
                    int Recursive(int x) => x > 0 ? Recursive(x - 1) : 0;
                }
                """)]));
        var check = SemanticSelfCheck.Require;
        check(output.Status == "BOUND_INPUT_BUNDLE", "connection fixture invalid");
        check(output.Connections.All(c => c.Status == "BOUNDARY_LINKED"), "direct body not connected");
        foreach (var link in output.Connections)
        {
            var body = output.Functions.Single(f => f.SymbolId == link.TargetId);
            check(link.Inputs.All(a => body.Boundary.Parameters.Any(p => p.Id == a.ParameterId)), "dangling parameter");
            check(link.Returns.All(r => r.Source.Path == body.Source.Path && r.CallSource == link.Source), "return context mixed");
        }
        var choose = output.Connections.Where(c => c.TargetId!.Contains("Choose")).ToArray();
        check(choose[0].Inputs.Select(a => a.Argument.Ordinal).SequenceEqual(new[] { 1, 0 }), "named order lost");
        check(choose[1].Inputs.Any(a => a.Argument.Kind == "DefaultValue" && a.Argument.Implicit), "default argument missing");
        check(choose.All(c => c.Returns.Length == 2), "multiple return sites missing");
        check(output.Connections.Single(c => c.TargetId!.Contains("Sum")).Inputs.Single().Argument.ElementSources.Length == 3,
            "expanded params element evidence missing");
        check(output.Connections.Single(c => c.TargetId!.Contains("Extension")).Inputs.Length == 2, "extension receiver mapping missing");
        check(output.Connections.Any(c => c.CallerId == c.TargetId && c.Returns.Length > 0), "recursive context missing");
        ConnectionBoundarySelfCheck.Run();
        ConnectionLimitSelfCheck.Run();
    }
}
