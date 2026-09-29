namespace SMSR.App.Mvp;

internal static class DashboardTimelineStyles
{
    public const string Css = """
        .integrity-alert{padding:10px 20px;background:#6b4310;color:#fff2d5;border-bottom:1px solid #c38b35;font-size:12px}.integrity-alert ul{margin:5px 0 0;padding-left:18px}
        .workflow-timeline{margin-top:15px;padding:12px;border:1px solid var(--border2);border-radius:10px;background:var(--surface)}.workflow-timeline h3{margin:0 0 8px;font-size:12px;color:#62adff}.workflow-timeline h3:not(:first-child){margin-top:14px}.timeline-control{display:flex;align-items:center;gap:8px;font-size:11px}.timeline-control input{min-width:0;flex:1}.timeline-control output{min-width:38px;text-align:right}
        #timeline-list{max-height:310px;overflow:auto;list-style:none;margin:10px 0 0;padding:0;font-size:11px}#timeline-list li{padding:7px 0;border-bottom:1px solid var(--border)}#timeline-list li:target{outline:2px solid #62adff}#timeline-list time{display:block;color:var(--muted)}#timeline-list p{margin:4px 0;white-space:pre-wrap;overflow-wrap:anywhere}#timeline-list details ul{padding-left:18px}.evidence-links{margin:0;padding-left:18px;font-size:11px;overflow-wrap:anywhere}.evidence-links li{margin:4px 0}
        """;
}
