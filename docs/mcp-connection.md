# MCP 연결 및 이벤트 기록

## 연결

1. SMSR 앱을 실행한다.
2. 고정된 로컬 주소 `http://127.0.0.1:49783/mcp`를 사용한다.
3. `접속 토큰 복사`를 누른다.
4. MCP 클라이언트에 endpoint와 `Authorization: Bearer <복사한 토큰>` 헤더를 설정한다.

토큰은 현재 Windows 사용자만 복호화할 수 있도록 DPAPI로 저장된다. 클라이언트의 보안 저장소 또는 환경 변수로 전달하고, 저장소·프롬프트·로그에는 넣지 않는다.

## 제공 도구

| 도구 | 입력 | 결과 |
|---|---|---|
| `save_plan` | 프로젝트·워크플로우 ID, 노드 ID·제목·가중치·의존성 | 계획 그래프 저장 |
| `get_plan` | 프로젝트 ID, 워크플로우 ID | 계획 노드에 적용된 최신 상태 |
| `record_event` | 이벤트 식별자, 프로젝트·워크플로우·노드·에이전트 ID, 상태 | 저장 결과와 중복 여부 |
| `get_state` | 프로젝트 ID, 워크플로우 ID | 각 노드의 최신 상태 |
| `record_lifecycle` | 세션 ID, 작업 경로, 이벤트 이름 | Codex 훅 전용 최소 활동 기록 |

`eventType`은 `NODE_STATUS_CHANGED`만 가능하며 `status`는 `PENDING`, `IN_PROGRESS`, `VALIDATING`, `SUCCESS`, `FAILED`, `RETRYING`, `BLOCKED` 중 하나여야 한다. 같은 `eventId`는 다시 저장하지 않고 `duplicate: true`를 반환한다.

## 호출 예제

MCP 클라이언트는 도구 목록·호출 요청을 자동으로 처리한다. 아래 PowerShell은 연결 문제를 확인할 때만 사용한다. 토큰 문자열 자체를 파일에 저장하지 않는다.

```powershell
$smsrEndpoint = 'http://127.0.0.1:49783/mcp'
$smsrToken = '<앱에서 복사한 토큰>'
$smsrHeaders = @{
  Authorization = "Bearer $smsrToken"
  Accept = 'application/json, text/event-stream'
  'MCP-Protocol-Version' = '2026-07-28'
  'MCP-Method' = 'tools/call'
  'MCP-Name' = 'record_event'
}
$smsrBody = @{
  jsonrpc = '2.0'
  id = 1
  method = 'tools/call'
  params = @{
    name = 'record_event'
    arguments = @{
      eventId = 'evt-example-001'
      projectId = 'sample-project'
      workflowId = 'wf-001'
      nodeId = 'STEP_01'
      agentId = 'WORKER_01'
      eventType = 'NODE_STATUS_CHANGED'
      status = 'IN_PROGRESS'
      summary = '작업을 시작했습니다.'
    }
    _meta = @{
      'io.modelcontextprotocol/protocolVersion' = '2026-07-28'
      'io.modelcontextprotocol/clientInfo' = @{ name = 'smsr-manual-check'; version = '1.0' }
      'io.modelcontextprotocol/clientCapabilities' = @{}
    }
  }
} | ConvertTo-Json -Depth 8
Invoke-WebRequest -Method Post -Uri $smsrEndpoint -Headers $smsrHeaders -ContentType 'application/json' -Body $smsrBody
```

기록 후 앱에서 `sample-project`와 `wf-001`을 입력해 대시보드를 열면 최신 상태를 확인할 수 있다.

## Codex 연결

SMSR 앱을 실행한 뒤 토큰을 현재 PowerShell 세션의 환경 변수에만 넣고 연결한다. 토큰을 저장소나 플러그인 파일에 쓰지 않는다.

```powershell
$env:SMSR_MCP_TOKEN = '<앱에서 복사한 토큰>'
codex mcp add smsr --url http://127.0.0.1:49783/mcp --bearer-token-env-var SMSR_MCP_TOKEN
codex plugin marketplace add D:\Gitsource\개인\SMSR
codex plugin add smsr-codex@personal
```

`.agents/plugins/marketplace.json`은 저장소의 로컬 플러그인 마켓플레이스이며 `plugins/smsr-codex`을 가리킨다. 설치 후 새 Codex task에서 `/hooks`로 훅을 검토·신뢰한다.

`plugins/smsr-codex`은 Codex용 훅·추적 지침 패키지다. 세션 시작 시 계획 기록에 사용할 `projectId`와 `workflowId`를 알려 주고, 사용자 요청 접수와 턴 종료만 자동 기록한다. 실제 계획 노드의 진행률은 Codex가 `save_plan`과 `record_event`로 갱신한다.

## 운영 점검

- 실제 대화에서 `save_plan` 후 `record_event`를 한 번 호출하고, 앱의 대시보드에서 제목·의존성·상태를 확인한다.
- 포트 충돌은 `Get-NetTCPConnection -LocalPort 49783`으로 확인한다. 충돌 프로세스를 종료한 뒤 앱을 다시 시작한다.
- 토큰이 설정되지 않았거나 서버가 중지된 경우 훅은 Codex 작업을 막지 않으며, SMSR 기록만 생략된다.
