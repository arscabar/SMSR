using System.Text.Json;

namespace SMSR.App.Mvp;

internal static class GraphTypeScriptControlSelfCheck
{
    internal const string Source="""
        export function decide(p:boolean,a:number,b:number){if(p)return a;return b;}
        export function endless(){for(;;){}}
        export function nullSet(a:any,b:any){a??=b;return a;}
        """;
    internal static void Verify(JsonElement result)
    {
        var functions=result.GetProperty("functions").EnumerateArray().Where(f=>f.GetProperty("path").GetString()=="ZControl.ts").ToArray();
        var control=functions[0].GetProperty("control");
        var nodes=control.GetProperty("nodes").EnumerateArray().ToDictionary(n=>n.GetProperty("id").GetInt32());
        var links=control.GetProperty("links").EnumerateArray().Where(e=>
            nodes[e.GetProperty("dependent").GetInt32()].GetProperty("kind").GetString()=="RETURN").ToArray();
        if(control.GetProperty("status").GetString()!="NORMAL_CFG_CONTROL_DEPENDENCE" || links.Length!=2 ||
            !links.Select(e=>e.GetProperty("outcome").GetString()).ToHashSet().SetEquals(["TRUE","FALSE"]) ||
            !nodes.Values.All(n=>n.GetProperty("source").GetProperty("path").GetString()=="ZControl.ts"))
            throw new Exception("TypeScript branch control not persisted");
        var endless=functions[1].GetProperty("control");
        if(endless.GetProperty("reason").GetString()!="NON_EXIT_REACHABLE_REGION" || endless.GetProperty("links").GetArrayLength()!=0)
            throw new Exception("TypeScript nonterminating boundary missing");
        var nullable=functions[2].GetProperty("control");
        var assignment=nullable.GetProperty("nodes").EnumerateArray().Single(n=>n.GetProperty("kind").GetString()=="ASSIGNMENT").GetProperty("id").GetInt32();
        var dependence=nullable.GetProperty("links").EnumerateArray().Single(e=>e.GetProperty("dependent").GetInt32()==assignment);
        if(dependence.GetProperty("outcome").GetString()!="NULLISH") throw new Exception("Nullish assignment control missing");
    }
}
