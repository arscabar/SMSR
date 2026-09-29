using System.Text;

namespace SMSR.App.Mvp;

internal static partial class GraphPageSections
{
    public static string Routes(string projectId, string? workflowId, string q, GraphRouteMap? map)
    {
        var html = new StringBuilder($"<section class=\"panel\"><h2>MCP/API 경로 지도</h2><p class=\"muted\">정적 선언의 경로·도구 이름만 표시합니다. 설정 값·인수·토큰은 색인하지 않습니다.</p><form action=\"/graph\" method=\"get\"><input type=\"hidden\" name=\"projectId\" value=\"{GraphPage.Encode(projectId)}\">");
        if (workflowId is not null) html.Append($"<input type=\"hidden\" name=\"workflowId\" value=\"{GraphPage.Encode(workflowId)}\">");
        if (q.Length > 0) html.Append($"<input type=\"hidden\" name=\"q\" value=\"{GraphPage.Encode(q)}\">");
        html.Append("<input type=\"hidden\" name=\"routes\" value=\"true\"><button>경로 지도 보기</button></form>");
        if (map is null) return html.Append("</section>").ToString();
        html.Append($"<p class=\"muted\">리비전 {map.Revision}</p><ul>");
        foreach (var route in map.Routes)
            html.Append($"<li><span class=\"badge\">{GraphPage.Encode(route.Kind)}</span> <a href=\"{GraphPage.Url(projectId, workflowId, q, route.NodeId)}\">{GraphPage.Encode(route.Label)}</a> · <a href=\"{GraphPage.SourceUrl(projectId, route.SourcePath, route.Line)}\">{GraphPage.Encode(route.SourcePath)}:{route.Line}</a></li>");
        html.Append("</ul>");
        if (map.Truncated) html.Append("<p class=\"muted\">첫 200개만 표시합니다.</p>");
        return html.Append("</section>").ToString();
    }
}
