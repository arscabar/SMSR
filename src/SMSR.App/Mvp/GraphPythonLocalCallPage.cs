namespace SMSR.App.Mvp;

internal static class GraphPythonLocalCallPage
{
    public const string Rendering = """
        function renderPythonLocalCall(parent,c) {
          const target=c.localTarget;if(!target)return;
          const note=document.createElement('p');note.textContent=`지역 함수 객체 연결 · ${target.status} · ${target.bindingReason||target.reason||'지역 값 전달 근거'} · 미확정 후보 포함: ${target.hasUnknown?'예':'아니요'}`;parent.append(note);
          table(parent,['대상 본문 ID'],target.bodyIds.map(id=>[id]));
          if(target.arguments.length)table(parent,['호출 인자 값','대상 입력 슬롯','매개변수 값'],target.arguments.map(a=>[a.argumentValueId,a.parameterIndex,a.parameterValueId]));
          if(target.returns.length)table(parent,['본문 반환 값','이번 호출 결과 값'],target.returns.map(r=>[r.returnValueId,r.resultValueId]));
        }
        """;
}
