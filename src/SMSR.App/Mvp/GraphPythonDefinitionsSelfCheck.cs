using System.Text.Json;

namespace SMSR.App.Mvp;

internal static class GraphPythonDefinitionsSelfCheck
{
    internal static void Verify(JsonElement[] functions)
    {
        var slot=functions.Single(f=>f.GetProperty("name").GetString()=="slot").GetProperty("definitions");
        if(slot.GetProperty("status").GetString()!="LOCAL_SLOT_DEFINITIONS") throw new Exception("Python definition model missing");
        var definitions=slot.GetProperty("definitions").EnumerateArray().ToDictionary(d=>d.GetProperty("id").GetString()!);
        var reads=slot.GetProperty("reads").EnumerateArray().ToDictionary(r=>r.GetProperty("id").GetString()!);
        var edges=slot.GetProperty("edges").EnumerateArray().ToArray();
        if(edges.Length!=2 || reads.Count!=2) throw new Exception("Python reaching edges missing");
        var origins=new HashSet<string>();
        foreach(var edge in edges)
        {
            var definition=definitions[edge.GetProperty("source").GetString()!];
            var read=reads[edge.GetProperty("target").GetString()!];
            origins.Add(definition.GetProperty("kind").GetString()!);
            if(definition.GetProperty("bindingId").GetString()!=read.GetProperty("bindingId").GetString() ||
                read.GetProperty("mayBeUnbound").GetBoolean()) throw new Exception("Python slot identity mismatch");
        }
        if(!origins.SetEquals(["PARAMETER","WRITE"])) throw new Exception("Python input/write origins missing");
    }
}
