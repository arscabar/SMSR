namespace SMSR.App.Mvp;

internal static class GraphCSharpConnectionsPage
{
    internal const string Rendering = """
        function renderCSharpConnections(parent,r,describe) {
          if(!r.connections)return;
          const title=document.createElement('h3');title.textContent='함수 간 인자·반환 연결';parent.append(title);
          const note=document.createElement('p');note.textContent='컴파일러의 매개변수 대응과 도달 가능한 반환 후보입니다. 실제 실행 순서·값 전파·taint 확정이 아닙니다. 가상/동적/조건부 호출·본문 없는 함수에는 반환 연결을 만들지 않습니다. 참조 인자와 비동기·iterator·참조 반환은 별도 분석이 필요합니다.';parent.append(note);
          const calls=new Map(r.calls.map(c=>[JSON.stringify(c.source),c.signature]));
          const parameters=new Map(r.functions.flatMap(f=>f.boundary?.parameters||[]).map(p=>[p.id,p.name]));
          const label=c=>calls.get(JSON.stringify(c.source))||'미해소 호출';
          const states={BOUNDARY_LINKED:'입출력 근거 연결',STATIC_TARGET_ONLY:'정적 대상만 확인',CONDITIONAL_CALL:'조건부 호출',NO_SOURCE_BODY:'대상 본문 없음',UNRESOLVED:'미해소',AMBIGUOUS:'모호',COMPILER_CANDIDATE:'컴파일 오류: 후보',AMBIGUOUS_BODY:'본문 모호'};
          table(parent,['호출 / 대상','판정','인자 대응','반환 후보'],r.connections.map(c=>[
            `${describe(c.source)} · ${label(c)}`,states[c.status]||c.status,c.inputs.length,c.returns.length]));
          r.connections.slice(0,100).forEach(c=>{
            const d=document.createElement('details'),s=document.createElement('summary');
            s.textContent=`입출력 상세 · ${label(c)} · ${describe(c.source)}`;d.append(s);parent.append(d);
            table(d,['인자 근거','매개변수 / 근거','종류','관계'],c.inputs.map(a=>[
              describe(a.argument.valueSource),`${parameters.get(a.parameterId)||'매개변수'} · ${describe(a.parameterSource)}`,
              `${a.argument.kind} · ${a.argument.refKind}${a.argument.implicit?' · 암묵적':''}`,
              a.relation==='ARGUMENT_PARAMETER'?'인자 대응':'참조 별칭 분석 필요']));
            table(d,['반환 후보 근거','호출 지점','판정'],c.returns.map(a=>[describe(a.source),describe(a.callSource),'조건 경로 미검증 후보']));
            if(c.receiver){const p=document.createElement('p');p.textContent='수신 객체 근거: '+describe(c.receiver.source)+' (힙/별칭 전파 미검증)';d.append(p)}
            const limits=document.createElement('p');limits.textContent='분석 제한: '+c.limitations.join(', ');d.append(limits);
          });
        }
        """;
}
