namespace SMSR.App.Mvp;

internal static class DashboardStyles
{
    public static string For(string? theme) => DashboardPalette.Resolve(theme) + Css + DashboardGraphStyles.Css;

    private const string Css = """
        *{box-sizing:border-box}
        body{margin:0;min-width:1100px;font-family:system-ui,sans-serif;background:var(--bg);color:var(--text)}
        a{color:inherit}header{height:86px;padding:12px 20px;display:flex;align-items:center;justify-content:space-between;border-bottom:1px solid var(--border);background:var(--panel)}
        h1,h2{margin:0}h1{font-size:18px}h2{font-size:15px;margin-bottom:9px}.muted{color:var(--muted);font-size:12px}
        .summary{display:flex;flex-wrap:wrap;justify-content:flex-end;gap:7px}.chip{padding:5px 9px;border:1px solid var(--border2);border-radius:999px;font-size:11px;font-weight:700}
        #alert{padding:10px 20px;background:#5a1e28;color:#ffd8dc;border-bottom:1px solid #9d3444}#alert strong{margin-right:6px}#alert a{font-weight:700}
        main{display:grid;grid-template-columns:280px minmax(430px,1fr) minmax(360px,400px);height:calc(100vh - 86px)}
        aside,section{min-width:0;overflow:auto}#agents,#details{padding:16px;background:var(--panel)}
        #agents{border-right:1px solid var(--border)}#details{border-left:1px solid var(--border)}#flow{padding:18px;background:var(--bg)}
        .flow-heading{display:flex;justify-content:space-between;margin-bottom:12px}.breadcrumb{display:flex;gap:7px;align-items:center;margin:3px 0 7px;font-size:12px;color:var(--muted)}
        .breadcrumb a{color:#62adff;text-decoration:none}.agent{padding:13px;margin:9px 0;border:1px solid var(--border2);border-radius:10px;background:var(--surface)}
        .agent.active{border-color:#61a8ff;background:var(--active)}.agent.error{border-color:#dd5668}.agent.stale{border-color:#a4937d}.agent-line{display:flex;align-items:center;justify-content:space-between;gap:8px}
        .agent-name{font-weight:700}.agent-focus{margin-top:12px;padding:10px;border-radius:8px;background:var(--bg)}.agent-focus span,.agent-facts span{display:block;margin-bottom:4px;color:var(--muted);font-size:10px}.agent-focus strong{display:block;font-size:13px;line-height:1.4}.agent-facts{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:8px;margin-top:9px}.agent-facts div{min-width:0;padding:8px;border:1px solid var(--border);border-radius:7px}.agent-facts strong{font-size:12px;overflow-wrap:anywhere}
        .badge{padding:3px 7px;border-radius:999px;background:var(--pending-stroke);font-size:10px;font-weight:700}.active .badge{background:#124d88;color:#cce7ff}.error .badge{background:#7f2837;color:#ffe0e3}.stale .badge{background:#665b4e;color:#fff1df}.task{margin-top:7px}
        .tech-details{margin-top:10px;color:var(--muted);font-size:11px}.tech-details summary{cursor:pointer}.tech-details code,.tech-details span{display:block;margin-top:5px;overflow-wrap:anywhere}
        #graph{min-height:500px;padding:20px;overflow:auto;border:1px solid var(--border);border-radius:10px;background:var(--graph)}
        .detail-card{padding:14px;border:1px solid var(--border2);border-radius:10px;background:var(--surface)}.detail-head{display:flex;align-items:flex-start;justify-content:space-between;gap:12px}.detail-head h3{margin:0;font-size:15px;line-height:1.35}.detail-status{flex:none;padding:4px 8px;border-radius:999px;background:var(--pending-stroke);font-size:10px;font-weight:700}.detail-status.active{background:#124d88;color:#cce7ff}.detail-status.success{background:#16613d;color:#d4ffe6}.detail-status.error{background:#7f2837;color:#ffe0e3}.detail-status.cancelled{background:#665b4e;color:#fff1df}
        .detail-meta{display:flex;align-items:center;justify-content:space-between;gap:10px;margin:12px 0;padding:9px;border:1px solid var(--border);border-radius:8px}.detail-meta div>span{display:block;margin-bottom:3px;color:var(--muted);font-size:10px}.detail-meta strong,.detail-meta>span{font-size:12px}.detail-meta>span{color:var(--muted);white-space:nowrap}.detail-section{margin-top:10px;padding:11px;border:1px solid var(--border);border-radius:8px;background:var(--bg)}.detail-section.primary{border-left:3px solid #62adff}.detail-section h4{margin:0 0 6px;color:#62adff;font-size:11px}.detail-section p{margin:0;white-space:pre-wrap;overflow-wrap:anywhere;font-size:12px;line-height:1.55}.detail-section ul{margin:0;padding-left:17px;font-size:11px;overflow-wrap:anywhere}.detail-criteria{margin-top:11px;color:var(--muted);font-size:11px}.detail-criteria summary{cursor:pointer}.detail-criteria p{margin:7px 0 0;white-space:pre-wrap;overflow-wrap:anywhere;line-height:1.5}.detail-updated{display:block;margin-top:13px;color:var(--muted);font-size:10px}
        .history-title{margin-top:20px}.history{padding-left:19px;color:var(--muted);font-size:12px}.history li{margin:7px 0}.activity li.running{color:var(--text)}.running-time{color:#62adff;font-weight:700}.empty{color:var(--muted);font-size:13px}
        .history-tools{display:flex;justify-content:flex-end;margin:-4px 0 8px}.history-tools button{padding:5px 9px;border:1px solid var(--border2);border-radius:7px;background:var(--surface);color:var(--text);cursor:pointer}
        .status-cards{display:grid;gap:8px}.status-card{border:1px solid var(--border2);border-left:3px solid var(--pending-stroke);border-radius:8px;background:var(--surface);overflow:hidden}
        .status-card.active{border-left-color:#61a8ff}.status-card.success{border-left-color:#48b97b}.status-card.error{border-left-color:#dd5668}.status-card.cancelled{border-left-color:#a4937d}
        .status-card summary{display:flex;align-items:center;gap:8px;padding:9px 10px;cursor:pointer;list-style:none}.status-card summary::-webkit-details-marker{display:none}
        .status-card summary:before{content:'›';font-size:18px;color:var(--muted);transition:transform .15s}.status-card[open] summary:before{transform:rotate(90deg)}
        .status-card-title{min-width:0;flex:1;font-size:12px;font-weight:700;overflow:hidden;text-overflow:ellipsis;white-space:nowrap}.status-badge{padding:3px 7px;border-radius:999px;background:var(--pending-stroke);font-size:10px;font-weight:700}
        .status-card.active .status-badge{background:#124d88;color:#cce7ff}.status-card.success .status-badge{background:#16613d;color:#d4ffe6}.status-card.error .status-badge{background:#7f2837;color:#ffe0e3}.status-card.cancelled .status-badge{background:#665b4e;color:#fff1df}
        .status-card-body{padding:0 12px 11px 32px;border-top:1px solid var(--border)}.status-card-body time,.status-meta{font-size:11px;color:var(--muted)}.status-card-body p{margin:7px 0;font-size:12px;overflow-wrap:anywhere}.status-artifacts{margin-top:7px;font-size:11px;overflow-wrap:anywhere}.status-artifacts span{display:block;color:var(--muted);margin-bottom:2px}
        """;
}
