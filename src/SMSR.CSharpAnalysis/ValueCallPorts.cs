using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace SMSR.CSharpAnalysis;

internal static class ValueCallPorts
{
    internal static void Read(IOperation call,IMethodSymbol target,IEnumerable<IArgumentOperation> arguments,
        IOperation? receiver,int result,int block,ValueBuilder graph)
    {
        var source=SymbolFacts.Source(call.Syntax); var targetId=SymbolFacts.Id(target);
        foreach(var argument in arguments)
            graph.Port(new("CALL_ARGUMENT",graph.Results[argument],block,SymbolFacts.Source(argument.Syntax),
                targetId,argument.Parameter?.Ordinal,source));
        if(receiver is not null) graph.Port(new("CALL_RECEIVER",graph.Results[receiver],block,
            SymbolFacts.Source(receiver.Syntax),targetId,null,source));
        if(call.Type is not null && call.Type.SpecialType!=SpecialType.System_Void)
            graph.Port(new("CALL_RESULT",result,block,source,targetId,null,source));
    }
}
