using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace SMSR.CSharpAnalysis;

internal static class ValueExpressions
{
    internal static int Read(IOperation op,int block,ValueBuilder graph)
    {
        var method=op switch {
            IBinaryOperation b=>b.OperatorMethod, IUnaryOperation u=>u.OperatorMethod,
            IConversionOperation c=>c.OperatorMethod, ICompoundAssignmentOperation c=>c.OperatorMethod,
            IIncrementOrDecrementOperation i=>i.OperatorMethod, _=>null
        };
        var status=op switch {
            IInvocationOperation or IObjectCreationOperation=>"OPAQUE_CALL",
            IInstanceReferenceOperation=>"OPAQUE_RECEIVER",
            IMemberReferenceOperation or IArrayElementReferenceOperation or IArrayCreationOperation or IArrayInitializerOperation=>"OPAQUE_HEAP",
            _ when method is not null=>"OPAQUE_OPERATOR",
            _=>"LOCAL_VALUE"
        };
        var kind=op.Kind+ (op switch { IBinaryOperation b=>":"+b.OperatorKind, IUnaryOperation u=>":"+u.OperatorKind,
            ICompoundAssignmentOperation c=>":"+c.OperatorKind, _=>"" });
        var id=graph.Add(op,block,kind,status,method is null?null:SymbolFacts.Id(method));
        if(status=="LOCAL_VALUE")
            foreach(var child in op.ChildOperations) graph.Link(graph.Results[child],id,"OPERAND");
        if(op is IFlowCaptureOperation capture) graph.Capture(capture.Id,id,block,true,SymbolFacts.Source(op.Syntax));
        if(op is IFlowCaptureReferenceOperation reference) graph.Capture(reference.Id,id,block,false,SymbolFacts.Source(op.Syntax));
        if(op is IInvocationOperation call)
            ValueCallPorts.Read(call,call.TargetMethod,call.Arguments,call.Instance,id,block,graph);
        if(op is IObjectCreationOperation create && create.Constructor is { } constructor)
            ValueCallPorts.Read(create,constructor,create.Arguments,null,id,block,graph);
        return id;
    }
}
