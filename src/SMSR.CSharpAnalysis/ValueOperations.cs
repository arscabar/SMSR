using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace SMSR.CSharpAnalysis;

internal static class ValueOperations
{
    internal static void Read(IOperation root,int block,ValueBuilder graph)
    {
        var stack=new Stack<(IOperation Op,bool Done)>(); stack.Push((root,false));
        while(stack.TryPop(out var item))
        {
            var op=item.Op;
            if(!item.Done)
            {
                stack.Push((op,true));
                var children=op is ISimpleAssignmentOperation a ? [a.Value] : op.ChildOperations.ToArray();
                foreach(var child in children.Reverse()) stack.Push((child,false));
                continue;
            }
            int result;
            if(op is ILocalReferenceOperation or IParameterReferenceOperation) result=graph.Variable(op,false);
            else if(op is ISimpleAssignmentOperation assignment)
            {
                result=graph.Variable(assignment.Target,true);
                graph.Link(graph.Results[assignment.Value],result,"ASSIGNMENT");
            }
            else if(op is ICompoundAssignmentOperation compound)
            {
                var operation=ValueExpressions.Read(op,block,graph);
                result=graph.Variable(compound.Target,true); graph.Link(operation,result,"ASSIGNMENT");
            }
            else if(op is IIncrementOrDecrementOperation increment)
            {
                var operation=ValueExpressions.Read(op,block,graph);
                var written=graph.Variable(increment.Target,true); graph.Link(operation,written,"ASSIGNMENT");
                result=increment.IsPostfix?graph.Results[increment.Target]:written;
            }
            else if(op is IExpressionStatementOperation statement) result=graph.Results[statement.Operation];
            else result=ValueExpressions.Read(op,block,graph);
            graph.Results[op]=result;
        }
    }
}
