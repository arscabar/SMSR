using System.Text.Json;

namespace SMSR.App.Mvp;

internal static class GraphTypeScriptLoopsSelfCheck
{
    internal static void Verify(JsonElement result)
    {
        foreach(var (index,expected) in new[]{(1,true),(2,false)})
        {
            var value=result.GetProperty("functions")[index].GetProperty("values");
            if(value.GetProperty("status").GetString()!="VALUE_FLOW_CANDIDATES")
                throw new Exception("Loop values unavailable");
            var nodes=value.GetProperty("nodes").EnumerateArray().ToArray();
            string Id(string kind)=>nodes.Single(n=>n.GetProperty("kind").GetString()==kind).GetProperty("id").GetString()!;
            var reached=new HashSet<string>{Id("PARAMETER_INPUT")};
            var edges=value.GetProperty("edges").EnumerateArray().ToArray();
            bool changed;
            do
            {
                changed=false;
                foreach(var edge in edges)
                    if(edge.GetProperty("relation").GetString()!="CONTROL_CONDITION_CANDIDATE" &&
                        reached.Contains(edge.GetProperty("source").GetString()!) &&
                        reached.Add(edge.GetProperty("target").GetString()!)) changed=true;
            } while(changed);
            if(reached.Contains(Id("RETURN"))!=expected)
                throw new Exception("Loop continue/break value evidence not persisted");
        }
    }
}
