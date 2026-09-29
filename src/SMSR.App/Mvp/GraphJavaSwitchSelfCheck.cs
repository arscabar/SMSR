using System.Text.Json;

namespace SMSR.App.Mvp;

internal static class GraphJavaSwitchSelfCheck
{
    internal const string Source="""
        class Switches {
          static int pick(int k,int a,int b){switch(k){case 0:return a;default:return b;}}
          static int erase(int k,int a){switch(k){case 0:a=0;default:a=1;}return a;}
          static int rule(int k,int a,int b){int x=0;switch(k){case 0,1 -> x=a;default -> x=b;}return x;}
        }
        """;
    internal static void Verify(JsonElement result)
    {
        foreach(var (name,wanted) in new[]{("pick",new[]{1,2}),("erase",Array.Empty<int>()),("rule",new[]{1,2})})
        {
            var m=result.GetProperty("methods").EnumerateArray().Single(m=>m.GetProperty("id").GetString()!.StartsWith("source:Switches#"+name+"("));
            var c=m.GetProperty("control");var summary=m.GetProperty("valueSummary");
            var nodes=c.GetProperty("nodes").EnumerateArray().ToDictionary(n=>n.GetProperty("id").GetInt32());
            var inputs=summary.GetProperty("returns").EnumerateArray().SelectMany(r=>r.GetProperty("parameterIndices").EnumerateArray()).Select(p=>p.GetInt32()).ToHashSet();
            if(c.GetProperty("status").GetString()!="NORMAL_CFG_CONTROL_DEPENDENCE" ||
                summary.GetProperty("status").GetString()!="DATA_DEPENDENCY_CANDIDATES" || !inputs.SetEquals(wanted) ||
                summary.GetProperty("returns").EnumerateArray().Any(r=>r.GetProperty("unknownValueIds").GetArrayLength()!=0) ||
                nodes.Values.Count(n=>n.GetProperty("kind").GetString()=="SWITCH_VALUE")!=1 ||
                nodes.Values.Count(n=>n.GetProperty("kind").GetString()=="CASE_TEST")!=(name=="rule"?2:1))
                throw new Exception("Java switch control/value evidence not persisted");
            if(name=="pick")
            {
                var links=c.GetProperty("links").EnumerateArray().Where(e=>nodes[e.GetProperty("dependent").GetInt32()].GetProperty("kind").GetString()=="RETURN");
                if(!links.Select(e=>e.GetProperty("outcome").GetString()).ToHashSet().SetEquals(["CASE_MATCH","CASE_NO_MATCH"]))
                    throw new Exception("Java switch return selection missing");
            }
        }
    }
}
