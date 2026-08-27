# MCP 연결 및 이벤트 기록

## 연결

1. SMSR 앱을 실행한다.
2. 창에 표시된 서버 주소에 `/mcp`를 붙인다. 예: `http://127.0.0.1:51234/mcp`
3. `접속 토큰 복사`를 누른다.
4. MCP 클라이언트에 endpoint와 `Authorization: Bearer <복사한 토큰>` 헤더를 설정한다.

토큰은 현재 Windows 사용자만 복호화할 수 있도록 DPAPI로 저장된다. 클라이언트의 보안 저장소 또는 환경 변수로 전달하고, 저장소·프롬프트·로그에는 넣지 않는다.

## 제공 도구

| 도구 | 입력 | 결과 |
|---|---|---|
| `record_event` | 이벤트 식별자, 프로젝트·워크플로우·노드·에이전트 ID, 상태 | 저장 결과와 중복 여부 |
| `get_state` | 프로젝트 ID, 워크플로우 ID | 각 노드의 최신 상태 |

`eventType`은 `NODE_STATUS_CHANGED`만 가능하며 `status`는 `PENDING`, `IN_PROGRESS`, `VALIDATING`, `SUCCESS`, `FAILED`, `RETRYING`, `BLOCKED` 중 하나여야 한다. 같은 `eventId`는 다시 저장하지 않고 `duplicate: true`를 반환한다.

## 호출 예제

MCP 클라이언트는 도구 목록·호출 요청을 자동으로 처리한다. 아래 PowerShell은 연결 문제를 확인할 때만 사용한다. 토큰 문자열 자체를 파일에 저장하지 않는다.

```powershell
$smsrEndpoint = 'http://127.0.0.1:51234/mcp'
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
