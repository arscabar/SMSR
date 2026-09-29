namespace SMSR.App.Mvp;

internal static class GraphPythonPage
{
    public const string Rendering = """
        function renderPython(parent, compiler) {
          const heading=document.createElement('h3');heading.textContent='Python 컴파일러 변수·제어 근거';parent.append(heading);
          const note=document.createElement('p');note.textContent=`${compiler.status} · CPython ${compiler.runtime||'미확인'} · 대상 코드는 실행하지 않음. 슬롯은 변수 저장 위치이며 실제 값·호출 대상·taint 판정이 아닙니다. 예외 범위는 처리 후보, 중단 후 연결은 재개 가능 위치입니다.`;parent.append(note);
          if(compiler.diagnostics) table(parent,['컴파일 진단','줄'],compiler.diagnostics.map(d=>[d.code,d.line]));
          table(parent,['코드 범위','시작 줄','명령','전이','예외 범위'],compiler.functions.map(f=>[f.name,f.firstLine,f.instructions.length,f.edges.length,f.exceptionRegions.length]));
          compiler.functions.slice(0,100).forEach(f=>{
            const details=document.createElement('details'),summary=document.createElement('summary');summary.textContent=`Python 상세 · ${f.name} · ${f.id}`;details.append(summary);parent.append(details);
            renderPythonDefinitions(details,f.definitions);
            renderPythonSummary(details,f.valueSummary);
            renderPythonValues(details,f.values);
            renderPythonControl(details,f);
            const bindings=Object.entries(f.locals).map(([name,id])=>[name,'지역',id]);
            Object.entries(f.cells).forEach(([name,id])=>bindings.push([name,'클로저 저장소',id]));
            Object.entries(f.free).forEach(([name,id])=>bindings.push([name,'외부 클로저',id]));
            table(details,['변수','종류','저장소 ID'],bindings);
            const pos=s=>s?`${s.start.line+1}:${s.start.character+1}`:'암묵적 위치';
            table(details,['위치','명령 위치','명령','이름 조회','저장소'],f.instructions.map(i=>[pos(i.source),i.offset,i.opcode,i.binding?`${i.binding.name} · ${i.binding.kind}`:'',i.binding?.bindingId||'']));
            table(details,['출발 명령','도착 명령','전이'],f.edges.map(e=>[e.source,e.target,e.kind]));
            table(details,['보호 시작','끝(미포함)','처리 명령','스택 깊이'],f.exceptionRegions.map(e=>[e.start,e.end,e.target,e.depth]));
          });
          if(compiler.functions.length>100) parent.append(document.createTextNode('상세는 앞 100개 코드 범위만 표시합니다. 전체는 원자료에서 확인하세요.'));
        }
        """;
}
