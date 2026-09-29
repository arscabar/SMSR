namespace SMSR.App.Mvp;

internal static class GraphJavaPage
{
    internal const string Section="""
        <section class="panel"><h2>Java 17 명시적 파일 묶음 분석</h2>
        <p>설치된 JDK와 SMSR Java 분석기가 필요합니다. 입력 파일과 Java 17 표준 라이브러리만 해석하며 대상 코드·processor·빌드를 실행하지 않습니다. switch 문·식의 콜론형 이어 실행·화살표형 종료, 중첩 yield의 결과·지역 변수 합류를 지원합니다. CASE_MATCH/CASE_NO_MATCH는 상수 선택의 정규화 근거이지 실제 값 판정이 아닙니다. null/언박싱 예외·try/finally·패턴·패키지/생성 코드·전체 PDG/taint는 미지원입니다.</p>
        <label for="java-input">색인된 Java 상대 경로 (한 줄에 하나, 최대 500개)</label><textarea id="java-input" placeholder="src/Lib.java&#10;src/Entry.java"></textarea>
        <button data-action="java">Java 분석·저장</button><button data-action="java-result">Java 저장 결과</button>
        <div id="java-result" role="status"></div></section>
        """;
    internal const string Rendering="""
        function renderJava(parent,title,data) {
          const report=data.report||data,r=report.result,at=s=>s?`${s.path}:${s.start.line+1}:${s.start.character+1}`:'';
          title.textContent=`리비전 ${report.revision} · Java ${r.languageVersion} · ${r.status} · JDK ${r.runtime} · ${data.stale?'갱신 필요':'입력 묶음 기준'} · 진단 ${r.diagnosticCount}개${r.diagnosticsTruncated?' (일부 표시)':''}`;
          const state=s=>({BOUND_INPUT_BUNDLE:'입력 묶음에서 연결',STATIC_TARGET_ONLY:'정적 대상만 확인',COMPILER_CANDIDATE:'컴파일 오류: 후보',UNRESOLVED:'미해소'})[s]||s;
          renderJavaSummaries(parent,r,at);
          renderJavaControl(parent,r,at);
          renderJavaValues(parent,r,at);
          table(parent,['호출 근거','컴파일러 대상','판정','방식 / 문맥','식 타입'],r.calls.map(c=>[at(c.source),c.signature,state(c.resolution),`${c.dispatch} / ${c.scope}`,c.expressionType]));
          r.calls.slice(0,100).filter(c=>c.arguments.length).forEach(c=>{
            const detail=document.createElement('details'),summary=document.createElement('summary');
            summary.textContent=`Java 인자 대응 · ${c.signature||'미해소'} · ${at(c.source)}`;detail.append(summary);parent.append(detail);
            table(detail,['인자 근거','타입','매개변수 순번(0부터)'],c.arguments.map(a=>[at(a.source),a.type,a.parameterOrdinal]));
          });
          table(parent,['선언','종류','타입','근거'],r.symbols.map(s=>[s.name,s.kind,s.type,at(s.source)]));
          table(parent,['진단 코드','수준','근거'],r.diagnostics.map(d=>[d.code,d.severity,at(d.source)]));
        }
        """;
}
