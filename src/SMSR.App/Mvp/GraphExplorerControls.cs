namespace SMSR.App.Mvp;

internal static class GraphExplorerControls
{
    internal const string Diagnostics = """
        <details id="diagnostics" class="index-panel"><summary>색인 진단</summary>
        <div class="graph-tools"><span id="issue-count"></span><button type="button" id="issue-all">프로젝트 전체</button>
        <button type="button" id="issue-previous">이전</button><button type="button" id="issue-next">다음</button></div>
        <div id="issue-list" aria-live="polite"></div></details>
        """;
    internal const string Trace = """
        <section class="panel trace-panel"><div id="trace-tools" class="graph-tools" hidden><strong id="trace-start"></strong>
        <label><input type="checkbox" id="trace-inferred"> 추론 포함</label>
        <label>연결 단계 <select id="trace-depth"><option value="3">3</option><option value="5" selected>5</option><option value="8">8</option></select></label>
        <button type="button" id="trace-reset">초기화</button></div><div id="trace-result" aria-live="polite"></div></section>
        """;
    internal const string Render = """
        <div id="graph-tools" class="graph-tools" hidden>
        <select id="relation-direction" aria-label="관계 방향"><option value="both">모든 방향</option><option value="incoming">들어오는 연결</option><option value="outgoing">나가는 연결</option></select>
        <select id="relation-kind" aria-label="관계 종류"><option value="">모든 연결</option><option value="CALLS">호출</option><option value="REFERENCES">참조</option><option value="CONTAINS">포함</option><option value="DEFINES">선언</option><option value="INHERITS">상속</option><option value="IMPLEMENTS">구현</option><option value="IMPORTS">가져오기</option><option value="IMPORTS_FROM">모듈 참조</option><option value="PROJECT_REFERENCE">프로젝트 참조</option><option value="LINKS_TO">문서 링크</option><option value="USES">사용</option><option value="METHOD">메서드</option><option value="DECLARES">경로 선언</option></select>
        <span id="relation-count"></span>
        <button type="button" id="relation-previous">이전</button><span id="relation-page"></span><button type="button" id="relation-next">다음</button>
        <button type="button" id="relation-expand">연결 한 단계 더</button>
        <button type="button" id="relation-reset" disabled>배치 초기화</button>
        </div>
        """;
}
