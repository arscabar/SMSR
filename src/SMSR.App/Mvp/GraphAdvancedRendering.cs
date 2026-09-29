namespace SMSR.App.Mvp;

internal static class GraphAdvancedRendering
{
    public const string Script = """
        function table(parent, columns, rows) {
          const table=document.createElement('table'), head=table.createTHead().insertRow();
          columns.forEach(c=>{const th=document.createElement('th');th.textContent=c;head.append(th)});
          rows.slice(0,100).forEach(values=>{const row=table.insertRow();values.forEach(value=>{const cell=row.insertCell();cell.textContent=typeof value==='object'?JSON.stringify(value):String(value??'')})});
          parent.append(table);
          if(rows.length>100) parent.append(document.createTextNode(`앞 100개 표시 / 전체 ${rows.length}개. 원자료에서 전체 확인.`));
        }
        function renderResult(parent, action, data) {
          parent.replaceChildren();
          if(data.error){parent.textContent=data.error;return}
          const title=document.createElement('p');parent.append(title);
          if(action==='cypher') {
            title.textContent=`리비전 ${data.revision} · ${data.dialect} · ${data.rows.length}행${data.truncated?' · 결과 제한 적용':''}`;
            table(parent,data.columns,data.rows);
          } else if(action==='semantic') {
            title.textContent=`리비전 ${data.revision} · ${data.model}`;
            table(parent,['제목','파일','줄','유사도'],data.hits.map(h=>[h.node.label,h.node.sourcePath,h.node.line,h.score.toFixed(4)]));
          } else if(action==='body-semantic') {
            title.textContent=`리비전 ${data.revision} · ${data.model} · 범위 ${data.scope||'전체'} · ${data.files}파일 / ${data.chunks}조각 · 민감 패턴 제외 ${data.excludedFiles}파일`;
            const list=document.createElement('ol');parent.append(list);
            data.hits.forEach(h=>{const item=document.createElement('li'),link=document.createElement('a');link.href=h.sourceUrl;link.textContent=`${h.path}:${h.line}–${h.endLine} · 유사도 ${h.score.toFixed(4)}`;item.append(link);list.append(item)});
          } else if(action==='csharp'||action==='csharp-result') {
            renderCSharp(parent,title,data);
          } else if(action==='java'||action==='java-result') {
            renderJava(parent,title,data);
          } else if(action==='jdt'||action==='jdt-result') {
            renderJdt(parent,title,data);
          } else if(action==='typescript'||action==='typescript-result') {
            renderTypeScript(parent,title,data);
          } else {
            const report=data.report||data, r=report.result;
            title.textContent=`리비전 ${report.revision} · ${r.language||'미지원'} · ${r.status} · ${data.stale?'갱신 필요':'저장된 분석'} · ${report.analyzedAt}`;
            table(parent,['선언','종류','줄','해소 상태'],r.symbols.map(s=>[s.name,s.kind,s.line,s.resolution]));
            if(r.compiler) renderPython(parent,r.compiler);
            const heading=document.createElement('h3');heading.textContent='검토할 위험 경로 (확정 취약점 아님)';parent.append(heading);
            if(!r.findings.length) parent.append(document.createTextNode('발견된 경로 없음. 코드의 안전성을 보장하지 않습니다.'));
            r.findings.slice(0,100).forEach(f=>{const p=document.createElement('p');p.textContent=f.path.map(id=>{const s=r.statements.find(s=>s.id===id);return s?`${s.line}줄 ${s.kind}`:id}).join(' → ');parent.append(p)});
            const heading2=document.createElement('h3');heading2.textContent='데이터·제어 의존 관계 (보수적 추정)';parent.append(heading2);
            table(parent,['출발','도착','관계','변수'],r.edges.map(e=>[e.source,e.target,e.relation,e.variable||'']));
          }
          const details=document.createElement('details'), summary=document.createElement('summary'), raw=document.createElement('pre');
          summary.textContent='전체 원자료·지원 범위 보기';raw.textContent=JSON.stringify(data,null,2);details.append(summary,raw);parent.append(details);
        }
        """;
}
