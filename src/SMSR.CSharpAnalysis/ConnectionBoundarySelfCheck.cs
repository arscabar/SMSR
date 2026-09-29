using System.Text.Json;

namespace SMSR.CSharpAnalysis;

internal static class ConnectionBoundarySelfCheck
{
    internal static void Run()
    {
        var output = Analyzer.Run(new([new("Boundary.cs", """
            using System;
            using System.Threading.Tasks;
            using System.Collections.Generic;
            using System.Diagnostics;
            class C {
                int value;
                public C(int x) { value = x; }
                public int Direct(int x) => x;
                public virtual int Virtual(int x) => x;
                static void Update(ref int x, out int y, in int z) { x++; y = z; }
                static ref int Alias(ref int x) => ref x;
                static async Task<int> Later(int x) { await Task.Yield(); return x; }
                static IEnumerable<int> Iterate(int x) { yield return x; }
                [Conditional("NEVER_DEFINED")] static void Trace(int x) { }
                void Run(dynamic d) {
                    int x = 1; Update(ref x, out int y, in x); Alias(ref x);
                    Later(x); Iterate(x); Trace(x);
                    var c = new C(x); c.Direct(x); c.Virtual(x);
                    Func<int,int> f = Direct; f(x); d.Call(); new C(d);
                    Console.WriteLine("RAW_CALL_LITERAL");
                }
            }
            """)]));
        var check = SemanticSelfCheck.Require;
        check(output.Status == "BOUND_INPUT_BUNDLE", JsonSerializer.Serialize(output.Diagnostics));
        var links = output.Connections;
        check(links.Single(c => c.TargetId?.Contains("Update") == true).Inputs.All(a => a.Relation == "REFERENCE_ALIAS_REQUIRED"), "reference alias overclaimed");
        foreach (var method in new[] { "Alias", "Later", "Iterate", "#ctor" })
            check(links.Where(c => c.TargetId?.Contains(method) == true).All(c => c.Returns.Length == 0), "special return treated as scalar");
        check(links.Any(c => c.Receiver is not null), "instance receiver not linked");
        check(links.Any(c => c.Status == "CONDITIONAL_CALL"), "conditional execution overclaimed");
        check(links.Count(c => c.Status == "STATIC_TARGET_ONLY") >= 2, "dispatch target overclaimed");
        check(links.Any(c => c.Status == "UNRESOLVED") && links.Any(c => c.Status == "NO_SOURCE_BODY"), "unknown/external boundary missing");
        check(links.Where(c => c.Status != "BOUNDARY_LINKED").All(c => c.Returns.Length == 0), "uncertain return edge emitted");
        check(output.Calls.Count(c => c.Dispatch == "DYNAMIC") == 2, "dynamic construction guessed");
        check(!JsonSerializer.Serialize(output).Contains("RAW_CALL_LITERAL"), "literal persisted");
        ConnectionDispatchSelfCheck.Run();
    }
}
