namespace SMSR.App.Mvp;

internal static class GraphAdvancedPage
{
    public static string Render(string project, string? theme, string? workflowId = null) => $$"""
        <!doctype html><html lang="ko"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
        <title>SMSR 추가 기능</title><style>{{GraphPageStyles.For(theme)}}{{WebNavigation.Styles}}{{GraphAdvancedLayout.Css}}textarea{min-height:100px;background:var(--surface);color:var(--text);padding:10px}pre{white-space:pre-wrap;overflow-wrap:anywhere}div[role=status]{max-height:480px;overflow:auto}table{width:100%;border-collapse:collapse;font-size:13px}th,td{text-align:left;padding:7px;border-bottom:1px solid var(--border);overflow-wrap:anywhere}</style>
        {{WebNavigation.Render(project, workflowId, "advanced")}}<main data-project="{{GraphPage.Encode(project)}}"><h1>추가 기능</h1>
        <a href="/graph/explore?projectId={{Uri.EscapeDataString(project)}}{{(workflowId is null ? "" : "&amp;workflowId=" + Uri.EscapeDataString(workflowId))}}">← 찾기</a>
        <p class="advanced-intro">필요한 항목만 열어보세요. 질문·쿼리 원문은 저장하지 않으며, 모델과 분석은 이 컴퓨터에서 실행됩니다.</p>
        <section class="panel"><h2>Cypher 조회</h2><p>Kuzu 0.11.3 읽기 전용. Node(id, label, kind, path, line), REL(relation, confidence). 최대 5만 노드·20만 관계, 결과 200행. 쓰기·절차·파일 함수는 차단됩니다.</p>
        <label for="cypher-input">Cypher</label><textarea id="cypher-input">MATCH (n:Node) RETURN n.kind, count(n) ORDER BY count(n) DESC</textarea>
        <button data-action="cypher">조회</button><div id="cypher-result" role="status"></div></section>
        <section class="panel"><h2>다국어 의미 검색</h2><p>파일 경로·제목·종류의 384차원 로컬 벡터를 검색합니다. 본문 전체 검색은 아닙니다. 첫 검색은 모델 색인으로 시간이 걸릴 수 있습니다.</p>
        <label for="semantic-input">검색어</label><input id="semantic-input" placeholder="예: 작업 완료 상태 검증"><button data-action="semantic">의미 검색</button><div id="semantic-result" role="status"></div></section>
        <section class="panel"><h2>본문 의미 검색</h2><p>현재 색인과 해시가 같은 원문 전체를 나누어 검색합니다. 원문·질문은 저장하지 않고 벡터·해시만 저장합니다. 비밀 패턴이 있는 파일은 통째로 제외합니다. 모든 비밀을 자동 판별하지는 못하므로 민감 파일 제외 정책을 유지하세요. 최대 1만 조각; 첫 검색은 시간이 걸립니다.</p>
        <label for="body-semantic-input">본문 검색어</label><input id="body-semantic-input" placeholder="예: 사용자 인증 오류를 처리하는 방법">
        <label for="body-scope">본문 검색 범위</label><input id="body-scope" placeholder="상대 파일/폴더, 빈 값은 전체">
        <button data-action="body-semantic">본문 검색</button><div id="body-semantic-result" role="status"></div></section>
        <section class="panel"><h2>심벌·문장 의존성·위험 경로</h2><p>색인된 파일의 현재 해시를 확인합니다. 선언은 구문 근거, 호출은 미해소이며 함수 내부의 보수적 MAY_DEPEND 관계입니다. 전 언어 의미 정확도·전체 프로그램 PDG를 보장하지 않습니다. 결과가 없다고 안전한 코드는 아닙니다.</p>
        <label for="analyze-input">저장소 상대 파일 경로</label><input id="analyze-input" placeholder="src/example.py">
        <button data-action="analyze">분석·저장</button><button data-action="analysis">저장 결과 확인</button><div id="analyze-result" role="status"></div></section>
        {{GraphCSharpPage.Section}}
        {{GraphJavaPage.Section}}
        {{GraphJdtPage.Section}}
        {{GraphTypeScriptPage.Section}}
        <p class="advanced-note">Python 도구가 없으면 scripts/install-graph-runtime.ps1을 실행하세요. Git 전역 훅은 변경하지 않습니다.</p></main>
        <script>
        {{GraphAdvancedRendering.Script}}
        {{GraphPythonPage.Rendering}}
        {{GraphPythonDefinitionsPage.Rendering}}
        {{GraphPythonSummaryPage.Rendering}}
        {{GraphPythonLocalCallPage.Rendering}}
        {{GraphPythonValuesPage.Rendering}}
        {{GraphPythonControlPage.Rendering}}
        {{GraphCSharpPage.Rendering}}
        {{GraphCSharpFlowPage.Rendering}}
        {{GraphCSharpConnectionsPage.Rendering}}
        {{GraphCSharpDefinitionsPage.Rendering}}
        {{GraphCSharpControlPage.Rendering}}
        {{GraphCSharpValuesPage.Rendering}}
        {{GraphCSharpReturnsPage.Rendering}}
        {{GraphCSharpArgumentsPage.Rendering}}
        {{GraphJavaPage.Rendering}}
        {{GraphJdtPage.Rendering}}
        {{GraphJavaControlPage.Rendering}}
        {{GraphJavaValuesPage.Rendering}}
        {{GraphJavaSummaryPage.Rendering}}
        {{GraphTypeScriptPage.Rendering}}
        {{GraphTypeScriptConnectionsPage.Rendering}}
        {{GraphTypeScriptValuesPage.Rendering}}
        {{GraphTypeScriptSummaryPage.Rendering}}
        {{GraphAdvancedLayout.Script}}
        document.querySelectorAll('[data-action]').forEach(button=>button.onclick=async()=>{
          const action=button.dataset.action, id=action==='analysis'?'analyze':action.endsWith('-result')?action.slice(0,-7):action;
          const result=document.getElementById(id+'-result');result.textContent='처리 중…';
          window.smsrLoading?.show(result, ['cypher','semantic','body-semantic','analysis'].includes(action)?'searching':'solving', '처리 중…');button.disabled=true;
          const input=document.getElementById(id+'-input').value;
          const payload={projectId:document.querySelector('main').dataset.project,input,scope:action==='body-semantic'?document.getElementById('body-scope').value:''};
          if(id==='java'||id==='typescript'){delete payload.input;delete payload.scope;payload.paths=input.split(/\r?\n/).map(p=>p.trim()).filter(Boolean)}
          if(id==='jdt'){delete payload.input;delete payload.scope;payload.paths=input.split(/\r?\n/).map(p=>p.trim()).filter(Boolean);payload.path=document.getElementById('jdt-path').value.trim();payload.line=Number(document.getElementById('jdt-line').value)-1;payload.character=Number(document.getElementById('jdt-character').value)-1;payload.sourceRoots=document.getElementById('jdt-roots').value.trim()?document.getElementById('jdt-roots').value.split(/\r?\n/).map(p=>p.trim()).filter(Boolean):['']}
          if(id==='csharp'){delete payload.input;delete payload.scope;payload.paths=input.split(/\r?\n/).map(p=>p.trim()).filter(Boolean);payload.languageVersion=document.getElementById('csharp-version').value.trim();payload.defines=document.getElementById('csharp-defines').value.split(',').map(p=>p.trim()).filter(Boolean)}
          try{const response=await fetch('/api/graph/'+action,{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify(payload)});
            const value=await response.json();renderResult(result,action,value);
          }catch{result.textContent='서버 연결에 실패했습니다.'}finally{window.smsrLoading?.hide(result);button.disabled=false}
        });
        </script><script type="module" src="/assets/smsr-loading-orb.js"></script></html>
        """;
}
