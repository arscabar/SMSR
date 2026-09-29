namespace SMSR.App.Mvp;

internal static class GraphCSharpPage
{
    internal const string Section = """
        <section class="panel csharp-panel"><style>.csharp-panel label{display:block}</style><h2>C# 명시적 파일 묶음 분석</h2>
        <p>선택한 파일만 하나의 컴파일 단위로 해석합니다. 기본 C# 12·호스트 .NET 기준이며 실제 csproj 설정·NuGet·생성 코드·빌드 훅은 반영하지 않습니다. 가상/delegate 호출은 정적 대상만 표시하고 동적 호출은 미해소로 남깁니다. 전체 프로그램 PDG가 아닙니다. .NET 10 런타임과 분석기 포함 배포본이 필요합니다.</p>
        <label for="csharp-input">색인된 C# 상대 경로 (한 줄에 하나, 최대 500개)</label><textarea id="csharp-input" placeholder="src/Library.cs&#10;src/Entry.cs"></textarea>
        <label for="csharp-version">언어 버전</label><input id="csharp-version" value="CSharp12">
        <label for="csharp-defines">조건부 심벌 (쉼표 구분)</label><input id="csharp-defines" placeholder="DEBUG,FEATURE">
        <button data-action="csharp">묶음 분석·저장</button><button data-action="csharp-result">묶음 저장 결과</button>
        <div id="csharp-result" role="status"></div></section>
        """;

    internal const string Rendering = """
        function renderCSharp(parent,title,data) {
          const report=data.report||data,r=report.result;
          title.textContent=`리비전 ${report.revision} · ${r.status} · ${r.profile} · ${r.languageVersion} · .NET ${r.runtime} · ${data.stale?'갱신 필요':'입력 묶음 기준'} · 진단 ${r.diagnosticCount}개${r.diagnosticsTruncated?' (일부 표시)':''} · ${report.analyzedAt}`;
          const describe=s=>s?`${s.path}:${s.start.line+1}:${s.start.character+1}`:'';
          table(parent,['호출 위치','컴파일러 대상','판정','호출 방식'],r.calls.map(c=>[describe(c.source),c.signature||c.candidates.join(', '),c.resolution,c.dispatch]));
          table(parent,['진단 코드','수준','근거'],r.diagnostics.map(d=>[d.code,d.severity,describe(d.source)]));
          renderCSharpConnections(parent,r,describe);
          renderCSharpFlow(parent,r,describe);
          const links=document.createElement('ul');parent.append(links);
          r.paths.forEach(path=>{const li=document.createElement('li'),a=document.createElement('a');a.href='/graph/source?'+new URLSearchParams({projectId:document.querySelector('main').dataset.project,path,line:'1'});a.textContent=path;li.append(a);links.append(li)});
        }
        """;
}
