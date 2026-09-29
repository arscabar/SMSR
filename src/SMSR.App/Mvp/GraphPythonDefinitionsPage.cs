namespace SMSR.App.Mvp;

internal static class GraphPythonDefinitionsPage
{
    public const string Rendering = """
        function renderPythonDefinitions(parent, model) {
          if(!model) return;
          const heading=document.createElement('h4');heading.textContent='할당 → 읽기 도달 정의';parent.append(heading);
          const note=document.createElement('p');note.textContent=`${model.status}${model.reason?' · '+model.reason:''} · ${model.boundaries.join(', ')}. 바이트코드 모델의 후보 합집합이며 실제 실행·값 전파·안전성 판정이 아닙니다.`;parent.append(note);
          const origins=new Map(model.definitions.map(d=>[d.id,d]));
          const reads=new Map(model.reads.map(r=>[r.id,r]));
          const pos=s=>s?`${s.start.line+1}:${s.start.character+1}`:'입구';
          const kinds={PARAMETER:'입력 매개변수',WRITE:'할당',UNBOUND_ENTRY:'입구 미할당',UNBOUND_DELETE:'삭제 후 미할당'};
          table(parent,['변수','정의 근거','읽기 근거','미할당 후보'],model.edges.map(e=>{
            const d=origins.get(e.source),r=reads.get(e.target);
            return [r.name,`${kinds[d.kind]||d.kind} · ${pos(d.source)} · ${d.offset??'입구'}`,`${pos(r.source)} · ${r.offset}`,r.mayBeUnbound?'있음':'없음'];
          }));
          const unreachable=model.reads.filter(r=>!r.reachable);
          if(unreachable.length) table(parent,['모델상 미도달 읽기','위치','명령'],unreachable.map(r=>[r.name,pos(r.source),r.offset]));
        }
        """;
}
