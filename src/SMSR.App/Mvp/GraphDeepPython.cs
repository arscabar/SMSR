using System.Text.Json;
using static SMSR.App.Mvp.GraphDeepMapping;

namespace SMSR.App.Mvp;

internal static partial class GraphDeepProjection
{
    private static GraphDeepFact[] Python(JsonElement result, string path,
        GraphDeepMapping map, GraphDeepEvidence evidence)
    {
        var compiler = Object(result, "compiler");
        if (Text(compiler, "status") != "COMPILER_BYTECODE") return [];
        var functions = Items(compiler, "functions").GroupBy(f => Text(f, "id") ?? "")
            .Where(g => g.Key != "" && g.Count() == 1).ToDictionary(g => g.Key, g => g.Single());
        var facts = new List<GraphDeepFact>();
        foreach (var function in functions.Values)
        {
            var caller = map.Callable(path, Number(function, "firstLine"));
            if (caller is null) continue;
            foreach (var call in Items(Object(function, "valueSummary"), "calls"))
            {
                var local = Object(call, "localTarget");
                var bodies = Items(local, "bodyIds");
                if (Text(local, "status") != "LOCAL_BODY_CONNECTION_CANDIDATE" || bodies.Length != 1 ||
                    !functions.TryGetValue(bodies[0].GetString() ?? "", out var destination)) continue;
                var target = map.Callable(path, Number(destination, "firstLine"));
                var instruction = Items(function, "instructions").SingleOrDefault(i =>
                    Number(i, "offset") == Number(call, "offset"));
                if (target is null || instruction.ValueKind != JsonValueKind.Object) continue;
                var line = Number(Object(Object(instruction, "source"), "start"), "line") + 1;
                Add(facts, caller, target, line,
                    evidence with { Binding = "LOCAL_BODY_CONNECTION_CANDIDATE", Dispatch = "LOCAL_CANDIDATE" });
            }
        }
        return facts.ToArray();
    }
}
