namespace SMSR.App.Mvp;

internal static class GraphJavaControlPage
{
    internal const string Rendering="""
        function renderJavaControl(parent,r,at){
          renderNormalControl(parent,r.methods||[],at,'Java');
        }
        function renderNormalControl(parent,methods,at,language){
          const title=document.createElement('h3');title.textContent=`${language} 정상 경로 · 조건별 제어 의존`;parent.append(title);
          const note=document.createElement('p');note.textContent='컴파일러 AST의 정상 분기·단락·삼항·반복·break/continue·반환·명시적 throw 근거입니다. 유한 종료 경로의 후지배를 사용하며, 암묵적 예외·호출 내부 효과·실제 경로 만족성·함수 간 제어/taint는 해석하지 않습니다. 구문상 조건 후보와 별도입니다.';parent.append(note);
          for(const m of methods){
            const c=m.control,d=document.createElement('details'),s=document.createElement('summary');
            s.textContent=`${language} 제어 · ${m.name||m.id} · ${c?.status||'갱신 필요'} · ${c?.reason||''} · ${c?.links?.length||0}관계`;d.append(s);parent.append(d);
            if(!c)continue;
            const nodes=new Map(c.nodes.map(n=>[n.id,n]));
            const label=id=>id===-1?'합성 종료':`${id} ${nodes.get(id)?.kind||''} · ${at(nodes.get(id)?.source)}`;
            if(c.status==='UNAVAILABLE'){const p=document.createElement('p');p.textContent='제어 의존 미제공. 종료 불가 사유이면 도달 가능한 정상 전이는 아래에서 확인할 수 있습니다.';d.append(p);}
            table(d,['조건/전이','결과','의존 대상'],(c.links||[]).map(e=>[label(e.controller),e.outcome,label(e.dependent)]));
            table(d,['정상 전이 출발','도착','결과'],(c.transfers||[]).map(e=>[label(e.source),label(e.target),e.outcome]));
            table(d,['노드','즉시 후지배'],(c.postdominators||[]).map(p=>[label(p.offset),label(p.immediate)]));
          }
        }
        """;
}
