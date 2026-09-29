namespace SMSR.CSharpAnalysis;

internal static class ReturnBoundarySelfCheck
{
    internal static void Run()
    {
        var r=Analyzer.Run(new([new("Boundaries.cs", """
            static class E { public static int Add(this int x,int y) => x+y; }
            class C {
                static int Second(int x,int y=9) => y;
                int Default(int x) => Second(x);
                int Extension(int x) => x.Add(1);
                virtual public int Virtual(int x) => x;
                int Dispatch(int x) => Virtual(x);
                static int Id(int x) => x;
                int Local(int x) { static int L(int y)=>Id(y); return L(x); }
                int Ref(ref int x) => x;
                int RefCaller(int x) => Ref(ref x);
                int ThrowOnly(int x) { throw new System.Exception(); }
                int NoReturn(int x) => ThrowOnly(x);
                int Control(bool c,int x,int y) => c ? Id(x) : Id(y);
                static Box box = new Box();
                int Convert() => Id(box);
                class Box { public static implicit operator int(Box b) => 1; }
                void Discard(int x) { Id(x); }
            }
            """)]));
        var check=SemanticSelfCheck.Require;
        check(r.Status=="BOUND_INPUT_BUNDLE","return boundary fixture invalid");
        ReturnSummary Get(string name) => r.ReturnSummaries.Single(s=>s.SymbolId==
            r.Symbols.Single(d=>d.Name==name && d.Kind=="Method").Id);
        foreach(var name in new[]{"Extension","Local"})
            check(!Get(name).Uncertain && Get(name).ParameterOrdinals.SequenceEqual([0]),name+" input lost");
        check(!Get("Default").Uncertain && Get("Default").ParameterOrdinals.Length==0,"default literal became caller input");
        foreach(var name in new[]{"Dispatch","RefCaller","NoReturn","Convert"})
            check(Get(name).Uncertain,name+" unsupported boundary proved exact");
        check(!Get("Control").Uncertain && Get("Control").ParameterOrdinals.SequenceEqual([1,2]),"implicit control mixed into data");
        check(Get("Discard").Status=="UNAVAILABLE" && !Get("Discard").Calls.Single().Uncertain,"void caller hid precise call result");
        check(Get("Convert").Calls.Single().Uncertain,"user conversion uncertainty lost at call result");
    }
}
