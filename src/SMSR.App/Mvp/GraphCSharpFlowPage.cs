namespace SMSR.App.Mvp;

internal static class GraphCSharpFlowPage
{
    internal const string Rendering = """
        function renderCSharpFlow(parent,r,describe) {
          const note=document.createElement('p');parent.append(note);
          if(!r.functions){note.textContent='구버전 결과: 실행 흐름을 보려면 다시 분석하세요.';return}
          note.textContent=`컴파일러 흐름 ${r.functions.length}개 함수. 명시적 메서드·접근자와 내부 지역 함수/람다 기준입니다. 최상위 문장·필드 초기화·암묵적 멤버와 런타임 예외 경로 전체는 포함하지 않습니다. 변수 요약은 중첩 함수의 구문 영역을 포함하며 호출 실행·변수 간 의존성·taint 판정이 아닙니다.`;
          const names=new Map(r.symbols.map(s=>[s.id,s]));
          const name=id=>{const s=names.get(id);return s?`${s.name}${s.source?' ('+(s.source.start.line+1)+'줄)':''}`:'익명/외부 심벌'};
          const state=s=>({COMPILER_CFG:'컴파일러 확인',COMPILER_CANDIDATE:'컴파일 오류: 후보',NO_CFG:'실행 블록 없음',COMPILER_REGION_SUMMARY:'컴파일러 영역 요약',UNAVAILABLE:'분석 불가'}[s]||s);
          table(parent,['함수 / 근거','판정','실행 블록','읽는 변수','쓰는 변수'],r.functions.map(f=>[
            `${name(f.symbolId)} · ${describe(f.source)}`,state(f.status),f.blocks.length,
            f.variables.read.map(name).join(', '),f.variables.written.map(name).join(', ')]));
          r.functions.slice(0,100).forEach(f=>{
            const detail=document.createElement('details'),summary=document.createElement('summary');
            summary.textContent=`실행 흐름 상세 · ${name(f.symbolId)} · ${describe(f.source)}`;
            detail.append(summary);parent.append(detail);
            renderCSharpReturns(detail,r,f,name,describe);
            renderCSharpArguments(detail,r,f,name,describe);
            renderCSharpValues(detail,f,name,describe);
            renderCSharpControl(detail,f,describe);
            renderCSharpDefinitions(detail,f,name,describe);
            table(detail,['블록','도달 가능','조건','다음 블록 / 의미 / finally','조건 분기 / 의미 / finally'],f.blocks.map(b=>{
              const edge=e=>e?`${e.constantExcluded?'상수 조건 제외 · ':''}${e.destination??'종료'} / ${({Regular:'계속',Return:'반환',Throw:'예외',Rethrow:'재전파'})[e.semantics]||e.semantics} / ${e.finallyRegions.join(', ')||'없음'}`:'없음';
              return [b.ordinal+' '+({Entry:'진입',Exit:'종료',Block:'블록'}[b.kind]||b.kind),b.reachable?'가능':'불가',
                {None:'없음',WhenFalse:'거짓일 때',WhenTrue:'참일 때'}[b.condition]||b.condition,edge(b.fallThrough),edge(b.conditional)];
            }));
            table(detail,['변수 영역 판정','유입','유출','항상 할당','캡처'],[[state(f.variables.status),
              f.variables.flowsIn.map(name).join(', '),f.variables.flowsOut.map(name).join(', '),
              f.variables.alwaysAssigned.map(name).join(', '),f.variables.captured.map(name).join(', ')]]);
            table(detail,['예외/변수 영역','종류','블록 범위'],f.regions.map(v=>[v.id,v.kind,`${v.firstBlock}–${v.lastBlock}`]));
          });
        }
        """;
}
