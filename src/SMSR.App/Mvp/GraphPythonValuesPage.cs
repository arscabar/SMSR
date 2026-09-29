namespace SMSR.App.Mvp;

internal static class GraphPythonValuesPage
{
    public const string Rendering = """
        function renderPythonValues(parent, model) {
          if(!model) return;
          const heading=document.createElement('h4');heading.textContent='입력 → 연산 → 할당·반환·호출 인자';parent.append(heading);
          const note=document.createElement('p');note.textContent=`${model.status}${model.reason?' · '+model.reason:''}. 분기별 정적 후보이며 호출 반환은 미해석 경계입니다. 반복/속성 조회는 사용자 코드를 호출할 수 있고 수신 객체는 실제 self 확정이 아닙니다. 실행·힙/별칭·정밀 taint·안전성 판정이 아닙니다.`;parent.append(note);
          if(model.reason) return;
          const names={PARAMETER:'입력',READ:'읽기',WRITE:'할당',UNBOUND_ENTRY:'초기 미할당',UNBOUND_DELETE:'삭제 후 미할당',CONSTANT_REDACTED:'상수(값 제외)',OPERATION:'연산',CONSTRUCTION:'구성',RETURN:'반환',CALL_ARGUMENT:'호출 인자',CALL_TARGET:'미해소 호출 대상',OPAQUE_CALL_RESULT:'미해석 호출 반환',EXTERNAL_LOOKUP:'외부 이름 조회',BRANCH_TEST:'분기 조건'};
          Object.assign(names,{ITERATOR:'반복자(미해석)',ITERATION_VALUE:'반복 요소 후보',ITERATION_END:'반복 종료 후보',ATTRIBUTE_LOOKUP:'속성 조회(미해석)',CALL_RECEIVER_CANDIDATE:'수신 객체 후보',UNPACKED_ITEM:'분해 요소 후보'});
          const label=n=>`${names[n.kind]||n.kind}${n.name?' '+n.name:''}${n.itemIndex!==undefined?' #'+n.itemIndex:''}${n.argument!==undefined?' #'+n.argument+(n.keyword?' (이름 인자)':''):''} · ${n.source?`${n.source.start.line+1}:${n.source.start.character+1}`:'입구'} · ${n.offset??'입구'}`;
          const nodes=new Map(model.nodes.map(n=>[n.id,n]));
          table(parent,['출발 근거','도착 근거','관계','피연산자 순번'],model.edges.map(e=>[label(nodes.get(e.source)),label(nodes.get(e.target)),e.relation,e.operand??'']));
          const boundaries=model.nodes.filter(n=>['OPAQUE_CALL_RESULT','EXTERNAL_LOOKUP','UNBOUND_ENTRY','UNBOUND_DELETE'].includes(n.kind));
          if(boundaries.length) table(parent,['미확정 경계'],boundaries.map(n=>[label(n)]));
          const iterations=(model.transfers||[]).filter(e=>e.kind.startsWith('ITERATION_'));
          if(iterations.length) table(parent,['반복 명령','도착 명령','전이','건너뛴 END_FOR'],iterations.map(e=>[e.source,e.target,e.kind==='ITERATION_NEXT'?'다음 요소 후보':'반복 종료 후보',e.skippedOffset??'']));
        }
        """;
}
