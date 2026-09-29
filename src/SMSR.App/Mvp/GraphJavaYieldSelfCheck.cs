using System.Text.Json;

namespace SMSR.App.Mvp;

internal static class GraphJavaYieldSelfCheck
{
    internal const string Source="""
        class Yields {
          static int pick(int k,int a,int b){return switch(k){case 0 -> {yield a;}default -> b;};}
          static int nested(int k,int a,int b){return switch(k){default -> {int x=switch(k){default -> {yield a;}};x=b;yield x;}};}
          static int state(int k,int a,int b){int x=0;int y=switch(k){case 0 -> {x=a;yield 0;}default -> {x=b;yield 0;}};return x;}
        }
        """;
    internal static void Verify(JsonElement result)
    {
        foreach(var (name,wanted,count) in new[]{("pick",new[]{1,2},1),("nested",new[]{2},2),("state",new[]{1,2},1)})
        {
            var m=result.GetProperty("methods").EnumerateArray().Single(m=>m.GetProperty("id").GetString()!.StartsWith("source:Yields#"+name+"("));
            var c=m.GetProperty("control");var s=m.GetProperty("valueSummary");var v=m.GetProperty("values");
            var nodes=v.GetProperty("nodes").EnumerateArray().ToDictionary(n=>n.GetProperty("id").GetString()!);
            var inputs=s.GetProperty("returns").EnumerateArray().SelectMany(r=>r.GetProperty("parameterIndices").EnumerateArray()).Select(p=>p.GetInt32()).ToHashSet();
            if(c.GetProperty("status").GetString()!="NORMAL_CFG_CONTROL_DEPENDENCE" ||
                s.GetProperty("status").GetString()!="DATA_DEPENDENCY_CANDIDATES" || !inputs.SetEquals(wanted) ||
                s.GetProperty("returns").EnumerateArray().Any(r=>r.GetProperty("unknownValueIds").GetArrayLength()!=0) ||
                nodes.Values.Count(n=>n.GetProperty("kind").GetString()=="SWITCH_RESULT")!=count ||
                c.GetProperty("nodes").EnumerateArray().Count(n=>n.GetProperty("kind").GetString()=="SWITCH_VALUE")!=count)
                throw new Exception("Java yield result/state not persisted");
            var edges=v.GetProperty("edges").EnumerateArray().Where(e=>e.GetProperty("relation").GetString()=="SWITCH_RESULT_VALUE").ToArray();
            if(edges.Length!=2 || edges.Any(e=>!nodes.ContainsKey(e.GetProperty("source").GetString()!) ||
                nodes[e.GetProperty("target").GetString()!].GetProperty("kind").GetString()!="SWITCH_RESULT"))
                throw new Exception("Java yield ownership missing");
        }
    }
}
