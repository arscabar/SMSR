namespace SMSR.App.Mvp;

internal static class GraphTypeScriptSummaryPage
{
    internal const string Rendering="""
        function renderTypeScriptSummaries(parent,r,at){
          const title=document.createElement('h3');title.textContent='호출별 반환 데이터 의존 요약';parent.append(title);
          const note=document.createElement('p');note.textContent='본문 값 경로로 확인한 입력만 호출 결과에 연결한 후보입니다. 재귀는 고정점으로 계산합니다. 제어 의존·종료·실제 호출 대상·예외·힙 효과는 포함하지 않습니다. 미해석 값 0개도 안전성이나 완전성을 뜻하지 않습니다. 입력 순서는 0부터 시작합니다.';parent.append(note);
          for(const f of r.functions||[]){
            const s=f.valueSummary,details=document.createElement('details'),summary=document.createElement('summary');
            summary.textContent=`${at(f)} · ${s?.status||'구버전: 재분석 필요'} · ${s?.reason||''} · 호출 요약 ${s?.callEdges.length||0}개`;
            details.append(summary);parent.append(details);if(!s)continue;
            table(details,['반환 값 ID','영향 입력 순서','미해석 값 ID'],s.returns.map(x=>[x.returnValueId,x.parameterIndices.join(', '),x.unknownValueIds.join(', ')]));
            table(details,['호출 ID','본문 ID','입력 순서','출발 값','도착 값'],s.callEdges.map(e=>[e.callId,e.bodyId,e.parameterIndex,e.source,e.target]));
          }
        }
        """;
}
