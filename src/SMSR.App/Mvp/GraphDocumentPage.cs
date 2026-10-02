using System.Text;

namespace SMSR.App.Mvp;

internal static class GraphDocumentPage
{
    internal static string Render(string projectId, string path, GraphDocument doc, string? location, string? workflowId)
    {
        var rows = new StringBuilder();
        var source="/graph/document?projectId="+Uri.EscapeDataString(projectId)+"&path="+Uri.EscapeDataString(path)
            +(workflowId is null?"":"&workflowId="+Uri.EscapeDataString(workflowId));
        foreach (var block in doc.Blocks)
                rows.Append($"<article{(block.Location == location ? " id=\"selected-evidence\"" : "")} class=\"panel{(block.Location == location ? " selected" : "")}\"><h3><a href=\"{GraphPage.Encode(source+"&location="+Uri.EscapeDataString(block.Location))}\">{GraphPage.Encode(block.Location)}</a></h3><pre>{GraphPage.Encode(block.Text)}</pre></article>");
        return $$"""
            <!doctype html><html lang="ko"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
            <title>문서 근거 · SMSR</title><style>{{GraphPageStyles.For(null)}}{{WebNavigation.Styles}}pre{white-space:pre-wrap;overflow-wrap:anywhere}.selected{outline:2px solid #62adff;border-left:6px solid #62adff}</style></head>
            <body>{{WebNavigation.Render(projectId, workflowId, "find")}}<main><header><h1>{{GraphPage.Encode(path)}}</h1></header>
            <p>문서 근거 · 리비전 {{doc.Revision}} · {{GraphPage.Encode(doc.Status)}} · 제외 구간 {{doc.ExcludedBlocks}}개</p>
            {{GraphMediaEvidencePage.Render(projectId,path,doc,location)}}
            {{rows}}<p>읽기 전용 · 인용 위치 확인용 본문입니다. OCR·전사는 기계 인식 근거이며 정확성 보장이 아닙니다. 영상은 표본 프레임입니다.</p></main></body></html>
            """;
    }
}
