namespace SMSR.App.Mvp;

internal static class GraphFindStyles
{
    internal const string Css = """
        main{max-width:1340px}.workspace{grid-template-columns:minmax(240px,340px) minmax(0,1fr);align-items:start;min-height:500px}
        .workspace>.panel:last-child{grid-column:auto}.results{max-height:670px}.detail-panel{min-height:500px}
        .detail-panel>details{margin:12px 18px;padding:12px;border:1px solid var(--border2);border-radius:10px}
        .detail-panel summary,.extra-tools>summary{font-weight:700;cursor:pointer}.detail-panel details .graph{margin-top:12px;height:420px}
        .details{padding:20px}.details h3{font-size:23px}.details>p{font-size:12px;color:var(--muted)}
        .role-card,.related-card{padding:16px;margin:18px 0;background:var(--surface);border:1px solid var(--border2);border-radius:12px}
        .role-card h4,.related-card h4{margin:0 0 14px;color:var(--text);font-size:16px}.role-claim p{margin:5px 0 15px;font-size:15px;line-height:1.7}
        .role-card details{border-top:1px solid var(--border2);padding-top:12px}.role-card blockquote{white-space:pre-wrap;overflow-wrap:anywhere;margin:12px 0;padding:10px;border-left:3px solid #66a7ed}
        .related-item{padding:10px 0;border-bottom:1px solid var(--border2)}.related-item small{display:block;color:var(--muted);margin:5px 0}
        .role-card button,.related-card button{color:var(--text);background:var(--panel);border:1px solid var(--border2);border-radius:8px;padding:8px 12px;text-align:left}
        .details .related-item .source-action,.role-card .source-action{font-size:12px;padding:5px 8px;margin:5px 8px 0 0}
        .extra-tools{margin-top:22px}.extra-tools>summary{padding:15px;background:var(--panel);border:1px solid var(--border2);border-radius:12px}.extra-tools>div{padding-top:12px}
        #edge-details{padding:12px}#edge-details:empty{display:none}
        #role-coverage{margin-top:16px}#role-coverage button{background:var(--surface);color:var(--text);border:1px solid var(--border2);padding:8px 12px;border-radius:8px;margin:4px}
        #role-coverage button[aria-pressed=true]{border-color:#66a7ed}#role-coverage .coverage-file{display:block;text-align:left;overflow-wrap:anywhere;max-width:100%}
        #batch-preview-content,#vault-conflicts{max-height:240px;overflow:auto;overflow-wrap:anywhere}#batch-progress{display:block;width:100%;margin:12px 0}#vault-path{min-width:260px;max-width:100%}
        @media(max-width:760px){.workspace{display:block}.results{max-height:220px}.details{padding:15px}.detail-panel{min-height:240px}}
        """;
}
