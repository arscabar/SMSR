namespace SMSR.App.Mvp;

internal static class GraphTypeScriptPage
{
    internal const string Section="""
        <section class="panel"><h2>JavaScript·TypeScript·TSX 컴파일러 분석</h2>
        <p>설치된 Node와 VS Code의 TypeScript 6.0을 사용합니다. 선택 파일·표준 타입만 읽고 프로젝트 설정·패키지·빌드·대상 코드는 실행하지 않습니다. 일부 정상 CFG·조건별 제어 의존을 제공합니다. TRUE/FALSE는 진릿값, NULLISH/NON_NULLISH는 null/undefined 여부입니다. switch의 CASE_MATCH/CASE_NO_MATCH는 최초 선택값과 case 식의 엄격 일치/불일치 경로이며 실제 값을 판정한 결과가 아닙니다. case 본문은 다음 조건식을 거치지 않고 이어질 수 있습니다. 실제 실행 대상·전체 PDG/taint는 아닙니다. 오류가 있으면 후보로 표시합니다. 컴파일러 변경 후에는 다시 분석하세요.</p>
        <label for="typescript-input">색인된 JS/JSX/TS/TSX 상대 경로 (한 줄에 하나, 최대 500개·16 MiB)</label>
        <textarea id="typescript-input" placeholder="samples/graph-typescript/math.ts"></textarea>
        <button data-action="typescript">TS/JS 묶음 분석·저장</button><button data-action="typescript-result">TS/JS 저장 결과 확인</button>
        <div id="typescript-result" role="status"></div></section>
        """;
    internal const string Rendering="""
        function renderTypeScript(parent,title,data){
          const report=data.report||data,r=report.result;
          const at=s=>s?`${s.path}:${s.line||'?'} · UTF16 ${s.start}`:'';
          title.textContent=`TypeScript ${r.compilerVersion} · ${r.status} · 리비전 ${report.revision} · ${data.stale?'갱신 필요':'저장된 분석'} · ${r.diagnosticCount}개 진단 · ${report.analyzedAt}`;
          table(parent,['호출 위치','정적 연결','선택 시그니처','반환 타입'],r.calls.map(c=>[at(c),c.binding,at(c.target),c.returnType]));
          renderTypeScriptConnections(parent,r,at);
          renderTypeScriptSummaries(parent,r,at);
          renderNormalControl(parent,r.functions||[],at,'JS/TS');
          renderTypeScriptValues(parent,r,at);
          const args=document.createElement('details'),summary=document.createElement('summary');summary.textContent='인수·매개변수 대응 (실제 실행 흐름 아님)';args.append(summary);parent.append(args);
          table(args,['호출','대응 범위','인수 순서','타입','매개변수 ID'],r.calls.flatMap(c=>c.arguments.map(a=>[at(c),c.mapping,a.index,a.type,a.parameterSymbolId])));
          table(parent,['심벌','출처','선언 위치'],r.symbols.map(s=>[s.name,s.origin,s.declarations.map(at).join(', ')]));
          table(parent,['참조 위치','역할','심벌 ID'],r.references.map(s=>[at(s),s.role,s.symbolId]));
          table(parent,['진단 코드','종류','위치'],r.diagnostics.map(d=>[d.code,d.category,at(d)]));
          if(r.diagnosticsTruncated)parent.append(document.createTextNode('진단 앞 200개 표시. 전체 개수는 위 상태에 표시됩니다.'));
        }
        """;
}
