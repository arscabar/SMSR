namespace SMSR.App.Mvp;

internal static class GraphPythonControlPage
{
    public const string Rendering = """
        function renderPythonControl(parent,f) {
          const c=f.control;if(!c)return;
          const heading=document.createElement('h4');heading.textContent='조건 → 명령 제어 의존';parent.append(heading);
          const note=document.createElement('p');note.textContent=`${c.status} · ${c.reason||'정상 전이의 종료 경로 기준'} · -1은 합성 종료. 암묵적 예외·조건 만족성·실제 종료를 보장하지 않으며 taint/안전성 판정이 아닙니다. 컴파일러가 같은 소스 줄을 여러 명령으로 복제할 수 있습니다.`;parent.append(note);
          const items=new Map(f.instructions.map(i=>[i.offset,i]));
          const location=offset=>{const i=items.get(offset);return i?`${offset} · ${i.opcode} · ${i.source?`${i.source.start.line+1}:${i.source.start.character+1}`:'암묵적 위치'}`:'종료 (-1)'};
          table(parent,['조건 명령 · 소스 위치','분기 결과','다음 명령','영향받는 명령 · 소스 위치'],c.links.map(e=>[location(e.controller),e.outcome,e.successor,location(e.dependent)]));
          const details=document.createElement('details'),summary=document.createElement('summary');summary.textContent='후지배·정상 전이 상세';details.append(summary);parent.append(details);
          table(details,['명령','바로 뒤 후지배 명령'],c.postdominators.map(p=>[location(p.offset),location(p.immediate)]));
          table(details,['출발','도착','전이','분기 결과'],c.transfers.map(e=>[e.source,e.target,e.kind,e.outcome]));
        }
        """;
}
