namespace SMSR.App.Mvp;
internal static class GraphStructureStyles
{
    internal const string Css="""
        #overview{margin:12px 0;padding:16px;background:var(--panel);border:1px solid var(--border2);border-radius:14px}
        #overview summary{font-weight:700;cursor:pointer}#overview button{border:1px solid var(--border2);border-radius:8px;padding:8px;background:var(--surface);color:var(--text)}
        #overview button:focus-visible,#visual-map:focus-visible{outline:2px solid #62adff;outline-offset:2px}
        #overview nav,.structure-toolbar{display:flex;flex-wrap:wrap;gap:8px;align-items:center;margin:12px 0}.structure-toolbar input{flex:1;min-width:180px}
        .visual-workspace{display:grid;grid-template-columns:minmax(0,1fr) 280px;border:1px solid var(--border2);border-radius:12px;overflow:hidden}
        #visual-map{height:clamp(420px,65vh,720px);min-width:0;overflow:hidden;background:radial-gradient(var(--border) 1px,transparent 1px);background-size:18px 18px}
        .visual-sidebar{padding:14px;border-left:1px solid var(--border2);max-height:720px;overflow:auto;min-width:0}
        .visual-sidebar input[type=search]{width:100%}.visual-sidebar input[type=checkbox]{flex:none;accent-color:#62adff}
        #visual-info{margin:12px 0;line-height:1.5;overflow-wrap:anywhere}#visual-info h3{margin:6px 0}#visual-info small,#visual-results small{display:block;color:var(--muted)}
        #visual-info button,#visual-info a,#visual-results button{display:block;margin:6px 0;max-width:100%;text-align:left;overflow-wrap:anywhere}
        #visual-info a{padding:8px;background:#155fc8;color:white;border-radius:8px;text-decoration:none}
        #visual-info details{max-height:190px;overflow:auto}#visual-legend{max-height:230px;overflow:auto;margin-top:8px}
        #visual-legend label{display:flex;gap:6px;align-items:center;padding:5px 0;cursor:pointer}
        #visual-legend label span:not(.visual-dot){flex:1;min-width:0;overflow-wrap:anywhere}.visual-dot{width:10px;height:10px;border-radius:50%;flex:none}
        #visual-legend small,.visual-toolbar small,#overview-status,#structure-status,#visual-limits{color:var(--muted)}
        #structure-list,#overview-core{display:flex;flex-wrap:wrap;gap:7px;padding:10px 0}#structure-list button{max-width:360px;text-align:left;overflow-wrap:anywhere}
        #structure-list small{display:block;color:var(--muted);margin-top:4px}#overview details{margin-top:12px}
        #overview.visual-expanded{position:fixed;inset:8px;z-index:1000;margin:0;overflow:auto}#overview.visual-expanded #visual-map{height:calc(100vh -175px)}#overview.visual-expanded .visual-sidebar{max-height:calc(100vh -175px)}
        @media(max-width:760px){.visual-workspace{grid-template-columns:1fr}.visual-sidebar{border-left:0;border-top:1px solid var(--border2);max-height:420px}#visual-map{height:420px}}
        """;
}
