namespace SMSR.App.Mvp;

internal static class GraphTypeScriptValuesPage
{
    internal const string Rendering="""
        function renderTypeScriptValues(parent,r,at){
          const title=document.createElement('h3');title.textContent='함수 내부 값·도달 정의 후보';parent.append(title);
          const note=document.createElement('p');note.textContent='대입·if/switch·while/for/do·break/continue·증감/복합 대입의 정상 흐름 후보입니다. switch는 case 식의 대입과 본문 fall-through를 구분해 합칩니다. 조건 만족 가능성·예외·힙/별칭·연산자 효과·함수 간 요약은 확정하지 않습니다. 외부 호출 반환은 입력과 연결하지 않습니다. 제어 간선은 구문상 조건 근거이며 완전한 제어 의존이 아닙니다.';parent.append(note);
          for(const f of r.functions||[]){
            const details=document.createElement('details'),summary=document.createElement('summary'),v=f.values;
            summary.textContent=`${at(f)} · ${v?.status||'구버전: 재분석 필요'} · ${v?.reason||''} · ${v?.nodes.length||0}노드/${v?.edges.length||0}간선`;
            details.append(summary);parent.append(details);if(!v)continue;
            const nodes=new Map(v.nodes.map(n=>[n.id,n]));
            table(details,['종류','위치','심벌/기준 위치 ID'],v.nodes.map(n=>[n.kind,at(n),n.symbolId||n.anchorId]));
            table(details,['출발','도착','관계'],v.edges.map(e=>[
              `${nodes.get(e.source)?.kind} · ${at(nodes.get(e.source))}`,
              `${nodes.get(e.target)?.kind} · ${at(nodes.get(e.target))}`,e.relation]));
          }
        }
        """;
}
