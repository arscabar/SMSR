namespace SMSR.App.Mvp;

internal static class SmsrMcpInstructions
{
    public const string Text = """
        SMSR은 에이전트를 호출하지 않습니다. lifecycle 훅은 연결 상태만 자동 기록하며 작업 그래프는 사용자가 그래프, 흐름, 대시보드 또는 SMSR 워크플로우로 추적해 달라고 명시적으로 요청한 경우에만 생성하세요. 일반 작업에서는 save_plan, record_heartbeat, record_event를 호출하지 마세요. 그래프 요청이 시작되면 projectId는 저장소 폴더명, workflowId는 현재 Codex task ID를 기반으로 해당 요청 범위에서 안정적으로 유지하세요. 메인 에이전트는 save_plan을 호출하고 각 에이전트는 자신의 heartbeat와 의미 있는 상태 변화만 전송합니다. 관련 후속 요청은 완료 전까지 같은 그래프를 이어가고, 모든 작업이 SUCCESS, FAILED 또는 BLOCKED가 되면 최종 record_event를 보낸 뒤 heartbeat를 중단하세요. 완료된 그래프에 이후의 무관한 요청을 추가하지 마세요. 셸 명령, 프롬프트 원문, 비밀과 개별 도구 호출은 기록하지 마세요.
        """;
}
