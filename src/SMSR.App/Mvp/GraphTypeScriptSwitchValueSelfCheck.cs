using System.Text.Json;

namespace SMSR.App.Mvp;

internal static class GraphTypeScriptSwitchValueSelfCheck
{
    internal static void Verify(JsonElement result)
    {
        var functions=result.GetProperty("functions").EnumerateArray().Where(f=>f.GetProperty("path").GetString()=="ZSwitch.ts")
            .ToDictionary(f=>f.GetProperty("name").GetString()!);
        foreach(var name in new[]{"pick","killed","relay","erased"})
        {
            var f=functions[name];var v=f.GetProperty("values");var s=f.GetProperty("valueSummary");
            var inputs=s.GetProperty("returns").EnumerateArray().SelectMany(r=>r.GetProperty("parameterIndices").EnumerateArray()).Select(i=>i.GetInt32()).ToHashSet();
            int[] expected=name is "pick" or "relay"?[1,2]:[];
            if(v.GetProperty("status").GetString()!="VALUE_FLOW_CANDIDATES" ||
                s.GetProperty("status").GetString()!="DATA_DEPENDENCY_CANDIDATES" || !inputs.SetEquals(expected) ||
                s.GetProperty("returns").EnumerateArray().Any(r=>r.GetProperty("unknownValueIds").GetArrayLength()!=0))
                throw new Exception("Switch return data summary not persisted: "+name);
            var edges=s.GetProperty("callEdges").EnumerateArray().ToArray();
            if(edges.Length!=(name=="relay"?2:0)) throw new Exception("Switch call-site dependence mismatch");
            var nodes=v.GetProperty("nodes").EnumerateArray().ToDictionary(n=>n.GetProperty("id").GetString()!);
            if(edges.Any(e=>nodes[e.GetProperty("target").GetString()!].GetProperty("kind").GetString()!="OPAQUE_CALL_RESULT" ||
                nodes[e.GetProperty("source").GetString()!].GetProperty("kind").GetString() is not ("CALL_ARGUMENT_1" or "CALL_ARGUMENT_2")))
                throw new Exception("Switch call argument/result ids mismatch");
        }
    }
}
