namespace SMSR.App.Mvp;

internal static class GraphJavaValuesPage
{
    internal const string Rendering="""
        function renderJavaValues(parent,r,at){
          const title=document.createElement('h3');title.textContent='Java 메서드 내부 값·도달 정의 후보';parent.append(title);
          const note=document.createElement('p');note.textContent='대입·분기·반복·반환의 정상 흐름 후보입니다. 제어 간선은 구문상 조건 근거입니다. 예외·힙/별칭·변환 효과·실제 조건 만족성·함수 간 요약은 포함하지 않습니다. 호출 반환은 입력과 분리합니다. 미지원 메서드는 빈 그래프와 사유를 표시합니다.';parent.append(note);
          for(const m of r.methods||[]){
            const v=m.values,d=document.createElement('details'),s=document.createElement('summary');
            s.textContent=`${m.id} · ${at(m.source)} · ${v.status} · ${v.reason||''} · ${v.nodes.length}노드/${v.edges.length}간선`;d.append(s);parent.append(d);
            const nodes=new Map(v.nodes.map(n=>[n.id,n]));
            table(d,['종류','위치','심벌 ID'],v.nodes.map(n=>[n.kind,at(n.source),n.symbolId]));
            table(d,['출발','도착','관계'],v.edges.map(e=>[`${nodes.get(e.source)?.kind} · ${at(nodes.get(e.source)?.source)}`,`${nodes.get(e.target)?.kind} · ${at(nodes.get(e.target)?.source)}`,e.relation]));
          }
        }
        """;
}
