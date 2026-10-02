namespace SMSR.App.Mvp;
internal static class GraphKnowledgePanel
{
    internal const string Render="""
        <details class="index-panel"><summary>질문으로 찾기 · 근거 모으기</summary>
        <form id="question-form"><input id="question" type="search" maxlength="2000" placeholder="예: 저장 기능은 어디서 사용하나요?" aria-label="그래프 질문"><button class="action" type="submit">근거 찾기</button></form>
        <p id="question-status" role="status" aria-live="polite"></p><div id="question-hits"></div></details>
        <details class="index-panel"><summary>변경 감시 · 보고서 · 내보내기</summary>
        <button id="watch-toggle" type="button">변경 감시 상태 확인</button><span id="watch-state" role="status"></span>
        <button id="report-load" type="button">보고서 확인</button><div id="report-result"></div>
        <nav id="knowledge-export" aria-label="지식 그래프 내보내기"></nav>
        <small>내보내기는 저장된 전체 그래프입니다. 원문 코드·원문 질문은 포함하지 않습니다.</small></details>
        """;
}
