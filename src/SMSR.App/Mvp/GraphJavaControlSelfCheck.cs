using System.Text.Json;

namespace SMSR.App.Mvp;

internal static class GraphJavaControlSelfCheck
{
    internal const string Source="""
        class Control {
          static int decide(boolean p,int a,int b){if(p)return a;return b;}
          static void endless(){for(;;){}}
        }
        """;
    internal static void Verify(JsonElement result)
    {
        var methods=result.GetProperty("methods").EnumerateArray().ToArray();
        var control=methods.Single(m=>m.GetProperty("id").GetString()=="source:Control#decide(boolean,int,int)").GetProperty("control");
        var nodes=control.GetProperty("nodes").EnumerateArray().ToDictionary(n=>n.GetProperty("id").GetInt32());
        var links=control.GetProperty("links").EnumerateArray().Where(e=>
            nodes[e.GetProperty("dependent").GetInt32()].GetProperty("kind").GetString()=="RETURN").ToArray();
        if(control.GetProperty("status").GetString()!="NORMAL_CFG_CONTROL_DEPENDENCE" || links.Length!=2 ||
            !links.Select(e=>e.GetProperty("outcome").GetString()).ToHashSet().SetEquals(["TRUE","FALSE"]) ||
            !nodes.Values.All(n=>n.GetProperty("source").GetProperty("path").GetString()=="Control.java") ||
            !control.GetProperty("postdominators").EnumerateArray().Any(n=>n.GetProperty("immediate").GetInt32()==-1))
            throw new Exception("Java normal control dependence not persisted");
        var endless=methods.Single(m=>m.GetProperty("id").GetString()=="source:Control#endless()").GetProperty("control");
        if(endless.GetProperty("status").GetString()!="UNAVAILABLE" ||
            endless.GetProperty("reason").GetString()!="NON_EXIT_REACHABLE_REGION" || endless.GetProperty("links").GetArrayLength()!=0)
            throw new Exception("Java nonterminating control boundary missing");
    }
}
