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

SMSR의 `서버 · 연결` 탭에서 `초기 연결`을 누른다. 앱은 현재 고정 실행 파일 경로로 아래와 같은 stdio MCP 등록을 한 번 수행하고, 배포 폴더에 포함한 로컬 플러그인 마켓플레이스도 등록한다.

```powershell
codex mcp add smsr -- <SMSR.App.exe의 고정 경로> --mcp-stdio
```

stdio 브리지는 토큰을 Codex 설정·환경 변수·플러그인 파일에 저장하지 않는다. 실행될 때만 현재 Windows 사용자의 DPAPI 토큰을 읽어 `http://127.0.0.1:49783/mcp`로 전달한다. 따라서 SQLite를 직접 쓰지 않고 기존 서버의 SSE 알림도 유지한다.

등록 뒤 Codex를 재시작하고 새 task에서 `/hooks`로 SMSR 훅을 검토·신뢰한다. 이 승인은 사용자만 할 수 있다. 신뢰 후 앱에서 `확인했고 계속`을 누르면 MCP·플러그인 등록 상태를 다시 확인한다. 실제 첫 이벤트가 수신되면 작업 현황과 웹 대시보드가 갱신된다.

앱을 완전 종료했다가 다시 열어도 SQLite 데이터와 마지막으로 선택한 프로젝트·워크플로우 ID는 `%LocalAppData%\SMSR`에 유지된다. 시작 후 서버를 다시 열고 저장된 진행도·최근 이벤트를 자동 복원한다.

이미 이름이 `smsr`인 MCP가 존재하면 앱은 덮어쓰지 않는다. 기존 설정을 사용자가 검토하거나 제거한 뒤 다시 초기 연결을 실행한다.

`plugins/smsr-codex`은 Codex용 훅·추적 지침 패키지다. 세션 시작 시 계획 기록에 사용할 `projectId`와 `workflowId`를 알려 주고, 사용자 요청 접수와 턴 종료만 자동 기록한다. 실제 계획 노드의 진행률은 Codex가 `save_plan`과 `record_event`로 갱신한다.

## 운영 점검

- 실제 대화에서 `save_plan` 후 `record_event`를 한 번 호출하고, 앱의 대시보드에서 제목·의존성·상태를 확인한다.
- 포트 충돌은 `Get-NetTCPConnection -LocalPort 49783`으로 확인한다. 충돌 프로세스를 종료한 뒤 앱을 다시 시작한다.
- 서버가 중지된 경우 stdio 브리지는 `SMSR 로컬 서버가 실행 중이 아닙니다.`를 반환하며 Codex 설정은 바꾸지 않는다.
