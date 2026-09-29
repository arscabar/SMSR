namespace SMSR.CSharpAnalysis;

internal static class ControlLimitSelfCheck
{
    internal static void Run()
    {
        var blocks = new List<FlowBlock>();
        for (var n=0;n<150;n++)
            blocks.Add(new(n,"Block",true,"WhenTrue",[],[],new(150,"Regular",[]),
                new(n==149?300:n+1,"Regular",[])));
        for (var n=150;n<300;n++) blocks.Add(new(n,"Block",true,"None",[],[],new(n+1,"Regular",[]),null));
        blocks.Add(new(300,"Exit",true,"None",[],[],null,null));
        var budget=4_000_000;
        try {
            ControlFacts.Read(blocks.ToArray(),[],true,"VALUE",ref budget);
            throw new Exception("control edge expansion limit missing");
        } catch(ArgumentException e) when(e.Message=="Control dependence link limit exceeded") { }
        budget=4_000_000;
        var asyncResult=ControlFacts.Read(blocks.ToArray(),[],true,"ASYNC_RESULT",ref budget);
        SemanticSelfCheck.Require(asyncResult.Status=="UNAVAILABLE" && asyncResult.Links.Length==0,"suspension overclaimed");
        var invalid=ControlFacts.Read([new(0,"Block",true,"None",[],[],new(null,"Regular",[]),null)],[],true,"VALUE",ref budget);
        SemanticSelfCheck.Require(invalid.Status=="UNAVAILABLE","invalid branch accepted");
    }
}
