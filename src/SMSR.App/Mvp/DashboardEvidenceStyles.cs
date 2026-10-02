namespace SMSR.App.Mvp;

internal static class DashboardEvidenceStyles
{
    public const string Css = """
        .evidence-disclosure{margin-top:14px;border:1px solid var(--border2);border-radius:12px;background:var(--surface);overflow:hidden}
        .evidence-disclosure>summary{display:flex;align-items:center;gap:9px;padding:12px;cursor:pointer;list-style:none}
        .evidence-disclosure>summary::-webkit-details-marker{display:none}.evidence-disclosure>summary:before{content:'›';font-size:21px;color:var(--muted)}.evidence-disclosure[open]>summary:before{transform:rotate(90deg)}
        .evidence-disclosure>summary:hover{background:var(--active)}.evidence-disclosure>summary:focus-visible,.evidence-reference:focus-visible{outline:2px solid var(--text);outline-offset:-3px;border-radius:8px}
        .evidence-icon{display:grid;place-items:center;flex:none;width:32px;height:32px;border-radius:9px;background:var(--active);color:var(--text);font-size:20px}
        .evidence-heading{min-width:0;flex:1}.evidence-heading strong{display:block;font-size:13px}.evidence-heading small{display:block;margin-top:3px;font-size:10px;color:var(--muted)}
        .evidence-count{flex:none;padding:4px 8px;border:1px solid var(--border2);border-radius:999px;background:var(--bg);font-size:11px;font-weight:700;font-variant-numeric:tabular-nums}
        .evidence-disclosure>.empty,.evidence-limit{margin:0;padding:0 12px 12px}.evidence-disclosure[open]>summary{border-bottom:1px solid var(--border)}
        .evidence-links{display:grid;gap:9px;list-style:none;margin:0;padding:12px}.evidence-links .evidence-card{margin:0;padding:11px;border:1px solid var(--border);border-radius:9px;background:var(--bg)}
        .evidence-kind{display:block;margin-bottom:6px;color:var(--muted);font-size:10px}.evidence-reference{display:block;color:var(--text);font-size:12px;font-weight:600;line-height:1.5;overflow-wrap:anywhere;text-decoration:none}
        a.evidence-reference:hover{text-decoration:underline}.evidence-action{display:block;margin-top:5px;font-size:10px;font-weight:400;color:var(--muted)}
        .evidence-meta{display:flex;align-items:start;gap:9px;margin-top:10px;padding-top:8px;border-top:1px solid var(--border);color:var(--muted);font-size:10px;line-height:1.5}.evidence-meta span{flex:1;min-width:0;overflow-wrap:anywhere}.evidence-meta time{flex:none;font-variant-numeric:tabular-nums}
        #details>.evidence-disclosure:first-child{margin-top:0}.panel-disclosure-body{padding:12px}
        .panel-disclosure-body>.workflow-context,.panel-disclosure-body>.detail-card,.panel-disclosure-body>.workflow-timeline{border:0;padding:0;margin:0;border-radius:0;background:none}
        .panel-disclosure-body>.workflow-timeline>h3{display:none}.panel-disclosure-body>.empty{margin:0}
        .panel-disclosure-body .history-tools{margin:0 0 8px}.panel-disclosure-body .activity{margin:0}
        """;
}
