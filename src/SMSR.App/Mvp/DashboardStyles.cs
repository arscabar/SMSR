namespace SMSR.App.Mvp;

internal static class DashboardStyles
{
    public static string For(string? theme) => DashboardPalette.Resolve(theme) + Css + DashboardGraphStyles.Css;

    private const string Css = """
        *{box-sizing:border-box}
        body{margin:0;min-width:980px;font-family:system-ui,sans-serif;background:var(--bg);color:var(--text)}
        a{color:inherit}header{height:68px;padding:12px 20px;display:flex;align-items:center;justify-content:space-between;border-bottom:1px solid var(--border);background:var(--panel)}
        h1,h2{margin:0}h1{font-size:18px}h2{font-size:15px;margin-bottom:9px}.muted{color:var(--muted);font-size:12px}
        .summary{display:flex;gap:10px}.chip{padding:5px 9px;border:1px solid var(--border2);border-radius:999px;font-size:11px;font-weight:700}
        #alert{padding:10px 20px;background:#5a1e28;color:#ffd8dc;border-bottom:1px solid #9d3444}
        main{display:grid;grid-template-columns:250px minmax(410px,1fr) 320px;height:calc(100vh - 68px)}
        aside,section{min-width:0;overflow:auto}#agents,#details{padding:16px;background:var(--panel)}
        #agents{border-right:1px solid var(--border)}#details{border-left:1px solid var(--border)}#flow{padding:18px;background:var(--bg)}
        .flow-heading{display:flex;justify-content:space-between;margin-bottom:12px}.breadcrumb{display:flex;gap:7px;align-items:center;margin:3px 0 7px;font-size:12px;color:var(--muted)}
        .breadcrumb a{color:#62adff;text-decoration:none}.agent{padding:11px;margin:8px 0;border:1px solid var(--border2);border-radius:8px;background:var(--surface)}
        .agent.active{border-color:#61a8ff;background:var(--active)}.agent.error{border-color:#dd5668}.agent-line{display:flex;align-items:center;justify-content:space-between;gap:8px}
        .agent-name{font-weight:700}.agent-role{margin-top:4px;font-size:12px;font-weight:700;color:#62adff}
        .badge{padding:3px 7px;border-radius:999px;background:var(--pending-stroke);font-size:10px;font-weight:700}.active .badge{background:#124d88;color:#cce7ff}.error .badge{background:#7f2837;color:#ffe0e3}.task{margin-top:7px}
        #graph{min-height:500px;padding:20px;overflow:auto;border:1px solid var(--border);border-radius:10px;background:var(--graph)}
        .detail{margin:0;padding:12px;border:1px solid var(--border2);border-radius:8px;background:var(--surface)}.detail dt{margin-top:11px;color:var(--muted);font-size:12px}.detail dt:first-child{margin-top:0}
        .detail dd{margin:3px 0 0;white-space:pre-wrap;overflow-wrap:anywhere}.history-title{margin-top:20px}.history{padding-left:19px;color:var(--muted);font-size:12px}.history li{margin:7px 0}.empty{color:var(--muted);font-size:13px}
        .history-tools{display:flex;justify-content:flex-end;margin:-4px 0 8px}.history-tools button{padding:5px 9px;border:1px solid var(--border2);border-radius:7px;background:var(--surface);color:var(--text);cursor:pointer}
        .status-cards{display:grid;gap:8px}.status-card{border:1px solid var(--border2);border-left:3px solid var(--pending-stroke);border-radius:8px;background:var(--surface);overflow:hidden}
        .status-card.active{border-left-color:#61a8ff}.status-card.success{border-left-color:#48b97b}.status-card.error{border-left-color:#dd5668}
        .status-card summary{display:flex;align-items:center;gap:8px;padding:9px 10px;cursor:pointer;list-style:none}.status-card summary::-webkit-details-marker{display:none}
        .status-card summary:before{content:'›';font-size:18px;color:var(--muted);transition:transform .15s}.status-card[open] summary:before{transform:rotate(90deg)}
        .status-card-title{min-width:0;flex:1;font-size:12px;font-weight:700;overflow:hidden;text-overflow:ellipsis;white-space:nowrap}.status-badge{padding:3px 7px;border-radius:999px;background:var(--pending-stroke);font-size:10px;font-weight:700}
        .status-card.active .status-badge{background:#124d88;color:#cce7ff}.status-card.success .status-badge{background:#16613d;color:#d4ffe6}.status-card.error .status-badge{background:#7f2837;color:#ffe0e3}
        .status-card-body{padding:0 12px 11px 32px;border-top:1px solid var(--border)}.status-card-body time,.status-meta{font-size:11px;color:var(--muted)}.status-card-body p{margin:7px 0;font-size:12px;overflow-wrap:anywhere}.status-artifacts{margin-top:7px;font-size:11px;overflow-wrap:anywhere}.status-artifacts span{display:block;color:var(--muted);margin-bottom:2px}
        """;
}
