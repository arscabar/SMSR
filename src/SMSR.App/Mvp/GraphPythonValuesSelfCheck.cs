using System.Text.Json;

namespace SMSR.App.Mvp;

internal static class GraphPythonValuesSelfCheck
{
    internal static void Verify(JsonElement[] functions)
    {
        var slot=functions.Single(f=>f.GetProperty("name").GetString()=="slot").GetProperty("values");
        if(slot.GetProperty("status").GetString()!="VALUE_FLOW_CANDIDATES") throw new Exception("Python values missing");
        var nodes=slot.GetProperty("nodes").EnumerateArray().ToDictionary(n=>n.GetProperty("id").GetString()!);
        var edges=slot.GetProperty("edges").EnumerateArray().ToArray();
        if(edges.Length!=6 || nodes.Count!=7) throw new Exception("Python value graph changed");
        foreach(var edge in edges)
            if(!nodes.ContainsKey(edge.GetProperty("source").GetString()!) || !nodes.ContainsKey(edge.GetProperty("target").GetString()!))
                throw new Exception("Python value endpoint missing");
        var expected=new[]{"PARAMETER","READ","OPERATION","WRITE","READ","RETURN"};
        var current=nodes.Single(n=>n.Value.GetProperty("kind").GetString()==expected[0]).Key;
        foreach(var kind in expected.Skip(1))
        {
            current=edges.Single(e=>e.GetProperty("source").GetString()==current).GetProperty("target").GetString()!;
            if(nodes[current].GetProperty("kind").GetString()!=kind) throw new Exception("Python value path incorrect");
        }
        var outer=functions.Single(f=>f.GetProperty("name").GetString()=="outer").GetProperty("values");
        if(outer.GetProperty("reason").GetString()!="SHARED_CELLS" || outer.GetProperty("edges").GetArrayLength()!=0)
            throw new Exception("Python shared-cell boundary missing");
    }
}
