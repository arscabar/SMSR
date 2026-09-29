namespace SMSR.App.Mvp;

internal static class GraphCSharpReturnsPage
{
    internal const string Rendering = """
        function renderCSharpReturns(parent,r,f,name,describe) {
          const detail=document.createElement('details'),summary=document.createElement('summary');
          summary.textContent='호출별 반환 의존 · '+name(f.symbolId);detail.append(summary);parent.append(detail);
          const p=document.createElement('p');detail.append(p);
          const s=r.returnSummaries?.find(s=>s.symbolId===f.symbolId&&describe(s.source)===describe(f.source));
          if(!s){p.textContent='구버전 결과: 반환 의존을 보려면 다시 분석하세요.';return}
          const parameters=s.parameterOrdinals.map(i=>f.boundary.parameters.find(p=>p.ordinal===i)?.name||`#${i}`);
          const state={UNAVAILABLE:'반환 요약 불가',PARTIAL_RETURN_DEPENDENCE:'미확정 반환 영향 포함',MODELED_RETURN_DEPENDENCE:'모델 범위 내 반환 의존'};
          p.textContent=`${state[s.status]||s.status} · 확인된 입력: ${parameters.join(', ')||'없음'} · 호출 결과 ${s.calls.length}개. 호출별 인자를 분리합니다. 미확정 상태의 입력 없음은 무관함을 뜻하지 않습니다. 제어·힙·경로 조건·종료 보장·taint 판정은 제외하며 재귀 값 순환은 미확정으로 남깁니다.`;
          const nodes=new Map((f.definitions?.values?.nodes||[]).map(n=>[n.id,n]));
          const at=id=>`${id} · ${describe(nodes.get(id)?.source)}`;
          table(detail,['호출 위치 / 대상','연결 판정','값 불확실성','결과 근거'],s.calls.map(c=>[
            `${describe(c.source)} · ${name(c.targetId)}`,c.status==='RETURN_LINKED'?'반환 요약 연결':c.status,c.uncertain?'미확정 영향 있음':'모델 범위 내 확인',at(c.resultValue)]));
          table(detail,['호출 위치','입력 값 근거','반환 값 근거'],s.calls.flatMap(c=>c.links.map(l=>[describe(c.source),at(l.source),at(l.target)])));
        }
        """;
}
