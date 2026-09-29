namespace SMSR.App.Mvp;

internal static class GraphJdtPage
{
    internal const string Section="""
        <section class="panel"><h2>Java 언어 서버 정의 조회</h2>
        <p>설치된 VS Code Red Hat Java 1.56.0 win32-x64를 실행합니다. 선택한 파일만 임시 프로젝트로 복사하며 원본 설정·빌드·의존성 가져오기는 사용하지 않습니다. 기본 JRE 기준의 정의 후보이며 실제 호출 대상·전체 PDG/taint를 보장하지 않습니다. 설치 폴더는 사용자가 신뢰해야 하며 자동 설치하지 않습니다.</p>
        <label for="jdt-input">Java 상대 경로 (한 줄에 하나, 최대500개)</label><textarea id="jdt-input" placeholder="src/sample/Lib.java&#10;src/sample/Entry.java"></textarea>
        <label for="jdt-roots">소스 루트 (한 줄에 하나, 빈 값은 저장소 루트)</label><textarea id="jdt-roots" placeholder="src"></textarea>
        <label for="jdt-path">조회할 상대 파일</label><input id="jdt-path" placeholder="src/sample/Entry.java">
        <label for="jdt-line">줄 번호 (1부터)</label><input id="jdt-line" type="number" min="1" value="1">
        <label for="jdt-character">UTF-16 열 번호 (1부터, emoji는2칸)</label><input id="jdt-character" type="number" min="1" value="1">
        <button data-action="jdt">정의 조회·저장</button><button data-action="jdt-result">정의 저장 결과</button>
        <div id="jdt-result" role="status"></div></section>
        """;
    internal const string Rendering="""
        function renderJdt(parent,title,data){
          const report=data.report||data;
          title.textContent=`리비전 ${report.revision} · ${report.profile} · 정의 후보 ${report.candidates.length}개 · 범위 밖 제외 ${report.excluded}개 · ${data.stale?'갱신 필요':'입력 묶음 기준'} · ${report.analyzedAt}`;
          const list=document.createElement('ol');parent.append(list);
          report.candidates.forEach(c=>{const item=document.createElement('li'),link=document.createElement('a');
            link.href='/graph/source?'+new URLSearchParams({projectId:document.querySelector('main').dataset.project,path:c.path,line:String(c.range.start.line+1)});
            link.textContent=`${c.path}:${c.range.start.line+1}:${c.range.start.character+1}–${c.range.end.line+1}:${c.range.end.character+1} (끝 제외)`;
            item.append(link);list.append(item)});
          if(!report.candidates.length) parent.append(document.createTextNode('입력 범위 안 정의가 없습니다. 미해소·외부 정의·잘못된 소스 루트를 확인하세요.'));
        }
        """;
}
