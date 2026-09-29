using System.Text;

namespace SMSR.App.Mvp;

internal static partial class GraphPageSections
{
    private static string FeedbackButtons(string projectId, GraphEdge edge)
    {
        var attributes = $"data-project=\"{GraphPage.Encode(projectId)}\" data-source=\"{GraphPage.Encode(edge.SourceId)}\" data-target=\"{GraphPage.Encode(edge.TargetId)}\" data-relation=\"{GraphPage.Encode(edge.Relation)}\" data-owner=\"{GraphPage.Encode(edge.OwnerPath)}\" data-line=\"{edge.SourceLine}\"";
        return $"<button type=\"button\" {attributes} data-feedback=\"USEFUL\" aria-label=\"이 관계가 유용함\">유용</button> <button type=\"button\" {attributes} data-feedback=\"ERROR\" aria-label=\"이 관계가 오류임\">오류</button> <button type=\"button\" {attributes} data-feedback=\"CORRECTED\" aria-label=\"이 관계의 수정 확인\">수정됨</button>";
    }

    private static string FeedbackHistory(IReadOnlyList<GraphFeedback>? feedback)
    {
        if (feedback is null || feedback.Count == 0) return "";
        var html = new StringBuilder("<h3>관계 피드백</h3><ul>");
        foreach (var item in feedback)
            html.Append($"<li>{GraphPage.Encode(item.Verdict)} · {GraphPage.Encode(item.Relation)} → {GraphPage.Encode(item.TargetId)} · {GraphPage.Encode(item.OwnerPath)}:{item.SourceLine} · {item.CreatedAt.ToLocalTime():yyyy-MM-dd HH:mm}{(item.Stale ? " <span class=\"badge\">오래됨</span>" : "")}</li>");
        return html.Append("</ul>").ToString();
    }
}
