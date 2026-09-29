using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace SMSR.CSharpAnalysis;

internal static class CallFacts
{
    internal static CallBinding Read(SemanticModel model, ExpressionSyntax node, bool valid)
    {
        var info = model.GetSymbolInfo(node);
        var operation = model.GetOperation(node);
        var call = operation as IInvocationOperation;
        var target = call?.TargetMethod ?? (operation as IObjectCreationOperation)?.Constructor ?? info.Symbol as IMethodSymbol;
        var dynamic = operation is IDynamicInvocationOperation or IDynamicObjectCreationOperation;
        var dispatch = dynamic ? "DYNAMIC" : target?.MethodKind == MethodKind.DelegateInvoke ? "DELEGATE" :
            call?.ConstrainedToType is not null ? "CONSTRAINED" : call?.IsVirtual == true ? "VIRTUAL" : target is not null ? "DIRECT" : "UNKNOWN";
        var resolution = target is null ? (info.CandidateSymbols.Length > 0 ? "AMBIGUOUS" : "UNRESOLVED") :
            !valid ? "COMPILER_CANDIDATE" : dispatch is "DELEGATE" or "VIRTUAL" or "CONSTRAINED" ? "STATIC_TARGET_ONLY" : "BOUND_IN_BUNDLE";
        var owner = model.GetEnclosingSymbol(node.SpanStart);
        var arguments = (call?.Arguments ?? (operation as IObjectCreationOperation)?.Arguments ?? [])
            .Where(a => a.Parameter is not null).Select(ArgumentFacts.Read).ToArray();
        return new(SymbolFacts.Source(node), owner is null ? null : SymbolFacts.Id(owner),
            target is null ? null : SymbolFacts.Id(target), target is null ? null : SymbolFacts.Signature(target),
            resolution, dispatch, model.GetTypeInfo(node).Type is { } type ? SymbolFacts.Signature(type) : null,
            info.CandidateReason.ToString(), SymbolFacts.Candidates(info), arguments,
            new(call?.Instance is { } receiver ? SymbolFacts.Source(receiver.Syntax) : null,
                target?.IsConditional == true, BoundaryFacts.ReturnKind(target)));
    }
}
