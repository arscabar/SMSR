using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
namespace SMSR.App.Mvp;
internal static class GraphExportWiki
{
    internal static byte[] Render(GraphKnowledgeExport data,bool obsidian)
    {
        string Name(string id)=>"node-"+Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(id))).ToLowerInvariant();
        string Text(string value)=>value.Replace("\r"," ").Replace("\n"," ").Replace("<","&lt;").Replace(">","&gt;").Replace("[","\\[").Replace("]","\\]").Replace("#","\\#");
        var names=data.Nodes.ToDictionary(n=>n.NodeId,n=>Name(n.NodeId),StringComparer.Ordinal);
        var adjacent=data.Edges.SelectMany(e=>new[]{(Id:e.SourceId,Edge:e),(Id:e.TargetId,Edge:e)}).ToLookup(p=>p.Id,p=>p.Edge,StringComparer.Ordinal);
        var groups=data.Hyperedges.SelectMany(h=>h.Members.Select(m=>(m.NodeId,Group:h))).ToLookup(p=>p.NodeId,p=>p.Group,StringComparer.Ordinal);
        using var buffer=new MemoryStream();
        using(var zip=new ZipArchive(buffer,ZipArchiveMode.Create,true))
        {
            void Write(string path,string text){using var writer=new StreamWriter(zip.CreateEntry(path).Open(),new UTF8Encoding(false));writer.Write(text);}
            Write("index.md",$"# SMSR 지식 그래프\n\n프로젝트: {Text(data.ProjectId)} · 리비전: {data.Revision}\n\n{(data.Truncated?"범위 일부만 포함":"선택한 범위 포함")} · 원문 현재 상태 미확인 · 기존 노트에 자동 덮어쓰기하지 마세요.\n\n"+string.Join("\n",data.Nodes.Select(n=>$"- [{Text(n.Label)}]({names[n.NodeId]}.md)")));
            foreach(var group in data.Overview?.Groups??[])Write($"group-{group.Id}.md","# "+Text(group.Name)+"\n\n저장 리비전 "+data.Revision+" · 응집도 "+group.Cohesion+"\n\n"+string.Join("\n",group.MemberIds.Where(names.ContainsKey).Select(id=>$"- [{names[id]}]({names[id]}.md)")));
            foreach(var node in data.Nodes)
            {
                var content=$"# {Text(node.Label)}\n\n- 종류: {Text(node.Details?.EntityKind??node.Kind)}\n- 원문: `{node.SourcePath.Replace("`","")}`:{node.Line}\n- 저장 해시: `{node.Hash}`\n- 리비전: {data.Revision}\n\n";
                if(node.Details?.Rationale is{}reason)content+="## 원문 설계 근거\n\n"+Text(reason)+"\n\n";
                content+="## 연결\n\n";
                foreach(var edge in adjacent[node.NodeId].Distinct())
                {
                    var other=edge.SourceId==node.NodeId?edge.TargetId:edge.SourceId;
                    var link=obsidian?$"[[{names[other]}]]":$"[{names[other]}]({names[other]}.md)";
                    content+=$"- {(edge.SourceId==node.NodeId?"나감":"들어옴")} · {Text(edge.Relation)} · {link} · {Text(edge.Confidence)} · `{edge.OwnerPath.Replace("`","")}`:{edge.SourceLine}\n";
                }
                content+="\n## 참여 관계\n\n";
                foreach(var group in groups[node.NodeId])content+=$"- {Text(group.Label)} · {Text(group.Confidence)} · "+string.Join(", ",group.Members.Select(m=>$"[{Text(m.Role)}]({names[m.NodeId]}.md)"))+"\n";
                Write(names[node.NodeId]+".md",content);
            }
        }
        return buffer.ToArray();
    }
}
