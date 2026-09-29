namespace SMSR.CSharpAnalysis;

internal sealed record FlowOperation(int Id, int? Parent, int ChildIndex, string Kind,
    Span Source, bool Implicit, string? SymbolId, string? ParameterId);
internal sealed record FlowBranch(int? Destination, string Semantics, int[] FinallyRegions, bool ConstantExcluded = false);
internal sealed record FlowBlock(int Ordinal, string Kind, bool Reachable, string Condition,
    FlowOperation[] Operations, FlowOperation[] BranchValue, FlowBranch? FallThrough, FlowBranch? Conditional);
internal sealed record FlowRegion(int Id, int? Parent, string Kind, int FirstBlock, int LastBlock,
    string[] Locals, string[] LocalFunctions);
internal sealed record VariableFlow(string Status, Span? Source, string[] Declared, string[] Read,
    string[] Written, string[] FlowsIn, string[] FlowsOut, string[] AlwaysAssigned, string[] Captured);
internal sealed record FunctionFlow(string SymbolId, Span Source, string Status,
    FlowBlock[] Blocks, FlowRegion[] Regions, VariableFlow Variables, MethodBoundary Boundary, DefinitionFlow Definitions,
    ControlDependence Control);
