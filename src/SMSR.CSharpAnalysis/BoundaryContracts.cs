namespace SMSR.CSharpAnalysis;

internal sealed record ParameterSlot(string Id, int Ordinal, string Name, string Type, string RefKind,
    bool Optional, bool Params, Span? Source);
internal sealed record MethodBoundary(ParameterSlot[] Parameters, string ReturnKind, string? ReceiverId);
internal sealed record ConversionBinding(bool Exists, bool Identity, string? MethodId);
internal sealed record CallBoundary(Span? ReceiverSource, bool Conditional, string ReturnKind);
internal sealed record ParameterLink(ArgumentBinding Argument, string ParameterId, Span? ParameterSource, string Relation);
internal sealed record ReturnLink(int Block, Span Source, Span CallSource, string Relation);
internal sealed record ReceiverLink(Span Source, string TargetId);
internal sealed record CallConnection(Span Source, string? CallerId, string? TargetId, string Status,
    ParameterLink[] Inputs, ReturnLink[] Returns, ReceiverLink? Receiver, string[] Limitations);
