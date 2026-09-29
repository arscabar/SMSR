using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis;

namespace SMSR.CSharpAnalysis;

internal sealed class ValueBuilder(Dictionary<(IOperation, bool), int> bindings, VariableSite[] sites)
{
    internal readonly List<ValueNode> Nodes = sites.Select(s => new ValueNode(s.Id,s.Block,s.Kind,s.Source,"LOCAL_VALUE",s.SymbolId)).ToList();
    internal readonly List<ValueLink> Links = [];
    internal readonly List<ValuePort> Ports = [];
    internal readonly Dictionary<IOperation,int> Results = new(ReferenceEqualityComparer.Instance);
    internal readonly List<VariableSite> Captures = [];
    internal readonly List<int> CaptureValues = [];
    private readonly Dictionary<CaptureId,int> captureIds = [];

    internal int Variable(IOperation op, bool write) => bindings[(op,write)];
    internal int Add(IOperation op, int block, string kind, string status="LOCAL_VALUE", string? symbol=null)
    {
        Check(); var id=Nodes.Count;
        Nodes.Add(new(id,block,kind,SymbolFacts.Source(op.Syntax),status,symbol)); return id;
    }
    internal void Link(int from,int to,string relation) { Check(); Links.Add(new(from,to,relation)); }
    internal void Port(ValuePort port) { Check(); Ports.Add(port); }
    internal void Capture(CaptureId key,int value,int block,bool write,Span source)
    {
        if(!captureIds.TryGetValue(key,out var id)) { id=captureIds.Count; captureIds.Add(key,id); }
        Captures.Add(new(Captures.Count,block,"capture:"+id,write?"WRITE":"READ",source));
        CaptureValues.Add(value);
    }
    private void Check()
    {
        if(Nodes.Count+Links.Count+Ports.Count>=50_000) throw new ArgumentException("Value graph limit exceeded");
    }
}
