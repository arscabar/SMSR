using System.Text.Json;

namespace SMSR.App.Mvp;

internal static class GraphTypeScriptSwitchSelfCheck
{
    internal const string Source="""
        export function pick(x:number,a:number,b:number){
          switch(x){case 0:return a;default:return b;case 1:return a+1;}
        }
        export function killed(x:number,a:number){switch(x){case 0:a=0;break;default:a=1;}return a;}
        export function relay(x:number,a:number,b:number){return pick(x,a,b);}
        export function erased(x:number,a:number){return killed(x,a);}
        """;
    internal static void Verify(JsonElement result)
    {
        var function=result.GetProperty("functions").EnumerateArray().Single(f=>f.GetProperty("path").GetString()=="ZSwitch.ts" && f.GetProperty("name").GetString()=="pick");
        var c=function.GetProperty("control");
        var nodes=c.GetProperty("nodes").EnumerateArray().ToDictionary(n=>n.GetProperty("id").GetInt32());
        var cases=nodes.Values.Where(n=>n.GetProperty("kind").GetString()=="CASE_TEST").Select(n=>n.GetProperty("id").GetInt32()).ToHashSet();
        if(c.GetProperty("status").GetString()!="NORMAL_CFG_CONTROL_DEPENDENCE" || cases.Count!=2 ||
            nodes.Values.Count(n=>n.GetProperty("kind").GetString()=="SWITCH_VALUE")!=1)
            throw new Exception("TypeScript switch CFG missing");
        var edges=c.GetProperty("transfers").EnumerateArray().Where(e=>cases.Contains(e.GetProperty("source").GetInt32())).ToArray();
        if(edges.Length!=4 || !edges.Select(e=>e.GetProperty("outcome").GetString()).ToHashSet().SetEquals(["CASE_MATCH","CASE_NO_MATCH"]))
            throw new Exception("TypeScript case outcomes missing");
        var links=c.GetProperty("links").EnumerateArray().Where(e=>nodes[e.GetProperty("dependent").GetInt32()].GetProperty("kind").GetString()=="RETURN").ToArray();
        if(links.Select(e=>e.GetProperty("dependent").GetInt32()).Distinct().Count()!=3 ||
            !links.Any(e=>e.GetProperty("outcome").GetString()=="CASE_NO_MATCH"))
            throw new Exception("TypeScript switch return dependence missing");
        GraphTypeScriptSwitchValueSelfCheck.Verify(result);
    }
}
