namespace SMSR.App.Mvp;

internal static class GraphTypeScriptConnectionsPage
{
    internal const string Rendering="""
        function renderTypeScriptConnections(parent,r,at){
          if(!r.connections){parent.append(document.createTextNode('본문 연결이 없는 구버전입니다. 다시 분석하세요.'));return}
          const heading=document.createElement('h3');heading.textContent='호출별 본문 연결 후보';parent.append(heading);
          const note=document.createElement('p');note.textContent='정적 구현 후보입니다. 반환 도달 가능성·finally 효과·호출 대상 변경·수신 객체·힙/별칭을 확정하지 않습니다. 인수에서 반환값으로 바로 이어지는 간선을 만들지 않습니다.';parent.append(note);
          const calls=new Map(r.calls.map(c=>[c.id,c])), functions=new Map(r.functions.map(f=>[f.id,f]));
          table(parent,['호출 위치','구현 본문','연결 상태/제외 사유','입력/반환 후보'],r.connections.map(c=>[
            at(calls.get(c.callId)),at(functions.get(c.bodyId)),c.reason||c.status,`${c.inputs.length} / ${c.returns.length}`]));
          const details=document.createElement('details'),summary=document.createElement('summary');
          summary.textContent='본문 매개변수·반환 연결 상세';details.append(summary);parent.append(details);
          table(details,['호출','인수 위치','본문 매개변수','기본값 조건'],r.connections.flatMap(c=>c.inputs.map(p=>[
            at(calls.get(c.callId)),p.argument?at(p.argument):p.relation,p.parameterId,p.defaultCondition||''])));
          table(details,['반환 위치','호출 결과 위치','반환 종류'],r.connections.flatMap(c=>c.returns.map(v=>[
            at(v.source),at(calls.get(c.callId)),v.valueKind])));
          table(details,['함수 본문','반환 프로토콜','명시 반환 수','암묵 반환 검사'],r.functions.map(f=>[
            at(f),f.mode,f.returns.length,f.fallthrough]));
        }
        """;
}
