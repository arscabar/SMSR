namespace SMSR.App.Mvp;

internal static class GraphJavaSummaryPage
{
    internal const string Rendering="""
        function renderJavaSummaries(parent,r,at){
          const title=document.createElement('h3');title.textContent='Java 호출별 본문·반환 데이터 요약';parent.append(title);
          const note=document.createElement('p');note.textContent='직접 호출의 본문 데이터 의존 후보입니다. 다단계·재귀를 계산하고 호출별 값을 격리합니다. 가상/가변 호출은 미해석입니다. 제어 의존·종료·예외·힙/별칭·변환 효과는 포함하지 않습니다. 빈 영향/미해석 목록은 안전성이나 완전성을 뜻하지 않습니다. 입력 순서는 0부터입니다.';parent.append(note);
          table(parent,['호출 ID','본문','판정/사유','입력/반환 수'],(r.connections||[]).map(c=>[c.callId,c.bodyId,c.reason||c.status,`${c.inputs.length}/${c.returns.length}`]));
          for(const m of r.methods||[]){
            const v=m.valueSummary,d=document.createElement('details'),s=document.createElement('summary');
            s.textContent=`${m.id} · ${v?.status||'구버전: 재분석 필요'} · 호출 요약 ${v?.callEdges.length||0}개`;d.append(s);parent.append(d);if(!v)continue;
            table(d,['반환 값 ID','영향 입력 순서','미해석 값 ID'],v.returns.map(x=>[x.returnValueId,x.parameterIndices.join(', '),x.unknownValueIds.join(', ')]));
            table(d,['호출 ID','본문','입력 순서','출발','도착'],v.callEdges.map(e=>[e.callId,e.bodyId,e.parameterIndex,e.source,e.target]));
          }
        }
        """;
}
