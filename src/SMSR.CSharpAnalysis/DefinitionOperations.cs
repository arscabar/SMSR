using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace SMSR.CSharpAnalysis;

internal static class DefinitionOperations
{
    internal static string? Read(IEnumerable<IOperation> roots, int block, List<VariableSite> sites,
        Dictionary<(IOperation,bool),int> bindings)
    {
        var stack = new Stack<(IOperation Op, bool Write)>();
        foreach (var root in roots.Reverse()) stack.Push((root, false));
        while (stack.TryPop(out var item))
        {
            var op = item.Op;
            var symbol = op switch {
                ILocalReferenceOperation l when l.Local.RefKind == RefKind.None => (ISymbol)l.Local,
                IParameterReferenceOperation p when p.Parameter.RefKind == RefKind.None => p.Parameter,
                _ => null
            };
            if (symbol is not null)
            {
                if (op.Type?.SpecialType is not (>= SpecialType.System_Boolean and <= SpecialType.System_String))
                    return "NON_SCALAR_STORAGE";
                bindings.Add((op,item.Write),sites.Count);
                sites.Add(new(sites.Count, block, SymbolFacts.Id(symbol), item.Write ? "WRITE" : "READ", SymbolFacts.Source(op.Syntax)));
                continue;
            }
            if (item.Write) return "INDIRECT_WRITE";
            if (op is IAssignmentOperation assignment)
            {
                if (op is not (ISimpleAssignmentOperation { IsRef: false } or ICompoundAssignmentOperation))
                    return "ALIAS_OR_DECONSTRUCTION";
                stack.Push((assignment.Target, true));
                stack.Push((assignment.Value, false));
                if (op is ICompoundAssignmentOperation) stack.Push((assignment.Target, false));
                continue;
            }
            if (op is IIncrementOrDecrementOperation increment)
            {
                stack.Push((increment.Target, true)); stack.Push((increment.Target, false)); continue;
            }
            if (op is IArgumentOperation { Parameter.RefKind: not RefKind.None }) return "REFERENCE_ARGUMENT";
            // Unknown operation kinds must not silently hide a write or alternate evaluation path.
            if (op.Kind.ToString() is not ("ExpressionStatement" or "Literal" or "Conversion" or
                "Binary" or "Unary" or "Parenthesized" or "Invocation" or "Argument" or
                "FlowCapture" or "FlowCaptureReference" or "IsNull" or "DefaultValue" or
                "InstanceReference" or "ObjectCreation" or "FieldReference" or "PropertyReference" or
                "ArrayElementReference" or "ArrayCreation" or "ArrayInitializer" or "TypeOf" or "SizeOf"))
                return "OPERATION_" + op.Kind;
            foreach (var child in op.ChildOperations.Reverse()) stack.Push((child, false));
        }
        return null;
    }
}
