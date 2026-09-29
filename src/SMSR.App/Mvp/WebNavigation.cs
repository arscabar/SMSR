namespace SMSR.App.Mvp;

internal static class WebNavigation
{
    public const string Styles = """
        body{padding-left:210px}
        #site-nav{position:fixed;inset:0 auto 0 0;width:210px;z-index:20;box-sizing:border-box;background:#14233b;color:#dce8f7;padding:26px 16px;font:14px system-ui,"Segoe UI",sans-serif;display:flex;flex-direction:column;gap:25px;overflow:auto}
        #site-nav *{box-sizing:border-box}
        #site-nav .brand{font-size:21px;font-weight:800;color:white;padding:0 12px;text-decoration:none}
        #site-nav .project{margin:0 8px;padding:11px 12px;background:#253954;border-radius:11px;font-size:12px;overflow-wrap:anywhere}
        #site-nav .project strong{display:block;color:white;font-size:15px;margin-top:3px}
        #site-nav nav{display:grid;gap:5px}
        #site-nav nav a{display:block;color:inherit;text-decoration:none;padding:12px 14px;border-radius:10px;font-weight:650}
        #site-nav nav a:hover,#site-nav nav a[aria-current=page]{background:#345174;color:white}
        #site-nav nav a[aria-current=page]{box-shadow:inset 3px 0 #8fc1ff}
        #site-nav nav span{display:block;padding:12px 14px;color:#90a3bc}
        @media(max-width:760px){body{padding-left:0}#site-nav{position:relative;width:auto;padding:11px 12px;gap:0}#site-nav .brand,#site-nav .project{display:none}#site-nav nav{display:flex;overflow-x:auto}#site-nav nav a{white-space:nowrap;padding:9px 11px}}
        """;

    public static string Render(string projectId, string? workflowId, string active)
    {
        var project = Uri.EscapeDataString(projectId);
        var workflow = workflowId is null ? "" : "&amp;workflowId=" + Uri.EscapeDataString(workflowId);
        var dashboard = workflowId is null ? $"/graph/explore?projectId={project}" : $"/dashboard?projectId={project}{workflow}";
        string Item(string key, string label, string url) => $"<a href=\"{url}\"{(active == key ? " aria-current=\"page\"" : "")}>{label}</a>";
        return $"<aside id=\"site-nav\" data-project=\"{GraphPage.Encode(projectId)}\" data-workflow=\"{GraphPage.Encode(workflowId)}\"><a class=\"brand\" href=\"{dashboard}\">SMSR</a><div class=\"project\">현재 프로젝트<strong>{GraphPage.Encode(projectId)}</strong></div><nav aria-label=\"웹 화면 범주\">"
            + (workflowId is null ? "<span data-dashboard-disabled aria-disabled=\"true\" title=\"앱에서 작업을 선택하면 열 수 있습니다\">작업 현황</span>" : Item("dashboard", "작업 현황", dashboard))
            + Item("find", "찾기", $"/graph/explore?projectId={project}{workflow}")
            + Item("advanced", "추가 기능", $"/graph/advanced?projectId={project}{workflow}")
            + "</nav></aside><script>" + """
              (()=>{const nav=document.querySelector('#site-nav'),key='smsr-workflow:'+nav.dataset.project;
                try{if(nav.dataset.workflow)sessionStorage.setItem(key,nav.dataset.workflow);
                  else if(sessionStorage.getItem(key)){const item=nav.querySelector('[data-dashboard-disabled]');
                    const link=document.createElement('a');link.textContent='작업 현황';
                    link.href='/dashboard?'+new URLSearchParams({projectId:nav.dataset.project,workflowId:sessionStorage.getItem(key)});
                    item.replaceWith(link)}}catch{}})();
              """ + "</script>";
    }
}
