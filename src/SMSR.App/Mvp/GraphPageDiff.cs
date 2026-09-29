using System.Text;

namespace SMSR.App.Mvp;

internal static partial class GraphPageSections
{
    public static string Diff(string projectId, string? workflowId, string q, int latest, GraphDiff? diff)
    {
        if (latest < 2) return "";
        var html = new StringBuilder($"<section class=\"panel\"><h2>변경 전후 그래프 차이</h2><form action=\"/graph\" method=\"get\"><input type=\"hidden\" name=\"projectId\" value=\"{GraphPage.Encode(projectId)}\">");
        if (workflowId is not null) html.Append($"<input type=\"hidden\" name=\"workflowId\" value=\"{GraphPage.Encode(workflowId)}\">");
        if (q.Length > 0) html.Append($"<input type=\"hidden\" name=\"q\" value=\"{GraphPage.Encode(q)}\">");
        html.Append($"<label>이전 리비전 <input type=\"number\" name=\"fromRevision\" min=\"1\" max=\"{latest - 1}\" value=\"{diff?.FromRevision ?? latest - 1}\"></label><label>이후 리비전 <input type=\"number\" name=\"toRevision\" min=\"2\" max=\"{latest}\" value=\"{diff?.ToRevision ?? latest}\"></label><button>차이 보기</button></form>");
        if (diff is null) return html.Append("</section>").ToString();
        html.Append($"<p class=\"muted\">노드 변경 {diff.NodeChangeCount}개 · 관계 변경 {diff.EdgeChangeCount}개</p><ul>");
        foreach (var item in diff.Nodes)
        {
            var node = item.After ?? item.Before!;
            html.Append($"<li>{GraphPage.Encode(item.Change)} · <a href=\"{GraphPage.Url(projectId, workflowId, q, node.NodeId)}\">{GraphPage.Encode(node.NodeId)}</a> · {GraphPage.Encode(node.SourcePath)}:{node.Line}</li>");
        }
        foreach (var item in diff.Edges)
        {
            var edge = item.After ?? item.Before!;
            html.Append($"<li>{GraphPage.Encode(item.Change)} · {GraphPage.Encode(edge.SourceId)} → {GraphPage.Encode(edge.TargetId)} · {GraphPage.Encode(edge.Relation)} · <a href=\"{GraphPage.SourceUrl(projectId, edge.OwnerPath, edge.SourceLine)}\">{GraphPage.Encode(edge.OwnerPath)}:{edge.SourceLine}</a></li>");
        }
        html.Append("</ul>");
        if (diff.Truncated) html.Append("<p class=\"muted\">각 종류의 첫 100개 변경만 표시합니다.</p>");
        return html.Append("</section>").ToString();
    }
}
