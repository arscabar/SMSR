namespace SMSR.App.Mvp;

internal static class GraphPythonSummaryPage
{
    public const string Rendering = """
        function renderPythonSummary(parent,s) {
          if(!s)return;
          const title=document.createElement('h4');title.textContent='입력 → 반환·호출 요약';parent.append(title);
          const note=document.createElement('p');note.textContent=`${s.status} · ${s.reason||'본문·지원 지역 호출 데이터 의존 후보'}. 입력 번호는 컴파일러 슬롯 순서입니다. 실제 인자 대응은 본문 연결 표로 확인하세요. 조건 영향은 별도 제어 근거입니다. 미해석 목록이 비어도 안전함을 뜻하지 않습니다.`;parent.append(note);
          table(parent,['입력 슬롯','이름','종류'],s.parameters.map(p=>[p.index,p.name,p.kind]));
          const pos=n=>n.source?`${n.source.start.line+1}:${n.source.start.character+1}`:'암묵적 위치';
          const row=(name,n)=>[name,pos(n),n.parameterIndices.join(', ')||'없음',n.unknownValueIds.join(', ')||'없음',n.valueId];
          table(parent,['반환','위치','영향 입력 슬롯','미해석 값','값 ID'],s.returns.map((n,i)=>row(i,n)));
          if(s.callEdges?.length)table(parent,['호출 인자 값','호출 결과 값','본문 ID','본문 입력 슬롯'],s.callEdges.map(e=>[e.source,e.target,e.bodyId,e.parameterIndex]));
          s.calls.forEach(c=>{
            const h=document.createElement('p');h.textContent=`호출 명령 ${c.offset} · 결과 값 ${c.resultValueId||'없음'}`;parent.append(h);
            if(c.resultSummary)table(parent,['호출 결과 영향 입력','미해석 값'],[[c.resultSummary.parameterIndices.join(', ')||'없음',c.resultSummary.unknownValueIds.join(', ')||'없음']]);
            const rows=[];
            if(c.target)rows.push(row('대상 후보',c.target));
            if(c.receiver)rows.push(row('수신 객체 후보',c.receiver));
            c.arguments.forEach(a=>rows.push(row(`인자 ${a.index} · ${a.keyword?'이름 인자':'위치 인자'}`,a)));
            table(parent,['호출 입력','위치','영향 입력 슬롯','미해석 값','값 ID'],rows);
            renderPythonLocalCall(parent,c);
          });
          const boundary=document.createElement('p');boundary.textContent='유일한 지역 본문 후보의 명시적 위치·이름 인자를 매개변수에 대응시켜 반환 데이터 영향을 전파합니다. 위치 전용·이름 전용 제약과 중복/누락을 검사합니다. 이름 인자의 이름은 매칭 중에만 사용하고 저장하지 않습니다. 생략 기본값·가변 인자·공유셀은 연결하지 않습니다. 미해소 호출·본문 내부 미해석 값은 유지합니다. 실제 실행 대상·함수 객체 변경·제어/예외/종료/힙 효과는 확정하지 않습니다. 빈 목록은 안전성 증명이 아닙니다.';parent.append(boundary);
        }
        """;
}
