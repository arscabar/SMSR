namespace SMSR.CSharpAnalysis;

internal static class ValueBoundarySelfCheck
{
    internal static void Run()
    {
        var result=Analyzer.Run(new([new("Opaque.cs","""
            using System;
            struct Box { public static implicit operator int(Box b) => 1; }
            class C {
                static Box box;
                int Operator(int x) => x + box;
                static int External(int a) => a;
                int Call(int x) => External(x);
                string Heap() => Environment.NewLine;
                int Literal() => "RAW_VALUE_SECRET".Length;
                int Ref(ref int x) => x;
                C Self() => this;
                int Select(bool c,bool d,int x,int y) { int a=c?x:y; int b=d?a:0; return b; }
            }
            """)]));
        var check=SemanticSelfCheck.Require;
        check(result.Status=="BOUND_INPUT_BUNDLE","opaque fixture invalid");
        ValueFlow Get(string name)=>result.Functions.Single(f=>f.SymbolId==result.Symbols.Single(s=>s.Name==name && s.Kind=="Method").Id).Definitions.Values;
        var call=Get("Call"); var argument=call.Ports.Single(p=>p.Kind=="CALL_ARGUMENT");
        var output=call.Ports.Single(p=>p.Kind=="CALL_RESULT");
        check(call.Status=="PARTIAL_VALUE_DEPENDENCE" && argument.TargetId=="source:M:C.External(System.Int32)" &&
            argument.ParameterOrdinal==0 && argument.CallSource==output.CallSource &&
            ValueSelfCheck.Reaches(call,0,argument.Value) && !ValueSelfCheck.Reaches(call,argument.Value,output.Value),
            "unknown call silently propagated input to result");
        check(Get("Heap").Nodes.Any(n=>n.Status=="OPAQUE_HEAP"),"heap result overclaimed");
        var op=Get("Operator"); var unknown=op.Nodes.Single(n=>n.Status=="OPAQUE_OPERATOR");
        check(op.Status=="PARTIAL_VALUE_DEPENDENCE" && !op.Links.Any(l=>l.Target==unknown.Id),"user conversion silently propagated");
        check(Get("Self").Nodes.Any(n=>n.Status=="OPAQUE_RECEIVER"),"receiver state overclaimed");
        check(Get("Ref").Status=="UNAVAILABLE","reference alias value overclaimed");
        var select=Get("Select"); var end=select.Ports.Single(p=>p.Kind=="RETURN").Value;
        check(ValueSelfCheck.Reaches(select,2,end) && ValueSelfCheck.Reaches(select,3,end) &&
            !ValueSelfCheck.Reaches(select,0,end) && !ValueSelfCheck.Reaches(select,1,end),"multiple capture groups merged incorrectly");
        check(!System.Text.Json.JsonSerializer.Serialize(result).Contains("RAW_VALUE_SECRET"),"value literal leaked");
        var builder=new ValueBuilder([],[]);
        try { for(var i=0;i<=50_000;i++) builder.Link(0,0,"test"); throw new Exception("value graph cap absent"); }
        catch(ArgumentException e) when(e.Message=="Value graph limit exceeded") { }
    }
}
