using System.Text;

namespace SMSR.App.Mvp;

internal static partial class GraphPageSections
{
    public static string Communities(string projectId, string? workflowId, string q,
        int nodeCount, GraphCommunities? report)
    {
        if (nodeCount < 5000 && report is null) return "";
        var html = new StringBuilder($"<section class=\"panel\"><h2>대형 그래프 커뮤니티 축약</h2><form action=\"/graph\" method=\"get\"><input type=\"hidden\" name=\"projectId\" value=\"{GraphPage.Encode(projectId)}\">");
        if (workflowId is not null) html.Append($"<input type=\"hidden\" name=\"workflowId\" value=\"{GraphPage.Encode(workflowId)}\">");
        if (q.Length > 0) html.Append($"<input type=\"hidden\" name=\"q\" value=\"{GraphPage.Encode(q)}\">");
        html.Append("<input type=\"hidden\" name=\"communities\" value=\"true\"><button>커뮤니티 요약</button></form>");
        if (report is null) return html.Append("</section>").ToString();
        if (report.ScannedNodes == 0 && report.Truncated)
            return html.Append("<p class=\"muted\">분석 상한(노드 10만/관계 30만)을 초과했습니다.</p></section>").ToString();
        html.Append($"<p class=\"muted\">리비전 {report.Revision} · 그룹 {report.TotalGroups}개 · 고립 노드 {report.IsolatedNodes}개 · 원본 관계는 유지됩니다.</p><ul>");
        foreach (var group in report.Groups)
        {
            html.Append($"<li>그룹 {group.Id} · {group.NodeCount}개: ");
            foreach (var id in group.SampleNodeIds)
                html.Append($" <a href=\"{GraphPage.Url(projectId, workflowId, q, id)}\">{GraphPage.Encode(id)}</a>");
            if (group.NodeCount > group.SampleNodeIds.Count) html.Append(" 외");
            html.Append("</li>");
        }
        html.Append("</ul>");
        if (report.Truncated) html.Append("<p class=\"muted\">상위 50개 그룹만 표시합니다.</p>");
        return html.Append("</section>").ToString();
    }
}
