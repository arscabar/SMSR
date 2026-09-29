namespace SMSR.App.Mvp;

internal static class GraphCSharpDefinitionsPage
{
    internal const string Rendering = """
        function renderCSharpDefinitions(parent,f,name,describe) {
          const d=f.definitions,p=document.createElement('p');parent.append(p);
          if(!d){p.textContent='할당·읽기 의존: 구버전 결과입니다. 다시 분석하세요.';return}
          const reasons={COMPILATION_ERRORS:'컴파일 오류',NO_CFG:'실행 블록 없음',
            REFERENCE_OR_SUSPENSION:'참조·비동기·iterator 문맥',CAPTURED_STORAGE:'캡처 변수',
            EXCEPTION_OR_SPECIAL_REGION:'예외/특수 제어 영역',NON_SCALAR_STORAGE:'비기본형 저장소',
            INDIRECT_WRITE:'간접 쓰기',ALIAS_OR_DECONSTRUCTION:'별칭/분해 할당',REFERENCE_ARGUMENT:'참조 인자'};
          if(d.status!=='MAY_REACHING_DEFINITIONS'){
            p.textContent='할당·읽기 의존 분석 불가: '+d.limitations.map(v=>reasons[v]||v).join(', ');return;
          }
          p.textContent=`읽기에 도달할 수 있는 할당 ${d.links.length}개. 함수 내부 기본형·문자열 변수의 정상 CFG 경로 기준입니다. 분기 조건·힙·함수 간 값 전파·taint는 검증하지 않습니다. 결과가 없다고 안전함을 뜻하지 않습니다.`;
          const sites=new Map(d.sites.map(s=>[s.id,s]));
          table(parent,['변수','할당 후보 근거','읽기 근거'],d.links.map(l=>{
            const a=sites.get(l.definition),b=sites.get(l.read);
            return [name(b.symbolId),(a.kind==='ENTRY'?'호출 입력 · ':'')+describe(a.source),describe(b.source)];
          }));
        }
        """;
}
