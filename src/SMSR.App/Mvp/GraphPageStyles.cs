namespace SMSR.App.Mvp;

internal static class GraphPageStyles
{
    public static string For(string? theme) => DashboardPalette.Resolve(theme) + """
        *{box-sizing:border-box}body{margin:0;background:var(--bg);color:var(--text);font-family:system-ui,sans-serif;line-height:1.5}
        main{max-width:1200px;margin:auto;padding:24px}h1{font-size:22px;margin:0 0 6px}h2{font-size:16px;margin:0 0 12px}h3{font-size:14px;margin:0 0 8px}
        a{color:#75b8ff}header{display:flex;justify-content:space-between;gap:18px;align-items:center;margin-bottom:22px}
        .muted{color:var(--muted)}.panel{padding:17px;margin:14px 0;border:1px solid var(--border2);border-radius:12px;background:var(--panel)}
        .stats{display:flex;flex-wrap:wrap;gap:8px}.stat{padding:8px 11px;border-radius:8px;background:var(--surface);font-size:12px}
        .columns{display:grid;grid-template-columns:minmax(270px,1fr) minmax(360px,1.5fr);gap:14px}
        form{display:flex;flex-wrap:wrap;gap:8px}input{min-width:0;flex:1;padding:8px;border:1px solid var(--border2);border-radius:7px;background:var(--surface);color:var(--text)}
        button{padding:8px 12px;border:1px solid #438ddd;border-radius:7px;background:var(--active);color:var(--text);cursor:pointer}
        ul,ol{padding-left:20px;margin:8px 0}li{margin:8px 0;overflow-wrap:anywhere}.item{display:block;padding:8px;border:1px solid var(--border);border-radius:7px;text-decoration:none}
        .item:hover,.item:focus-visible{border-color:#75b8ff}.kind{font-size:11px;color:var(--muted)}.badge{display:inline-block;padding:2px 6px;border-radius:5px;background:var(--surface);font-size:10px}
        code{overflow-wrap:anywhere}.error{color:#ff9ca7}#index-message{font-size:12px}@media(max-width:760px){main{padding:14px}.columns{display:block}header{display:block}}
        """;
}
