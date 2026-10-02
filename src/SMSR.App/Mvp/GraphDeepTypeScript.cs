using System.Text.Json;
using static SMSR.App.Mvp.GraphDeepMapping;

namespace SMSR.App.Mvp;

internal static partial class GraphDeepProjection
{
    private static GraphDeepFact[] TypeScript(JsonElement result, GraphDeepMapping map,
        GraphDeepEvidence evidence)
    {
        if (Text(result, "status") != "COMPILER_BINDINGS") return [];
        var functions = Items(result, "functions").GroupBy(f => Text(f, "id") ?? "")
            .Where(g => g.Key != "" && g.Count() == 1).ToDictionary(g => g.Key, g => g.Single());
        var calls = Items(result, "calls").GroupBy(c => Text(c, "id") ?? "")
            .Where(g => g.Key != "" && g.Count() == 1).ToDictionary(g => g.Key, g => g.Single());
        var facts = new List<GraphDeepFact>();
        foreach (var connection in Items(result, "connections"))
        {
            if (Text(connection, "status") != "STATIC_BODY_CANDIDATE" ||
                !functions.TryGetValue(Text(connection, "callerFunctionId") ?? "", out var source) ||
                !functions.TryGetValue(Text(connection, "bodyId") ?? "", out var destination) ||
                !calls.TryGetValue(Text(connection, "callId") ?? "", out var call)) continue;
            var caller = map.Callable(Text(source, "path"), Number(source, "line"));
            var target = map.Callable(Text(destination, "path"), Number(destination, "line"));
            if (caller is null || target is null || Text(call, "path") != caller.OwnerPath) continue;
            Add(facts, caller, target, Number(call, "line"),
                evidence with { Binding = "STATIC_BODY_CANDIDATE", Dispatch = "STATIC" });
        }
        return facts.ToArray();
    }
}
