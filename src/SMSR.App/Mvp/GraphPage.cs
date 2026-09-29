using System.Net;

namespace SMSR.App.Mvp;

internal static class GraphPage
{
    public static string Render(string projectId, string? workflowId, string search,
        GraphHealth? health, GraphSearch? matches, GraphContext? context,
        GraphPath? path, GraphImpact? impact, GraphEvidence? evidence, string? theme,
        GraphCycles? cycles = null, GraphDiff? diff = null, IReadOnlyList<GraphFeedback>? feedback = null,
        GraphRouteMap? routes = null, GraphValidationGaps? gaps = null, GraphCommunities? communities = null,
        GraphCrossRepoMap? crossRepo = null)
    {
        var back = workflowId is null ? "" : $"<a href=\"{Encode(DashboardNavigation.Url(projectId, workflowId))}\">← 작업 그래프</a>";
        var root = health?.Info.RootPath ?? "";
        var summary = health is null ? "아직 색인되지 않았습니다." : $"리비전 {health.Info.Revision} · {health.Info.IndexedAt.ToLocalTime():yyyy-MM-dd HH:mm} · {Encode(root)}";
        var stats = health is null ? "" : $"<div class=\"stats\"><span class=\"stat\">파일 {health.Info.FileCount:N0}</span><span class=\"stat\">노드 {health.Info.NodeCount:N0}</span><span class=\"stat\">관계 {health.Info.EdgeCount:N0}</span><span class=\"stat\">파일 수준/미해소 {health.Info.UnresolvedEdges:N0}</span><span class=\"stat\">고아 {health.DanglingEdges:N0} · 자기 연결 {health.SelfLoops:N0} · 중복 참조 {health.DuplicateReferences:N0}</span></div>";
        var semantics = health is null ? "" : "<p class=\"muted\">FILE_ONLY는 파일만 확인한 인용이며 심벌 구현은 확인하지 않았습니다. 후보 파일은 자동 연결하지 않습니다.</p>";
        var freshness = health is null ? "" : "<button id=\"check-freshness\" type=\"button\">현재 소스 확인</button><p id=\"freshness-message\" role=\"status\" aria-live=\"polite\">현재 상태는 확인 전입니다.</p>";
        return $$"""
            <!doctype html><html lang="ko"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
            <title>SMSR 코드·문서 관계</title><style>{{GraphPageStyles.For(theme)}}{{WebNavigation.Styles}}</style></head><body>{{WebNavigation.Render(projectId, workflowId, "advanced")}}<main>
            <header><div><h1>코드·문서 관계</h1><div class="muted">{{Encode(projectId)}} · {{summary}}</div></div>{{back}}</header>
            <section class="panel"><h2>저장소 색인</h2><p><a href="/graph/explore?projectId={{Uri.EscapeDataString(projectId)}}#index-panel">전체·폴더별 색인 선택</a></p>
            <details><summary>프로젝트 폴더를 자동으로 찾지 못할 때</summary><p class="muted">처음 한 번 Git 최상위 폴더를 연결합니다. 민감 경로·생성 파일과 원문 본문은 저장하지 않습니다.</p>
            <form id="index-form"><input type="hidden" name="projectId" value="{{Encode(projectId)}}"><input name="rootPath" aria-label="Git 저장소 최상위 경로" placeholder="Git 저장소 최상위 경로" value="{{Encode(root)}}" required><button type="submit">폴더 연결·전체 색인</button></form></details>
            <p id="index-message" role="status" aria-live="polite"></p>{{stats}}{{semantics}}{{freshness}}</section>
            <div class="columns"><section class="panel"><h2>파일·문서 찾기</h2>
            <form id="graph-search" action="/graph" method="get"><input type="hidden" name="projectId" value="{{Encode(projectId)}}">{{HiddenWorkflow(workflowId)}}<input name="q" aria-label="파일이나 제목 검색" value="{{Encode(search)}}" placeholder="파일명·경로·제목"><button>검색</button></form><p id="search-message" role="status" aria-live="polite"></p>
            {{GraphPageSections.Search(projectId, workflowId, search, matches)}}</section>
            <section class="panel"><h2>관계와 영향</h2>{{GraphPageSections.Context(projectId, workflowId, search, context, path, impact, feedback)}}</section></div>
            {{GraphPageSections.Evidence(projectId, workflowId, search, evidence)}}
            {{GraphPageSections.Validation(gaps)}}
            <section class="panel"><h2>순환 관계</h2><form action="/graph" method="get"><input type="hidden" name="projectId" value="{{Encode(projectId)}}">{{HiddenWorkflow(workflowId)}}<input type="hidden" name="q" value="{{Encode(search)}}"><input type="hidden" name="cycles" value="true"><button>순환 관계 검사</button></form>{{GraphPageSections.Cycles(projectId, workflowId, search, cycles)}}</section>
            {{GraphPageSections.Diff(projectId, workflowId, search, health?.Info.Revision ?? 0, diff)}}
            {{GraphPageSections.Routes(projectId, workflowId, search, routes)}}
            {{GraphPageSections.Communities(projectId, workflowId, search, health?.Info.NodeCount ?? 0, communities)}}
            {{GraphPageCrossRepo.Render(projectId, workflowId, search, crossRepo)}}
            <section class="panel"><h2>로컬 고급 검색·분석</h2><a href="/graph/advanced?projectId={{Uri.EscapeDataString(projectId)}}{{(workflowId is null ? "" : "&amp;workflowId=" + Uri.EscapeDataString(workflowId))}}">Cypher · 의미 검색 · 심벌·흐름 분석 열기</a></section>
            {{GraphPageSections.Issues(projectId, workflowId, search, health)}}
            </main><script>
            document.getElementById('index-form').addEventListener('submit', async event => {
              event.preventDefault(); const form = event.currentTarget; const message = document.getElementById('index-message');
              message.className = ''; message.textContent = '색인 중…'; window.smsrLoading?.show(message, 'connecting', '색인 중…'); form.querySelector('button').disabled = true;
              try {
                const response = await fetch('/api/graph/index', {method:'POST', headers:{'Content-Type':'application/json'},
                  body:JSON.stringify({projectId:form.elements.projectId.value, rootPath:form.elements.rootPath.value})});
                const result = await response.json();
                if (!response.ok) { message.textContent = result.error || '색인에 실패했습니다.'; message.className = 'error'; }
                else location.reload();
              } catch { message.textContent = '서버 연결에 실패했습니다.'; message.className = 'error'; }
              finally { window.smsrLoading?.hide(message); form.querySelector('button').disabled = false; }
            });
            document.getElementById('check-freshness')?.addEventListener('click', async event => {
              const button = event.currentTarget; const message = document.getElementById('freshness-message');
              message.textContent = '현재 파일 확인 중…'; window.smsrLoading?.show(message, 'searching', '현재 파일 확인 중…'); button.disabled = true;
              try {
                const projectId = document.getElementById('index-form').elements.projectId.value;
                const response = await fetch('/api/graph/freshness?projectId=' + encodeURIComponent(projectId));
                const result = await response.json();
                const paths = result.samplePaths?.slice(0, 5).join(', ') || '';
                message.textContent = response.ok
                  ? result.isStale ? `색인 갱신 필요 · 추가 ${result.addedCount} · 변경 ${result.changedCount} · 삭제 ${result.removedCount}`
                    + (paths ? ` · 예: ${paths}${result.truncated || result.samplePaths.length > 5 ? ' 외' : ''}` : '')
                    : '현재 소스와 색인이 일치합니다.'
                  : result.error || '현재 상태 확인에 실패했습니다.';
              } catch { message.textContent = '서버 연결에 실패했습니다.'; }
              finally { window.smsrLoading?.hide(message); button.disabled = false; }
            });
            document.querySelectorAll('[data-feedback]').forEach(button => button.addEventListener('click', async () => {
              button.disabled = true;
              try {
                const response = await fetch('/api/graph/feedback', {method:'POST', headers:{'Content-Type':'application/json'},
                  body:JSON.stringify({projectId:button.dataset.project, sourceId:button.dataset.source,
                    targetId:button.dataset.target, relation:button.dataset.relation,
                    ownerPath:button.dataset.owner, sourceLine:Number(button.dataset.line), verdict:button.dataset.feedback})});
                if (!response.ok) throw new Error();
                location.reload();
              } catch { button.textContent = '저장 실패'; button.disabled = false; }
            }));
            document.getElementById('graph-search').addEventListener('submit', () => {
              const message = document.getElementById('search-message'); message.textContent = '검색 중…';
              window.smsrLoading?.show(message, 'searching', '검색 중…');
            });
            </script><script type="module" src="/assets/smsr-loading-orb.js"></script></body></html>
            """;
    }

    public static string Url(string projectId, string? workflowId, string search, string? nodeId = null, string? toId = null)
    {
        var url = "/graph?projectId=" + Uri.EscapeDataString(projectId);
        if (workflowId is not null) url += "&workflowId=" + Uri.EscapeDataString(workflowId);
        if (search.Length > 0) url += "&q=" + Uri.EscapeDataString(search);
        if (nodeId is not null) url += "&nodeId=" + Uri.EscapeDataString(nodeId);
        if (toId is not null) url += "&toId=" + Uri.EscapeDataString(toId);
        return Encode(url);
    }

    public static string SourceUrl(string projectId, string path, int line)
        => Encode("/graph/source?projectId=" + Uri.EscapeDataString(projectId)
            + "&path=" + Uri.EscapeDataString(path) + "&line=" + Math.Max(1, line));

    private static string HiddenWorkflow(string? workflowId)
        => workflowId is null ? "" : $"<input type=\"hidden\" name=\"workflowId\" value=\"{Encode(workflowId)}\">";
    public static string Encode(string? value) => WebUtility.HtmlEncode(value ?? "");
}
