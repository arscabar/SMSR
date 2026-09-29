using System.Text.Json;

namespace SMSR.App.Mvp;

internal static class GraphPythonControlSelfCheck
{
    internal static void Verify(JsonElement[] functions)
    {
        var f=functions.Single(f=>f.GetProperty("name").GetString()=="decision");
        var control=f.GetProperty("control");
        if(control.GetProperty("status").GetString()!="NORMAL_CFG_CONTROL_DEPENDENCE" ||
            control.GetProperty("exitNode").GetInt32()!=-1)
            throw new Exception("Python control status missing");
        var instructions=f.GetProperty("instructions").EnumerateArray().ToArray();
        var branch=instructions.Single(i=>i.GetProperty("opcode").GetString()=="POP_JUMP_IF_FALSE")
            .GetProperty("offset").GetInt32();
        var returns=instructions.Where(i=>i.GetProperty("opcode").GetString()=="RETURN_CONST")
            .Select(i=>i.GetProperty("offset").GetInt32()).ToHashSet();
        var links=control.GetProperty("links").EnumerateArray().ToArray();
        if(returns.Count!=2 || links.Length!=2 || links.Any(e=>e.GetProperty("controller").GetInt32()!=branch) ||
            !returns.SetEquals(links.Select(e=>e.GetProperty("dependent").GetInt32())) ||
            !new HashSet<string?>{"TRUE","FALSE"}.SetEquals(links.Select(e=>e.GetProperty("outcome").GetString())))
            throw new Exception("Python branch/return control links missing");
        var immediate=control.GetProperty("postdominators").EnumerateArray()
            .Single(p=>p.GetProperty("offset").GetInt32()==branch);
        if(immediate.GetProperty("immediate").GetInt32()!=-1)
            throw new Exception("Python synthetic exit missing");
        var endless=functions.Single(f=>f.GetProperty("name").GetString()=="endless").GetProperty("control");
        if(endless.GetProperty("reason").GetString()!="NON_EXIT_REACHABLE_REGION" ||
            endless.GetProperty("links").GetArrayLength()!=0)
            throw new Exception("Python non-exiting region falsely analyzed");
    }
}
