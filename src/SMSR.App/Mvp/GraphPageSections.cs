using System.Text;

namespace SMSR.App.Mvp;

internal static partial class GraphPageSections
{
    public static string Cycles(string projectId, string? workflowId, string q, GraphCycles? report)
    {
        if (report is null) return "<p class=\"muted\">검사 전입니다.</p>";
        if (report.ScannedNodes == 0 && report.Truncated)
            return "<p class=\"muted\">색인이 분석 상한을 초과해 순환 여부를 판정하지 않았습니다.</p>";
        if (report.TotalGroups == 0) return "<p class=\"muted\">순환 연결 그룹이 없습니다.</p>";
        var html = new StringBuilder($"<p class=\"muted\">리비전 {report.Revision} · 순환 연결 그룹 {report.TotalGroups}개</p><ul>");
        foreach (var group in report.Groups)
        {
            html.Append($"<li>{group.NodeCount}개 노드: ");
            foreach (var nodeId in group.NodeIds)
                html.Append($" <a href=\"{GraphPage.Url(projectId, workflowId, q, nodeId)}\">{GraphPage.Encode(nodeId)}</a>");
            if (group.NodeCount > group.NodeIds.Count) html.Append(" 외");
            html.Append("</li>");
        }
        html.Append("</ul>");
        if (report.Truncated) html.Append("<p class=\"muted\">최대 20개 그룹만 표시합니다.</p>");
        return html.ToString();
    }

    public static string Issues(string projectId, string? workflowId, string q, GraphHealth? health)
    {
        if (health?.Issues.Count is null or 0) return "";
        var html = new StringBuilder("<section class=\"panel\"><h2>관계 진단 · 미해소 참조</h2><ul>");
        foreach (var issue in health.Issues)
        {
            html.Append($"<li><a href=\"{GraphPage.SourceUrl(projectId, issue.OwnerPath, issue.SourceLine)}\">{GraphPage.Encode(issue.OwnerPath)}:{issue.SourceLine}</a> · {GraphPage.Encode(issue.Relation)} · {GraphPage.Encode(issue.Reason)}");
            if (issue.CandidatePaths.Count > 0)
            {
                html.Append("<br><span class=\"kind\">확인할 후보: </span>");
                foreach (var candidate in issue.CandidatePaths)
                    html.Append($" <a href=\"{GraphPage.Url(projectId, workflowId, q, "file:" + candidate)}\">{GraphPage.Encode(candidate)}</a>");
            }
            html.Append("</li>");
        }
        html.Append("</ul>");
        if (health.IssueCount > health.Issues.Count)
            html.Append("<p class=\"muted\">최대 100건만 표시합니다.</p>");
        return html.Append("</section>").ToString();
    }

    public static string Evidence(string projectId, string? workflowId, string q, GraphEvidence? evidence)
    {
        if (workflowId is null) return "";
        if (evidence is null || evidence.Matches.Count == 0)
            return "<section class=\"panel\"><h2>이 작업의 산출물</h2><p class=\"muted\">기록된 파일 근거가 없습니다.</p></section>";
        var html = new StringBuilder("<section class=\"panel\"><h2>이 작업의 산출물</h2><ul>");
        foreach (var item in evidence.Matches)
        {
            var reference = GraphPage.Encode(item.Reference);
            var label = item.FileNode is null ? reference + " <span class=\"badge\">미해소</span>"
                : $"<a href=\"{GraphPage.Url(projectId, workflowId, q, item.FileNode.NodeId)}\">{reference}</a> <span class=\"badge\">파일 확인</span>";
            if (item.FileNode is not null) label += $" · <a href=\"{GraphPage.SourceUrl(projectId, item.FileNode.SourcePath, item.FileNode.Line)}\">원문 열기</a>";
            html.Append($"<li>{label} <span class=\"kind\">작업 노드 {GraphPage.Encode(item.WorkflowNodeId)} · 이벤트 {GraphPage.Encode(item.EventId)}</span></li>");
        }
        html.Append("</ul>");
        if (evidence.Truncated) html.Append("<p class=\"muted\">최근 200개만 표시합니다.</p>");
        return html.Append("</section>").ToString();
    }

    public static string Search(string projectId, string? workflowId, string q, GraphSearch? search)
    {
        if (search is null) return "<p class=\"muted\">색인 후 파일과 문서를 탐색할 수 있습니다.</p>";
        if (search.Nodes.Count == 0) return "<p class=\"muted\">일치하는 항목이 없습니다.</p>";
        var html = new StringBuilder("<ul>");
        foreach (var node in search.Nodes)
            html.Append($"<li><a class=\"item\" href=\"{GraphPage.Url(projectId, workflowId, q, node.NodeId)}\"><strong>{GraphPage.Encode(node.Label)}</strong><br><span class=\"kind\">{GraphPage.Encode(node.Kind)} · {GraphPage.Encode(node.SourcePath)}:{node.Line}</span></a></li>");
        return html.Append(search.Truncated ? "</ul><p class=\"muted\">일부 결과만 표시합니다. 검색어를 좁혀주세요.</p>" : "</ul>").ToString();
    }

    public static string Context(string projectId, string? workflowId, string q,
        GraphContext? context, GraphPath? path, GraphImpact? impact,
        IReadOnlyList<GraphFeedback>? feedback = null)
    {
        if (context is null) return "<p class=\"muted\">왼쪽에서 파일이나 문서 제목을 선택하세요.</p>";
        var html = new StringBuilder();
        var exportUrl = GraphPage.Encode("/api/graph/export?projectId=" + Uri.EscapeDataString(projectId)
            + "&nodeId=" + Uri.EscapeDataString(context.Node.NodeId) + "&depth=2&direction=both");
        html.Append($"<h3>{GraphPage.Encode(context.Node.Label)}</h3><p class=\"muted\">{GraphPage.Encode(context.Node.SourcePath)}:{context.Node.Line} · {GraphPage.Encode(context.Node.Kind)} · <a href=\"{GraphPage.SourceUrl(projectId, context.Node.SourcePath, context.Node.Line)}\">근거 파일 열기</a> · <a href=\"{exportUrl}\">선택 범위 내보내기</a></p>");
        html.Append("<form action=\"/graph\" method=\"get\">");
        html.Append($"<input type=\"hidden\" name=\"projectId\" value=\"{GraphPage.Encode(projectId)}\"><input type=\"hidden\" name=\"nodeId\" value=\"{GraphPage.Encode(context.Node.NodeId)}\">");
        if (workflowId is not null) html.Append($"<input type=\"hidden\" name=\"workflowId\" value=\"{GraphPage.Encode(workflowId)}\">");
        if (q.Length > 0) html.Append($"<input type=\"hidden\" name=\"q\" value=\"{GraphPage.Encode(q)}\">");
        html.Append("<input name=\"toId\" aria-label=\"도착 노드 ID\" placeholder=\"도착 노드 ID\"><button>경로 찾기</button></form>");
        if (path is not null)
        {
            html.Append($"<h3>방향 경로 · {(path.Found ? path.Edges.Count + "단계" : "찾지 못함")}</h3>");
            if (path.Truncated) html.Append("<p class=\"muted\">깊이·노드 상한에서 탐색이 중단됐습니다.</p>");
            foreach (var edge in path.Edges) html.Append(EdgeText(projectId, workflowId, q, edge));
        }
        AppendNeighbors(html, "나가는 관계", projectId, workflowId, q, context.Outgoing);
        AppendNeighbors(html, "들어오는 관계", projectId, workflowId, q, context.Incoming);
        if (context.Truncated) html.Append("<p class=\"muted\">직접 관계 일부만 표시합니다.</p>");
        html.Append("<h3>역방향 영향</h3><ul>");
        foreach (var node in impact?.Nodes ?? [])
            html.Append($"<li><a href=\"{GraphPage.Url(projectId, workflowId, q, node.NodeId)}\">{GraphPage.Encode(node.Label)}</a> <span class=\"kind\">{GraphPage.Encode(node.SourcePath)}</span></li>");
        html.Append("</ul>");
        if (impact?.Truncated == true) html.Append("<p class=\"muted\">영향 범위 일부만 표시합니다.</p>");
        html.Append(FeedbackHistory(feedback));
        return html.ToString();
    }

    private static void AppendNeighbors(StringBuilder html, string title, string projectId,
        string? workflowId, string q, IReadOnlyList<GraphNeighbor> neighbors)
    {
        html.Append($"<h3>{title} ({neighbors.Count})</h3><ul>");
        foreach (var neighbor in neighbors)
            html.Append($"<li><a href=\"{GraphPage.Url(projectId, workflowId, q, neighbor.Node.NodeId)}\">{GraphPage.Encode(neighbor.Node.Label)}</a> <span class=\"badge\">{GraphPage.Encode(neighbor.Edge.Relation)} · {GraphPage.Encode(neighbor.Edge.Resolution)}</span><br><span class=\"kind\"><a href=\"{GraphPage.SourceUrl(projectId, neighbor.Edge.OwnerPath, neighbor.Edge.SourceLine)}\">{GraphPage.Encode(neighbor.Edge.OwnerPath)}:{neighbor.Edge.SourceLine}</a> · {GraphPage.Encode(neighbor.Edge.Confidence)}</span> {FeedbackButtons(projectId, neighbor.Edge)}</li>");
        html.Append("</ul>");
    }

    private static string EdgeText(string projectId, string? workflowId, string q, GraphEdge edge)
        => $"<p><a href=\"{GraphPage.Url(projectId, workflowId, q, edge.SourceId)}\">{GraphPage.Encode(edge.SourceId)}</a> → <a href=\"{GraphPage.Url(projectId, workflowId, q, edge.TargetId)}\">{GraphPage.Encode(edge.TargetId)}</a> <span class=\"badge\">{GraphPage.Encode(edge.Relation)} · {GraphPage.Encode(edge.Resolution)}</span><br><span class=\"kind\"><a href=\"{GraphPage.SourceUrl(projectId, edge.OwnerPath, edge.SourceLine)}\">{GraphPage.Encode(edge.OwnerPath)}:{edge.SourceLine}</a></span></p>";
}
