namespace SMSR.App.Mvp;

internal static class SmsrMcpInstructions
{
    public const string Text = """
        SMSR은 에이전트를 호출하지 않으며 일반 작업은 어떤 SMSR 기록도 만들지 않습니다. 사용자가 그래프, 흐름, 대시보드 또는 SMSR 워크플로우 추적을 명시적으로 요청한 경우에만 save_plan, record_heartbeat, record_event를 사용하세요. 새 그래프는 저장소 폴더명을 projectId로 사용하고 첫 save_plan에서 workflowId를 생략하세요. SMSR이 `yyyyMMdd-HHmmssfff__프로젝트명__작업명` 형식으로 반환한 workflowId를 해당 그래프가 끝날 때까지 모든 heartbeat와 event에 그대로 사용하세요. 진행 중 계획의 순서나 범위가 바뀌면 같은 workflowId로 save_plan을 다시 호출해 노드를 추가·변경·정렬하세요. 기존 노드 상태는 유지되고 새 노드는 PENDING으로 시작합니다. SUCCESS 노드는 다시 열거나 변경하거나 새 하위 작업의 부모로 사용하지 마세요. 후속 작업은 형제 또는 새 최상위 nodeId로 추가하고 dependsOn으로 연결하세요. 완료된 그래프에는 새 작업을 추가하지 말고 새 그래프를 생성하세요. 계획 저장 직후 첫 실행 노드를 IN_PROGRESS로 기록하고, 노드 시작·구현 단계 완료·검증 시작·재시도·산출물 생성·완료처럼 상태, 진행률, 다음 작업이 바뀌는 즉시 record_event를 보내세요. 여러 변경을 작업 끝에 몰아서 보내지 마세요. 변경 없이 오래 실행할 때만 30초 이내 간격으로 record_heartbeat를 보내세요. dependsOn 선행 노드는 반드시 SUCCESS(자동 100%)로 완료한 뒤 후행 노드를 IN_PROGRESS로 시작하세요. 병렬 작업만 같은 시점에 IN_PROGRESS로 기록할 수 있습니다. 사용자가 이전 그래프를 불러오거나 이어 달라고 요청하면 list_workflows로 후보를 조회하고, 선택된 workflowId의 get_plan과 get_state를 읽은 뒤 그 ID를 그대로 사용하세요. 후보가 여러 개이고 선택 의도가 불명확하면 새 그래프를 만들지 말고 사용자에게 선택을 요청하세요. 관련 후속 요청은 완료 전까지 같은 그래프를 이어가고 모든 작업이 SUCCESS, FAILED 또는 BLOCKED가 되면 최종 record_event 후 heartbeat를 중단하세요. 셸 명령, 프롬프트 원문, 비밀과 개별 도구 호출은 기록하지 마세요.
        """;
}
