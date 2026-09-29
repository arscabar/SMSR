using System.Text;

namespace SMSR.App.Mvp;

internal static class GraphSourcePage
{
    public static string Render(string projectId, GraphSourcePreview source, string? theme, string? workflowId = null)
    {
        var rows = new StringBuilder();
        for (var i = 0; i < source.Lines.Count; i++)
        {
            var number = source.FirstLine + i;
            var selected = number == source.SelectedLine ? " class=\"selected\"" : "";
            rows.Append($"<div{selected}><span>{number}</span><code>{GraphPage.Encode(source.Lines[i])}</code></div>");
        }
        return $$"""
            <!doctype html><html lang="ko"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
            <title>근거 파일 · SMSR</title><style>{{GraphPageStyles.For(theme)}}{{WebNavigation.Styles}}
            .source{overflow:auto}.source div{display:flex;min-width:max-content;white-space:pre;font:13px/1.6 ui-monospace,monospace}.source span{position:sticky;left:0;min-width:56px;padding-right:12px;text-align:right;color:var(--muted);background:var(--panel);user-select:none}.source code{padding-right:20px}.source .selected,.source .selected span{background:var(--active)}
            </style></head><body>{{WebNavigation.Render(projectId, workflowId, "find")}}<main><header><div><h1>근거 파일</h1><p class="muted">{{GraphPage.Encode(source.Path)}} · 리비전 {{source.Revision}} · {{source.SelectedLine}}줄 주변</p></div><a href="/graph/explore?projectId={{Uri.EscapeDataString(projectId)}}{{(workflowId is null ? "" : "&amp;workflowId=" + Uri.EscapeDataString(workflowId))}}">← 찾기</a></header>
            <section class="panel source" aria-label="파일 내용">{{rows}}</section><p class="muted">읽기 전용 미리보기 · 색인 당시 해시와 일치하는 파일만 표시합니다.</p></main></body></html>
            """;
    }
}
