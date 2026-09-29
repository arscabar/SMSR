namespace SMSR.App.Mvp;

internal static class GraphCSharpValuesPage
{
    internal const string Rendering = """
        function renderCSharpValues(parent,f,name,describe) {
          const v=f.definitions?.values,detail=document.createElement('details'),summary=document.createElement('summary');
          summary.textContent='값 전달 상세 · '+name(f.symbolId);detail.append(summary);parent.append(detail);
          const p=document.createElement('p');detail.append(p);
          if(!v||v.status==='UNAVAILABLE'){p.textContent='값 의존 분석 불가: '+(v?.limitations.join(', ')||'구버전 결과: 다시 분석하세요.');return}
          p.textContent=`값 근거 ${v.nodes.length}개 · 연결 ${v.links.length}개 · ${v.status==='PARTIAL_VALUE_DEPENDENCE'?'미해석 구간 포함':'함수 내부 값 의존'}. 값 자체·실행 순서·taint 인증이 아닙니다. 이 내부 그래프는 호출 경계를 보존하며, 해석 가능한 함수 간 연결은 별도 호출별 반환 의존에서 확인합니다. 힙·사용자 연산자는 미해석입니다.`;
          const nodes=new Map(v.nodes.map(n=>[n.id,n]));
          const kind=k=>({ENTRY:'호출 입력',READ:'변수 읽기',WRITE:'변수 쓰기',FlowCapture:'임시 값 저장',FlowCaptureReference:'임시 값 읽기'})[k]||k;
          const at=id=>{const n=nodes.get(id);return `${id} · ${kind(n.kind)}${n.symbolId?' · '+name(n.symbolId):''} · ${describe(n.source)}`};
          table(detail,['경계','값 근거'],v.ports.map(p=>[({RETURN:'반환',CONDITION:'조건',CALL_ARGUMENT:'호출 인자',CALL_RESULT:'호출 결과',CALL_RECEIVER:'호출 수신자'})[p.kind]||p.kind,at(p.value)]));
          const unknown=v.nodes.filter(n=>n.status!=='LOCAL_VALUE');
          table(detail,['미해석 값','사유'],unknown.map(n=>[at(n.id),({OPAQUE_CALL:'함수 간 결과 미해석',OPAQUE_HEAP:'힙 상태 미해석',OPAQUE_OPERATOR:'사용자 연산자 미해석',OPAQUE_RECEIVER:'수신 객체 미해석'})[n.status]||n.status]));
          table(detail,['입력 근거','관계','결과 근거'],v.links.map(l=>[at(l.source),({OPERAND:'연산 입력',ASSIGNMENT:'할당',REACHING_DEFINITION:'도달 정의',CAPTURE_DEFINITION:'임시 값 도달 정의'})[l.relation]||l.relation,at(l.target)]));
        }
        """;
}
