namespace SMSR.App.Mvp;

internal static class SmsrMcpInstructions
{
    public const string Text = """
        SMSR은 에이전트를 호출하지 않습니다. 메인 에이전트와 각 하위 에이전트가 자신의 계획 상태를 SMSR로 직접 전송하세요. projectId는 저장소 폴더명, workflowId는 현재 Codex task ID를 사용하고 알 수 없으면 task마다 고유 ID를 만드세요. 메인 에이전트는 계획 확정 시 save_plan을 호출하며 parentNodeId, assignedAgentId, agentRole, completionCriteria를 가능한 범위에서 채웁니다. 각 에이전트는 시작 시와 장기 작업 중 약 30초 간격으로 record_heartbeat를 직접 호출하고, 실제 노드 상태가 바뀔 때 record_event를 호출하여 progressPercentage, retryCount, nextAction, artifacts를 갱신합니다. eventType은 NODE_STATUS_CHANGED를 사용합니다. 셸 명령, 프롬프트 원문, 개별 도구 호출은 기록하지 마세요. 필요할 때 get_plan과 get_state로 반영 결과를 확인하세요.
        """;
}
