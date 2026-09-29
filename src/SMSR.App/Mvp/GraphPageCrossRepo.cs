namespace SMSR.App.Mvp;

internal static class GraphPageCrossRepo
{
    public static string Render(string projectId, string? workflowId, string search, GraphCrossRepoMap? map)
    {
        var form = $"<section class=\"panel\"><h2>저장소 간 명시적 연결</h2><p class=\"muted\">" +
            "색인된 다른 저장소의 파일로 정확히 해소된 Markdown 링크·프로젝트 참조만 연결합니다. 같은 이름만으로는 합치지 않습니다.</p>" +
            $"<form action=\"/graph\" method=\"get\"><input type=\"hidden\" name=\"projectId\" value=\"{GraphPage.Encode(projectId)}\">" +
            (workflowId is null ? "" : $"<input type=\"hidden\" name=\"workflowId\" value=\"{GraphPage.Encode(workflowId)}\">") +
            $"<input type=\"hidden\" name=\"q\" value=\"{GraphPage.Encode(search)}\"><input type=\"hidden\" name=\"crossRepo\" value=\"true\"><button>연결 보기</button></form>";
        if (map is null) return form + "</section>";
        if (map.Stale) form += "<p class=\"error\">저장소 간 연결이 오래되었거나 갱신되지 않았습니다. 색인 갱신을 다시 실행하세요. 아래 결과를 현재 연결로 확정하지 마세요.</p>";
        if (map.Edges.Count == 0) return form + "<p class=\"muted\">명시적으로 해소된 다른 저장소 연결이 없습니다.</p></section>";
        var rows = string.Join("", map.Edges.Select(edge =>
            $"<li><a href=\"{GraphPage.Url(edge.SourceProjectId, null, "", edge.SourceId)}\">{GraphPage.Encode(edge.SourceProjectId + "/" + edge.OwnerPath)}</a>" +
            $" {edge.SourceLine}행 → <a href=\"{GraphPage.Url(edge.TargetProjectId, null, "", edge.TargetId)}\">" +
            $"{GraphPage.Encode(edge.TargetProjectId + "/" + edge.TargetId[5..])}</a></li>"));
        return form + "<ul>" + rows + "</ul>" + (map.Truncated ? "<p class=\"muted\">처음 500개만 표시합니다.</p>" : "") + "</section>";
    }
}
