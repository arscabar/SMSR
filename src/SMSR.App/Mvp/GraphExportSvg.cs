using System.Text;
using System.Xml.Linq;
namespace SMSR.App.Mvp;
internal static class GraphExportSvg
{
    internal static string Render(GraphKnowledgeExport data)
    {
        if(data.Nodes.Count>500||data.Edges.Count>2000)throw new InvalidOperationException("SVG는 500개 노드·2000개 관계까지 제공합니다. 선택 범위를 줄이세요.");
        XNamespace ns="http://www.w3.org/2000/svg";
        var columns=Math.Max(1,Math.Min(6,data.Nodes.Count));var height=100+100*((data.Nodes.Count+columns-1)/columns);
        var svg=new XElement(ns+"svg",new XAttribute("viewBox",$"0 0 {columns*240} {height}"),new XAttribute("role","img"),
            new XElement(ns+"title",$"{data.ProjectId} · 리비전 {data.Revision} · 저장된 구조"),
            new XElement(ns+"metadata",System.Text.Json.JsonSerializer.Serialize(data,GraphWorker.Json)),
            new XElement(ns+"rect",new XAttribute("width","100%"),new XAttribute("height","100%"),new XAttribute("fill","#101722")));
        var positions=data.Nodes.Select((n,i)=>(n.NodeId,X:20+240*(i%columns),Y:60+100*(i/columns))).ToDictionary(p=>p.NodeId);
        foreach(var e in data.Edges)
        {
            var a=positions[e.SourceId];var b=positions[e.TargetId];
            svg.Add(new XElement(ns+"line",new XAttribute("x1",a.X+100),new XAttribute("y1",a.Y+32),new XAttribute("x2",b.X+100),new XAttribute("y2",b.Y+32),
                new XAttribute("stroke",e.Confidence=="INFERRED"?"#ffc36b":"#6a91b9"),new XElement(ns+"title",$"{e.SourceId} → {e.Relation} → {e.TargetId} · {e.Confidence}")));
        }
        foreach(var n in data.Nodes)
        {
            var p=positions[n.NodeId];svg.Add(new XElement(ns+"g",new XElement(ns+"title",n.Label+" · "+n.SourcePath+":"+n.Line),
                new XElement(ns+"rect",new XAttribute("x",p.X),new XAttribute("y",p.Y),new XAttribute("width",200),new XAttribute("height",64),new XAttribute("rx",8),new XAttribute("fill","#203a57"),new XAttribute("stroke","#83beff")),
                new XElement(ns+"text",new XAttribute("x",p.X+10),new XAttribute("y",p.Y+30),new XAttribute("fill","white"),new XAttribute("font-size",13),n.Label.Length>22?n.Label[..22]+"…":n.Label)));
        }
        return svg.ToString();
    }
}
