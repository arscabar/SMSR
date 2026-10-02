namespace SMSR.App.Mvp;
internal static class GraphOverviewPanel
{
    internal static string Render=>$$"""
        <style>{{GraphStructureStyles.Css}}</style>
        <details id="overview"><summary>전체 관계 지도</summary>
        <nav class="visual-toolbar" aria-label="관계 지도 조작">
        <button id="visual-home" type="button">전체 구조</button><button id="visual-fit" type="button">화면 맞춤</button>
        <button id="visual-in" type="button" aria-label="지도 확대">＋</button><button id="visual-out" type="button" aria-label="지도 축소">−</button>
        <button id="visual-reset" type="button">배치 초기화</button><button id="overview-retry" type="button">다시 확인</button>
        <button id="visual-expand" type="button" aria-pressed="false">크게 보기</button>
        <small>드래그로 이동 · 휠로 확대 · 그룹을 선택해 내부 탐색</small></nav>
        <p id="overview-status" role="status" aria-live="polite"></p>
        <div class="visual-workspace"><div id="visual-map" tabindex="0" role="region" aria-label="실제 관계 지도. 방향키로 이동, 더하기 빼기로 확대 축소"></div>
        <aside class="visual-sidebar"><input id="visual-search" type="search" aria-label="지도에서 찾기" placeholder="그룹·항목 찾기">
        <div id="visual-results"></div><div id="visual-info" aria-live="polite">노드나 관계선을 선택하세요.</div>
        <h3>구조 그룹</h3><label><input id="visual-all" type="checkbox" checked> 모두 표시</label><div id="visual-legend"></div></aside></div>
        <details><summary>주요 구성요소·분석 참고</summary><div id="overview-core"></div><div id="overview-insights"></div></details>
        <details><summary>분석 범위</summary><p id="visual-limits"></p></details>
        <details id="structure-panel"><summary>폴더·파일로 찾아가기</summary>
        <nav id="structure-breadcrumbs" aria-label="구조 탐색 경로"></nav>
        <div class="structure-toolbar"><input id="structure-filter" type="search" aria-label="현재 영역 찾기" placeholder="이 영역에서 폴더·파일·항목 찾기">
        <button id="structure-refresh" type="button">다시 확인</button></div>
        <p id="structure-status" role="status" aria-live="polite"></p>
        <h4 id="structure-list-title">이 영역의 모든 항목</h4><div id="structure-list"></div>
        </details></details>
        """;
}
