namespace SMSR.App.Mvp;

internal static class GraphExplorerPage
{
    public static string Render(string projectId, string? workflowId, string? view, string? theme)
    {
        const string title = "무엇을 찾고 있나요?";
        var workflow = workflowId is null ? "" : "&amp;workflowId=" + Uri.EscapeDataString(workflowId);
        return $$"""
            <!doctype html><html lang="ko"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
            <title>SMSR · {{title}}</title><style>{{DashboardPalette.Resolve(theme)}}{{WebNavigation.Styles}}{{Styles}}{{GraphFindStyles.Css}}</style></head>
            <body>{{WebNavigation.Render(projectId, workflowId, "find")}}<main id="explorer" data-project="{{GraphPage.Encode(projectId)}}" data-workflow="{{GraphPage.Encode(workflowId)}}">
            <header><span class="eyebrow">{{GraphPage.Encode(projectId)}}</span><h1>{{title}}</h1></header>
            <details id="index-panel" class="index-panel"><summary id="index-title">색인 범위 확인</summary>
            <p id="index-location" class="muted"></p><form id="index-form">
            <label><input type="radio" name="scope" value="all" checked> 프로젝트 전체</label>
            <label><input type="radio" name="scope" value="folders"> 선택한 폴더만</label>
            <div id="index-folders" class="folder-list" hidden></div><small id="scope-note" hidden>선택한 폴더가 기존 색인 범위를 대체합니다.</small>
            <button type="submit" class="action">색인 시작</button><span id="index-status" role="status" aria-live="polite"></span>
            </form></details>
            <form id="search-form" role="search"><input id="query" type="search" aria-label="파일·문서 검색" placeholder="파일명, 경로, 클래스·함수 이름" autocomplete="off"><button type="submit">찾기</button></form>
            <p id="status" role="status" aria-live="polite"></p>
            <div class="legend" aria-label="검색 결과 종류 필터"><button type="button" class="kind-code" data-kind="code" aria-pressed="false">코드</button><button type="button" class="kind-document" data-kind="document" aria-pressed="false">문서</button><button type="button" class="kind-image" data-kind="image" aria-pressed="false">이미지</button><button type="button" class="kind-video" data-kind="video" aria-pressed="false">영상</button><button type="button" class="kind-audio" data-kind="audio" aria-pressed="false">음원</button></div>
            <div class="workspace"><section class="panel"><h2>검색 결과 <small id="count"></small></h2><div id="results" class="results"></div><nav id="search-pages" class="search-pages" aria-label="검색 결과 페이지" hidden><button type="button" id="previous-page">이전</button><span id="page-label"></span><button type="button" id="next-page">다음</button></nav></section>
            <section class="panel detail-panel"><h2>선택 항목</h2><div id="details" class="details">왼쪽에서 파일이나 문서를 선택하세요.</div>
            <details id="relation-panel"><summary id="graph-title">연결 지도 보기</summary>{{GraphExplorerControls.Render}}<div id="graph" class="graph" role="region" aria-label="선택 항목의 관계 그래프"></div><div id="edge-details"></div></details></section></div>
            <details id="role-coverage" class="index-panel"><summary>설명 현황 확인</summary><div id="role-coverage-content"></div></details>
            {{GraphKnowledgeSyncPanel.Render}}
            <details id="extra-tools" class="extra-tools"><summary>추가 분석 도구</summary><div>
            {{GraphKnowledgePanel.Render}}
            {{GraphOverviewPanel.Render}}
            {{GraphExplorerControls.Trace}}
            {{GraphExplorerControls.Diagnostics}}
            <p class="links"><a href="/graph?projectId={{Uri.EscapeDataString(projectId)}}{{workflow}}">색인 갱신·상세 관계 도구</a> · <a href="/graph/advanced?projectId={{Uri.EscapeDataString(projectId)}}{{workflow}}">고급 분석 도구</a></p>
            </div></details>
            </main><script src="/assets/vis-network-9.1.6.min.js" integrity="sha384-Ux6phic9PEHJ38YtrijhkzyJ8yQlH8i/+buBR8s3mAZOJrP1gwyvAcIYl3GWtpX1"></script><script type="module" src="/assets/smsr-loading-orb.js"></script><script type="module" src="/assets/graph-explorer.js"></script></body></html>
            """;
    }

    private const string Styles = """
        *{box-sizing:border-box}body{margin:0;background:var(--bg);color:var(--text);font:14px system-ui,"Segoe UI",sans-serif}main{max-width:1700px;margin:auto;padding:26px clamp(16px,3vw,44px)}
        header{margin-bottom:20px}header h1{font-size:30px;letter-spacing:-.04em;margin:4px 0}header p,.muted{color:var(--muted)}header p{margin:0}.eyebrow{color:var(--muted);font-size:12px;font-weight:700}
        #search-form{display:flex;gap:9px;background:var(--panel);padding:16px;border:1px solid var(--border2);border-radius:14px}input{min-width:0;flex:1;border:1px solid var(--border2);border-radius:9px;padding:12px;background:var(--surface);color:var(--text);font:inherit}
        .index-panel{margin:0 0 12px;padding:13px 16px;background:var(--panel);border:1px solid var(--border2);border-radius:14px}.index-panel summary{cursor:pointer;font-weight:700}.index-panel p{margin:10px 0;overflow-wrap:anywhere}.index-panel form{display:flex;flex-wrap:wrap;align-items:center;gap:10px}.index-panel label{display:inline-flex;align-items:center;gap:5px}.index-panel input{flex:none}.folder-list{width:100%;max-height:170px;overflow:auto;display:flex;flex-wrap:wrap;gap:8px}.folder-list[hidden]{display:none}.folder-list label{padding:6px 9px;border:1px solid var(--border2);border-radius:8px}.index-panel .action{padding:8px 12px}.index-panel #index-status{color:var(--muted)}
        button{cursor:pointer;font:inherit}#search-form button,.action{border:0;border-radius:9px;background:#155fc8;color:white;padding:10px 18px;font-weight:700}#status{min-height:24px;color:var(--muted)}
        .legend{display:flex;flex-wrap:wrap;gap:7px;margin:-4px 0 12px;font-size:11px}.legend button{display:inline-flex;align-items:center;gap:6px;padding:5px 9px;color:var(--text);border:1px solid var(--border2);border-radius:999px;background:var(--panel);font-size:inherit}.legend button:before{content:'';width:9px;height:9px;border-radius:3px;background:var(--kind)}.legend button[aria-pressed=true]{border-color:var(--kind);background:color-mix(in srgb,var(--panel) 75%,var(--kind))}.legend button:focus-visible{outline:2px solid var(--kind);outline-offset:2px}.kind-code{--kind:#38bda8}.kind-document{--kind:#ab8bd7}.kind-image{--kind:#e8a84d}.kind-video{--kind:#ec7e91}.kind-audio{--kind:#d3bb55}.kind-other{--kind:#9aaabe}
        .workspace{display:grid;grid-template-columns:minmax(220px,270px) minmax(350px,1fr) minmax(230px,280px);gap:12px;min-height:520px}.panel{min-width:0;overflow:hidden;background:var(--panel);border:1px solid var(--border2);border-radius:14px}.panel h2{font-size:15px;margin:0;padding:17px;border-bottom:1px solid var(--border)}.panel small{color:var(--muted)}
        .results{max-height:550px;overflow:auto;padding:8px}.result{display:block;width:100%;text-align:left;border:1px solid transparent;border-radius:9px;background:none;color:var(--text);padding:10px;margin:2px 0;overflow-wrap:anywhere}.result:hover,.result[aria-selected=true]{border-color:#6ca9ec;background:var(--active)}.result strong,.result small{display:block}.result small{margin-top:3px;color:var(--muted)}
        .search-pages{display:flex;align-items:center;justify-content:space-between;gap:8px;padding:10px;border-top:1px solid var(--border);font-size:12px}.search-pages[hidden]{display:none}.search-pages button{padding:6px 9px;border:1px solid var(--border2);border-radius:8px;background:var(--surface);color:var(--text)}.search-pages button:disabled{opacity:.45;cursor:default}
        .result[class*=kind-]{border-left:3px solid var(--kind)}.graph .graph-node rect{stroke:var(--kind);fill:color-mix(in srgb,var(--surface) 84%,var(--kind))}
        .graph-tools{display:flex;align-items:center;justify-content:space-between;gap:8px;padding:8px 12px;border-bottom:1px solid var(--border);color:var(--muted);font-size:12px}.graph-tools[hidden]{display:none}.graph-tools button{border:1px solid var(--border2);border-radius:7px;background:var(--surface);color:var(--text);padding:5px 9px}.graph{height:430px;overflow:auto;background:radial-gradient(var(--border) 1px,transparent 1px);background-size:18px 18px}.graph svg{display:block;width:100%;height:100%;min-width:620px}.graph line{stroke:#66a7ed;stroke-width:3;stroke-dasharray:10 7;animation:edge-travel 1.1s linear infinite}.graph rect{fill:var(--surface);stroke:#6ca9ec;stroke-width:2;transition:fill .18s,stroke .18s}.graph text{fill:var(--text);font:12px system-ui}.graph .edge-label{fill:var(--muted);font-size:10px}.graph-node{animation:node-enter .32s ease-out both}.graph-node.selected rect{animation:node-glow 2s ease-in-out infinite}.graph-node:hover rect,.graph-node:focus-visible rect{fill:var(--active);stroke:#a9d2ff}.details{padding:14px;line-height:1.5}.details h3{margin:0 0 10px;overflow-wrap:anywhere}.details p{overflow-wrap:anywhere}.details a,.links a{color:#62adff}.links{font-size:12px;margin:16px 0}.empty{padding:14px;color:var(--muted)}.media-preview{display:block;width:100%;max-height:360px;margin-top:12px;object-fit:contain;border-radius:9px;background:#090f18}
        .details .source-action{display:inline-flex;margin-top:6px;padding:10px 14px;border-radius:9px;background:#155fc8;color:white;font-weight:700;text-decoration:none}.details .source-action:hover,.details .source-action:focus-visible{background:#2375e4;outline:2px solid #a9d2ff}
        .graph-tools{flex-wrap:wrap}.graph-tools select{background:var(--surface);color:var(--text);padding:6px;border:1px solid var(--border2);border-radius:7px}.trace-panel{margin-top:12px}.trace-panel:has(#trace-tools[hidden]):has(#trace-result:empty){display:none}#trace-result{padding:16px}.trace-step{padding:12px;margin:8px 0;border-left:3px solid #66a7ed;background:var(--surface);border-radius:8px}.trace-step small{display:block;margin-top:7px}.trace-actions{display:flex;flex-wrap:wrap;gap:6px;margin-top:12px}.trace-step button,.trace-actions button{background:var(--surface);color:var(--text);border:1px solid var(--border2);padding:7px;border-radius:7px}.trace-actions button:disabled{opacity:.4}.trace-step a{display:inline-block;color:#62adff;margin-top:6px}
        @keyframes node-enter{from{opacity:0;transform:translateY(8px)}to{opacity:1;transform:translateY(0)}}@keyframes edge-travel{to{stroke-dashoffset:-17}}@keyframes node-glow{50%{filter:drop-shadow(0 0 12px #559beacc)}}
        @media(max-width:1400px){.workspace{grid-template-columns:230px minmax(350px,1fr)}.workspace>.panel:last-child{grid-column:1/-1}}
        @media(max-width:760px){main{padding:18px 12px}.workspace{display:block}.panel{margin-bottom:10px}.results{max-height:240px}.graph{height:350px}}
        @media(prefers-reduced-motion:reduce){*,*::before,*::after{animation-duration:.01ms!important;transition-duration:.01ms!important}}
        """;
}
