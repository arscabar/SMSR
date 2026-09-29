namespace SMSR.App.Mvp;

internal static class GraphCSharpArgumentsPage
{
    internal const string Rendering = """
        function renderCSharpArguments(parent,r,f,name,describe) {
          const detail=document.createElement('details'),summary=document.createElement('summary');
          summary.textContent='호출 인자 경로 · '+name(f.symbolId);detail.append(summary);parent.append(detail);
          const s=r.returnSummaries?.find(s=>s.symbolId===f.symbolId&&describe(s.source)===describe(f.source)),a=s?.arguments;
          const p=document.createElement('p');detail.append(p);
          if(!a||a.status==='UNAVAILABLE'){p.textContent='인자 경로 분석 불가: '+(a?.limitations.join(', ')||'구버전 결과: 다시 분석하세요.');return}
          p.textContent=`호출 인자 ${a.origins.length}개. 반환값 없는 호출도 포함합니다. 입력별 대표 데이터 경로 하나이며 실행 이력·취약점 판정은 아닙니다. 반환 요약 간선은 대상 본문 전체 경로를 생략한 연결입니다. 미확정 값의 경로 없음은 안전함을 뜻하지 않습니다.`;
          const nodes=new Map(f.definitions.values.nodes.map(n=>[n.id,n]));
          const signatures=new Map(r.calls.map(c=>[describe(c.source)+'|'+c.targetId,c.signature]));
          const target=o=>signatures.get(describe(o.callSource)+'|'+o.targetId)||name(o.targetId);
          const input=i=>f.boundary.parameters.find(p=>p.ordinal===i)?.name||`#${i}`;
          const at=id=>`${id} · ${nodes.get(id)?.kind} · ${describe(nodes.get(id)?.source)}`;
          table(detail,['호출 / 인자 순번(0부터)','확인된 입력','값 불확실성 / 대상 연결'],a.origins.map(o=>[
            `${target(o)} · ${describe(o.callSource)} · #${o.parameterOrdinal??'미상'}`,
            o.dependencies.map(d=>input(d.parameterOrdinal)).join(', ')||'없음',`${o.uncertain?'미확정 영향 있음':'모델 범위 내 확인'} · ${o.callStatus}`]));
          a.origins.slice(0,100).forEach(o=>o.dependencies.forEach(d=>{
            const trace=document.createElement('details'),title=document.createElement('summary');
            title.textContent=`입력 ${input(d.parameterOrdinal)} → ${target(o)} 인자 #${o.parameterOrdinal} · ${describe(o.source)}`;
            trace.append(title);detail.append(trace);
            table(trace,['입력 근거','관계','다음 근거'],d.path.map(e=>[at(e.source),e.relation==='ARGUMENT_RETURN_DEPENDENCE'?'반환 요약 연결':e.relation,at(e.target)]));
          }));
          if(a.origins.length>100){const note=document.createElement('p');note.textContent='상세 경로는 앞 100개 인자만 표시합니다. 전체 근거는 원자료에서 확인하세요.';detail.append(note)}
        }
        """;
}
