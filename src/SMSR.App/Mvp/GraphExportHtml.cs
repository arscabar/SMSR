using System.IO;
using System.Net;
using System.Text.Json;
namespace SMSR.App.Mvp;
internal static class GraphExportHtml
{
    internal static string Render(GraphKnowledgeExport data)
    {
        var script=File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"WebAssets","graph-export.js"));
        var json=JsonSerializer.Serialize(data,GraphWorker.Json); // Default encoder escapes HTML/script delimiters.
        return "<!doctype html><html lang=\"ko\"><meta charset=\"utf-8\"><meta http-equiv=\"Content-Security-Policy\" content=\"default-src 'none'; script-src 'unsafe-inline'; style-src 'unsafe-inline'; connect-src 'none'; base-uri 'none'\"><title>SMSR 그래프</title><style>body{font:16px system-ui;background:#101722;color:#e7eef8;margin:24px}button,input{font:inherit;margin:6px;padding:8px}button{cursor:pointer}svg{width:100%;height:420px;background:#182333}pre{white-space:pre-wrap}a{color:#8bf}#nodes button{max-width:100%;overflow-wrap:anywhere}</style><h1>"+WebUtility.HtmlEncode(data.ProjectId)+" 지식 그래프</h1><p>저장 리비전 "+data.Revision+" · 원문 현재 상태 미확인 · "+(data.Truncated?"일부 범위":"전체 범위")+"</p><input id=\"query\" aria-label=\"이름·경로 검색\" placeholder=\"이름·경로 검색\"><button id=\"previous\">이전</button><button id=\"next\">다음</button><output id=\"position\"></output><div id=\"nodes\"></div><svg id=\"map\" role=\"img\" aria-label=\"선택 노드와 직접 연결\"></svg><p id=\"limit\"></p><pre id=\"detail\"></pre><script type=\"application/json\" id=\"data\">"+json+"</script><script>"+script+"</script></html>";
    }
}
