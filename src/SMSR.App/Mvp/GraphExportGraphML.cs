using System.Xml.Linq;
namespace SMSR.App.Mvp;
internal static class GraphExportGraphML
{
    internal static string Render(GraphKnowledgeExport data)
    {
        XNamespace ns="http://graphml.graphdrawing.org/xmlns";
        XElement Value(string key,string? value)=>new(ns+"data",new XAttribute("key",key),value??"");
        var graph=new XElement(ns+"graph",new XAttribute("id","SMSR"),new XAttribute("edgedefault","directed"),Value("revision",data.Revision.ToString()));
        graph.Add(Value("scope",data.Scope),Value("truncated",data.Truncated.ToString()));
        foreach(var n in data.Nodes)graph.Add(new XElement(ns+"node",new XAttribute("id",n.NodeId),Value("label",n.Label),Value("kind",n.Details?.EntityKind??n.Kind),Value("path",n.SourcePath),Value("line",n.Line.ToString()),Value("hash",n.Hash),Value("details",System.Text.Json.JsonSerializer.Serialize(n.Details))));
        var i=0;foreach(var e in data.Edges)graph.Add(new XElement(ns+"edge",new XAttribute("id","e"+i++),new XAttribute("source",e.SourceId),new XAttribute("target",e.TargetId),Value("label",e.Relation),Value("confidence",e.Confidence),Value("path",e.OwnerPath),Value("line",e.SourceLine.ToString()),Value("evidence",System.Text.Json.JsonSerializer.Serialize(e.Evidence))));
        foreach(var h in data.Hyperedges)graph.Add(new XElement(ns+"hyperedge",new XAttribute("id",h.HyperedgeId),Value("label",h.Label),Value("confidence",h.Confidence),Value("path",h.OwnerPath),Value("line",h.SourceLine.ToString()),Value("evidence",System.Text.Json.JsonSerializer.Serialize(h.Evidence)),h.Members.Select(m=>new XElement(ns+"endpoint",new XAttribute("node",m.NodeId),new XAttribute("type","undir"),Value("role",m.Role)))));
        return new XDocument(new XElement(ns+"graphml",new[]{"revision","scope","truncated","label","kind","path","line","hash","confidence","evidence","details","role"}.Select(k=>new XElement(ns+"key",new XAttribute("id",k),new XAttribute("for","all"),new XAttribute("attr.name",k),new XAttribute("attr.type","string"))),graph)).ToString();
    }
}
