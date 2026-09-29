using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SMSR.App.Mvp;

internal static class GraphPythonSelfCheck
{
    internal static async Task RunAsync(string root)
    {
        var folder=Path.Combine(root,"python-http");
        await using var host=await LocalServer.StartAsync(folder,0);
        var store=new EventStore(Path.Combine(folder,"smsr.db"));
        var source="def outer(value):\n def inner(): return value\n return inner\ndef slot(value):\n value += 1\n return value\n# PYTHON_SOURCE_MARKER";
        source+="\ndef walk(items,obj):\n for value in items:\n  obj.send(value)\n return obj.field\n";
        source+="\ndef decision(flag):\n if flag: return 1\n return 0\ndef endless():\n while True: pass\n";
        source+="\ndef summarize(value,target):\n return target(value,original=value)\n";
        source+="\ndef local(value):\n def inner(x): return x\n copied=inner\n return copied(value)\n";
        source+="\ndef erased(value):\n def inner(x):\n  x=0\n  return x\n return inner(value)\n";
        source+="\ndef nested(value):\n def middle(x):\n  def inner(y): return y\n  return inner(x)\n return middle(value)\n";
        source+=GraphPythonKeywordSelfCheck.Source;
        await File.WriteAllTextAsync(Path.Combine(root,"scope.py"),source,new UTF8Encoding(false));
        var hash=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(source)));
        await store.ApplyGraphScanAsync("python",new(root,[new("scope.py",hash,"code")],
            [new("file:scope.py","scope.py","code","scope.py","scope.py",1,hash)],[],[],["scope.py"],[],[]),false);
        using var client=new HttpClient(); var input=new {projectId="python",input="scope.py"};
        using var denied=await client.PostAsJsonAsync(host.Address+"/api/graph/analyze",input);
        if(denied.StatusCode!=HttpStatusCode.Unauthorized) throw new Exception("Python origin guard missing");
        client.DefaultRequestHeaders.Add("Origin",host.Address);
        using var response=await client.PostAsJsonAsync(host.Address+"/api/graph/analyze",input);
        response.EnsureSuccessStatusCode(); Verify(await response.Content.ReadFromJsonAsync<JsonElement>());
        await GraphPythonStorageSelfCheck.RunAsync(root,folder,host.Address,client,store);
    }
    internal static void Verify(JsonElement report)
    {
        var compiler=report.GetProperty("result").GetProperty("compiler");
        var functions=compiler.GetProperty("functions").EnumerateArray().ToArray();
        var outer=functions.Single(f=>f.GetProperty("name").GetString()=="outer");
        var inner=functions.Single(f=>f.GetProperty("name").GetString()=="outer.<locals>.inner");
        if(report.GetProperty("analysisVersion").GetInt32()!=10 || compiler.GetProperty("status").GetString()!="COMPILER_BYTECODE" ||
            outer.GetProperty("cells").GetProperty("value").GetString()!=inner.GetProperty("free").GetProperty("value").GetString())
            throw new Exception("Python compiler closure binding missing");
        GraphPythonDefinitionsSelfCheck.Verify(functions);
        GraphPythonValuesSelfCheck.Verify(functions);
        GraphPythonProtocolsSelfCheck.Verify(functions);
        GraphPythonControlSelfCheck.Verify(functions);
        GraphPythonSummarySelfCheck.Verify(functions);
        GraphPythonLocalCallSelfCheck.Verify(functions);
        GraphPythonCallSummarySelfCheck.Verify(functions);
        GraphPythonKeywordSelfCheck.Verify(functions);
    }
}
