namespace SMSR.App.Mvp;
internal static class GraphKnowledgeSyncPanel
{
    internal const string Render = """
        <details id="role-batch-panel" class="index-panel"><summary>여러 파일 설명 생성</summary>
        <p class="muted">최신 설명은 재사용합니다. 새 설명은 Codex 사용량을 소비합니다.</p>
        <label>대상 <select id="batch-folder" aria-label="설명 생성 범위"><option value="">색인된 코드 전체</option></select></label>
        <button type="button" id="batch-preview">대상 확인</button>
        <div id="batch-preview-content"></div>
        <label><input type="checkbox" id="batch-consent">확인한 대상의 Codex 분석에 동의</label>
        <button type="button" id="batch-start" class="action" disabled>설명 생성 시작</button>
        <p id="batch-status" role="status" aria-live="polite"></p><progress id="batch-progress" max="1" value="0" aria-label="설명 생성 진행률"></progress>
        <button type="button" id="batch-pause">일시정지</button><button type="button" id="batch-resume">재개</button><button type="button" id="batch-retry">실패만 재시도</button>
        <button type="button" id="batch-refresh">진행 다시 확인</button></details>
        <details id="vault-panel" class="index-panel"><summary>Obsidian 보관함 연결</summary>
        <p class="muted">분석 노트만 갱신합니다. 개인 노트와 수정한 생성 노트는 덮어쓰지 않습니다.</p>
        <form id="vault-form"><input id="vault-path" aria-label="Obsidian 보관함 경로" placeholder="보관함 폴더 선택 또는 새 전용 경로">
        <button type="button" id="vault-pick">폴더 선택</button>
        <label><input type="checkbox" id="vault-create">없는 폴더 새로 만들기</label>
        <label><input type="checkbox" id="vault-auto">색인·설명 변경 후 자동 동기화</label>
        <label><input type="checkbox" id="vault-consent" required>SMSR 생성 영역 쓰기에 동의</label>
        <button type="submit" class="action">연결 저장</button></form>
        <button type="button" id="vault-sync">지금 동기화</button><button type="button" id="vault-refresh">상태 확인</button>
        <a href="obsidian://choose-vault">Obsidian에서 보관함 등록</a><p id="vault-status" role="status" aria-live="polite"></p><div id="vault-conflicts"></div></details>
        """;
}
