namespace SMSR.App.Mvp;

internal static class GraphCSharpControlPage
{
    internal const string Rendering = """
        function renderCSharpControl(parent,f,describe) {
          const c=f.control,p=document.createElement('p');parent.append(p);
          if(!c){p.textContent='제어 의존: 구버전 결과입니다. 다시 분석하세요.';return}
          if(c.status!=='NORMAL_CFG_CONTROL_DEPENDENCE'){
            const reasons={COMPILATION_ERRORS:'컴파일 오류',NO_CFG:'실행 블록 없음',SUSPENSION:'비동기/iterator',
              EXCEPTION_OR_SPECIAL_REGION:'예외/특수 제어 영역',NON_EXIT_REACHABLE_REGION:'종료에 도달할 수 없는 영역',FINALLY_ROUTING:'finally 경로'};
            p.textContent='제어 의존 분석 불가: '+c.limitations.map(v=>reasons[v]||v).join(', ');return;
          }
          p.textContent=`분기 조건에 따른 제어 의존 ${c.links.length}개. 종료로 이어지는 CFG 경로 기준이며 조건의 실제 성립·암묵적 예외·실행 이력·taint 판정은 아닙니다. 같은 블록의 연결은 다음 반복 회차를 제어할 수 있음을 뜻합니다.`;
          const blocks=new Map(f.blocks.map(b=>[b.ordinal,b]));
          const at=id=>{const b=blocks.get(id);return describe(b.branchValue.find(o=>o.parent===null)?.source||b.operations.find(o=>o.parent===null)?.source)||({Exit:'정상 반환 종료',Entry:'진입'})[b.kind]||'위치 없는 블록'};
          table(parent,['조건 블록 / 근거','선택','영향 블록 / 근거'],c.links.map(l=>[
            `${l.controller} · ${at(l.controller)}`,({TRUE:'참',FALSE:'거짓',ALWAYS:'항상'})[l.outcome]||l.outcome,
            `${l.dependent} · ${at(l.dependent)}`]));
        }
        """;
}
