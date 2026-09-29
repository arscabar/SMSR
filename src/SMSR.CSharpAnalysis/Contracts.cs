using System.Text.Json;

namespace SMSR.CSharpAnalysis;

internal static class Contracts
{
    internal static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
}
internal sealed record SourceInput(string Path, string Text);
internal sealed record AnalysisInput(SourceInput[] Files, string LanguageVersion = "CSharp12", string[]? Defines = null);
internal sealed record Position(int Line, int Character);
internal sealed record Span(string Path, Position Start, Position End);
internal sealed record Declaration(string Id, string Name, string Kind, Span? Source);
internal sealed record ArgumentBinding(int Ordinal, string RefKind, string Kind, Span Source,
    Span ValueSource, string CompilerParameterId, bool Implicit, ConversionBinding InConversion,
    ConversionBinding OutConversion, Span[] ElementSources);
internal sealed record CallBinding(Span Source, string? CallerId, string? TargetId, string? Signature,
    string Resolution, string Dispatch, string? ReturnType, string CandidateReason,
    string[] Candidates, ArgumentBinding[] Arguments, CallBoundary Boundary);
internal sealed record ReferenceBinding(Span Source, string? TargetId, string? Alias,
    string Resolution, string[] Candidates);
internal sealed record CompilerIssue(string Code, string Severity, Span? Source);
internal sealed record AnalysisOutput(string Status, string Profile, string LanguageVersion,
    string[] Defines, string Runtime, string CompilerVersion, string[] Paths,
    Declaration[] Symbols, CallBinding[] Calls, ReferenceBinding[] References, CompilerIssue[] Diagnostics,
    int DiagnosticCount, bool DiagnosticsTruncated, FunctionFlow[] Functions, CallConnection[] Connections,
    ReturnSummary[] ReturnSummaries);
