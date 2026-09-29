using System.Text.Json;

namespace SMSR.App.Mvp;

internal static class GraphJavaValuesSelfCheck
{
    internal static void Verify(JsonElement result)
    {
        foreach(var (id,expected) in new[]{("source:Lib#pick(int)",true),("source:Lib#kill(int)",false)})
        {
            var method=result.GetProperty("methods").EnumerateArray().Single(m=>m.GetProperty("id").GetString()==id);
            var value=method.GetProperty("values");
            var nodes=value.GetProperty("nodes").EnumerateArray().ToArray();
            string Id(string kind)=>nodes.Single(n=>n.GetProperty("kind").GetString()==kind).GetProperty("id").GetString()!;
            if(value.GetProperty("status").GetString()!="VALUE_FLOW_CANDIDATES" ||
                nodes.Single(n=>n.GetProperty("kind").GetString()=="PARAMETER_INPUT").GetProperty("symbolId").ValueKind!=JsonValueKind.String)
                throw new Exception("Java value/symbol evidence missing");
            var seen=new HashSet<string>{Id("PARAMETER_INPUT")};bool changed;
            do
            {
                changed=false;
                foreach(var e in value.GetProperty("edges").EnumerateArray())
                    if(e.GetProperty("relation").GetString()!="CONTROL_CONDITION_CANDIDATE" &&
                        seen.Contains(e.GetProperty("source").GetString()!) && seen.Add(e.GetProperty("target").GetString()!)) changed=true;
            } while(changed);
            if(seen.Contains(Id("RETURN"))!=expected) throw new Exception("Java GEN/KILL evidence not persisted");
        }
    }
}
