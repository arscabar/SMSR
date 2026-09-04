# MCP 연결 및 이벤트 기록

실제 기록 분류와 이전 그래프 재개 절차는 [일일 기록과 복잡 작업 그래프 안내](graph-tracking-guide.md)를 따른다.

## Codex 직접 연결

1. SMSR을 실행한다. 앱이 서버·Windows 자동 시작·Codex 공유 MCP·사용자 전역 기록 훅을 자동 구성한다.
2. 처음 등록하거나 기존 OAuth 연결에서 업데이트한 환경이면 Codex를 한 번 다시 열고 `~/.codex/hooks.json` 신뢰만 승인한다.
3. 이후 파일 변경이 없는 간단한 요청은 기록하지 않고, 작은 파일 변경은 일일 기록에만, 복잡한 변경은 설정 또는 명시 요청에 따라 그래프로 기록된다.

SMSR은 별도 Codex CLI, Node.js, npm을 사용하지 않는다. 현재 사용자의 Codex 공유 설정 `~/.codex/config.toml`에는 설치된 SMSR 실행 파일의 stdio 브리지를 등록한다.

```toml
[mcp_servers.smsr]
command = "<현재 PC의 SMSR.Bridge.exe 절대 경로>"
args = ["--mcp-stdio"]
startup_timeout_sec = 30
tool_timeout_sec = 60
enabled = true
```

Codex가 `SMSR.Bridge.exe --mcp-stdio`를 직접 실행하면 브리지가 12개 도구를 노출하고 내부 HTTP MCP로 요청을 전달한다. 서버가 꺼져 있으면 브리지가 같은 폴더의 `SMSR.App.exe`를 `--background --ensure-server`로 한 번만 실행한 뒤 준비를 기다린다. 설치·휴대용 실행 때 GUI 앱에서 현재 실행 파일의 콘솔 브리지 사본을 생성하므로 특정 PC 경로를 하드코딩하지 않는다. 서버와 브리지는 `%LocalAppData%\SMSR\mcp-bridge-token.bin`의 DPAPI 보호 토큰으로 상호 인증하며, 토큰 원문은 Codex 설정이나 로그에 기록하지 않는다. 브리지는 시작 즉시 보호된 연결 신호를 보내며 서버 준비 동안 최대 30초 재시도한다.

서버는 `127.0.0.1`에만 bind한다. 기존 Streamable HTTP 클라이언트와 진단을 위해 OAuth DCR·PKCE endpoint도 유지하지만 Codex 기본 연결은 이를 사용하지 않으므로 브라우저 인증이나 인증 화면을 먼저 만들기 위한 도구 호출이 필요 없다.

## 제공 도구

| 도구 | 입력 | 결과 |
|---|---|---|
| `save_plan` | 프로젝트 ID, 선택형 workflow ID, 제목·가중치·의존성, 부모 노드, 담당 에이전트·역할, 완료 조건 | 새 그래프는 ID 생략 시 `날짜시간__프로젝트명__작업명`으로 생성하며, 같은 작업의 재호출은 노드 순서·추가·변경을 반영 |
| `get_plan` | 프로젝트 ID, 워크플로우 ID | 계획 노드에 적용된 최신 상태 |
| `list_workflows` | 프로젝트 ID | 기존 그래프를 최근 활동 순서와 상태로 조회 |
| `record_daily_activity` | 작업·프로젝트·task ID, 제목·요약·상태, 변경 파일·검증·산출물, 선택형 workflow ID | 같은 activity ID의 날짜별 작업 카드를 생성 또는 갱신 |
| `record_event` | 이벤트·노드·에이전트 ID, 상태, 진행률, 재시도, 다음 작업, 산출물 | 저장 결과와 중복 여부 |
| `record_heartbeat` | 프로젝트·워크플로우·에이전트 ID, 역할, 상태, 현재 노드 | 에이전트 최신 생존 상태 |
| `get_state` | 프로젝트 ID, 워크플로우 ID | 노드 및 에이전트 최신 상태 |
| `generate_summary` | 프로젝트 ID, 워크플로우 ID | 로컬 상태 기반 요약 생성·저장 |
| `save_summary` | 프로젝트 ID, 워크플로우 ID, 요약 내용 | 외부 생성 요약 저장 |
| `export_workflow` | 프로젝트 ID, 워크플로우 ID | HTML·Markdown·JSON·ZIP 내보내기 |
| `get_daily_summary_request` | SMSR이 발급한 일자별 요약 요청 ID | 해당 날짜의 전체 프로젝트 작업 자료와 요약 형식 |
| `save_daily_summary_result` | 요청 ID, 완성된 한국어 요약 | 대기 중인 SMSR 요약 화면에 결과 반환 |

`eventType`은 `NODE_STATUS_CHANGED`만 가능하며 `status`는 `PENDING`, `IN_PROGRESS`, `VALIDATING`, `SUCCESS`, `FAILED`, `RETRYING`, `BLOCKED` 중 하나여야 한다. 같은 `eventId`는 다시 저장하지 않고 `duplicate: true`를 반환한다.

`record_event`와 `record_heartbeat`는 먼저 저장된 계획과 계획에 포함된 node ID가 있어야 한다. 새 그래프에서는 반드시 `save_plan`을 먼저 호출해야 하며, 이 순서를 어긴 이벤트는 저장하지 않는다. 따라서 Codex task UUID가 계획 제목 없는 독립 그래프로 먼저 생기는 것을 방지한다.

`dependsOn`으로 연결된 순차 작업은 모든 선행 노드가 `SUCCESS`가 된 뒤에만 후행 노드를 `IN_PROGRESS`, `VALIDATING`, `RETRYING`, `SUCCESS`로 기록할 수 있다. `SUCCESS`는 진행률 100%로 정규화되고 다시 진행 상태로 열 수 없다. 같은 요청의 후속 작업은 새 형제 또는 최상위 노드로 추가하고 기존 완료 노드와 `dependsOn`으로 연결한다. 모든 기존 노드가 완료된 그래프도 이 방식의 관련 후속 노드는 허용하며, 연결되지 않은 별도 작업은 새 workflow로 생성해야 한다. 계획을 재저장하면 같은 ID의 상태는 유지되고 새 노드는 `PENDING`으로 시작한다. 선행 작업이 끝나기 전에 전달된 후행 상태는 저장하지 않으며, 과거의 잘못된 병렬 상태도 대시보드에서는 `PENDING 0%`로 보정한다. 의존성이 없는 노드는 병렬 진행할 수 있다.

SMSR은 에이전트를 능동 호출하거나 polling하지 않는다. 그래프 추적이 요청된 동안 각 에이전트는 계획 직후 첫 노드 시작과 상태·진행률·검증·재시도·다음 작업·산출물 변경을 `record_event`로 즉시 전송한다. 의미 있는 변경 없이 작업이 계속될 때만 30초 이내 간격으로 `record_heartbeat`를 호출한다. 활성 heartbeat가 90초 넘게 갱신되지 않으면 대시보드에서 `STALE`로 표시한다. 수신된 이벤트는 별도 새로 고침 주기 없이 SSE로 즉시 앱과 브라우저에 전달된다.

## 운영 점검

- Codex 설정 확인: `~/.codex/config.toml`의 `mcp_servers.smsr`가 현재 설치 폴더의 `SMSR.Bridge.exe`와 `--mcp-stdio`를 가리켜야 한다.
- OAuth 검색 확인: `Invoke-RestMethod http://127.0.0.1:49783/.well-known/oauth-authorization-server`
- 보호 상태 확인: 브리지·OAuth 인증 없이 `/mcp`를 요청하면 `401`과 `resource_metadata`가 포함된 `WWW-Authenticate` 헤더가 반환되어야 한다.
- MCP 또는 그래프 추적 훅이 없다면 `연결·그래프 추적 설정 복구`를 눌러 설정을 일괄 복구한 뒤 Codex를 한 번 다시 연다.
- 실제 대화에서 `save_plan`과 `record_event`를 호출하고 SMSR 대시보드에서 계획·상태가 반영되는지 확인한다.
- 포트 충돌은 `Get-NetTCPConnection -LocalPort 49783`으로 확인한다.

SQLite 데이터와 마지막 선택 항목은 앱을 재시작해도 `%LocalAppData%\SMSR`에 유지된다. 기존 설정 파일은 등록 시 `config.toml.smsr.bak`으로 백업한다.

WPF 작업 현황의 통합 캘린더는 한 달짜리 날짜 그리드로 모든 프로젝트의 그래프와 일일 기록을 마지막 수신 날짜별로 조회한다. 날짜를 고르면 복잡 작업 그래프, 프로젝트별 작업 카드, 변경 파일과 검증 결과를 함께 볼 수 있다. 별도 `작업 요약` 탭은 금일 또는 그리드 선택일자의 모든 프로젝트 기록을 Gemini로 요약한다. Gemini 키가 없거나 호출이 실패하면 `codex://threads/new?prompt=...`로 요청이 채워진 새 Codex 작업을 열며, 사용자가 전송을 누른 뒤 Codex가 위 두 요약 도구로 결과를 돌려준다. Codex Desktop 딥링크는 보안상 프롬프트를 자동 전송하지 않는다. `기록 관리`에서 워크플로우·프로젝트·전체 이력을 삭제하면 SQLite 일일 기록과 이벤트, 활동 JSONL, 자동추적 세션 매핑을 함께 정리한다. 이미 내보낸 ZIP/HTML과 앱 설정은 보존한다.

같은 Windows 사용자에서는 재부팅 후 Windows 자동 시작, stdio 명령, DPAPI 브리지 토큰이 그대로 복원된다. 다른 PC에서는 설치된 실행 파일을 한 번 시작하면 그 PC의 실제 경로와 새 DPAPI 토큰을 자동 생성하므로 설정을 복사하거나 인증할 필요가 없다. Windows 자동 시작이 누락되거나 SMSR을 완전히 종료했더라도 다음 Codex 시작 시 stdio 브리지가 대시보드 본체와 서버를 자동 복구한다.

설정 파일 존재만으로 연결 완료를 추정하지 않는다. Codex가 stdio 브리지를 시작해 보호된 연결 신호를 보내면 설정 버튼이 숨겨지고 `Codex 연결됨 · 도구 12개` 상태로 전환된다. 확인을 위한 별도 에이전트 도구 호출은 필요 없다.

자동 설정은 현재 설치 폴더의 콘솔 브리지를 MCP stdio와 전역 훅 명령에, GUI 앱을 Windows 자동 시작에 등록한다. 다른 컴퓨터에서는 그 컴퓨터의 실제 경로로 자동 재생성된다. 비관리 훅의 최초 신뢰는 Codex 보안 경계라 앱이 대신 승인하지 않는다.

## MCP 지침과 작업 분류

SMSR MCP는 초기화 응답에서 기록 분류 규칙을 제공한다. 앱이 병합한 사용자 전역 훅은 `UserPromptSubmit`, `PostToolUse`, `SessionStart/End`, `SubagentStart/Stop`, `Stop`을 처리한다. `UserPromptSubmit`은 프로젝트 폴더명과 현재 Codex 세션 ID를 자동 주입하고, 나머지 훅은 활성 그래프가 있을 때만 정규화된 활동 JSONL을 기록한다. 위임 래퍼의 원본·부모 ID는 새 workflow ID로 사용하지 않는다. 에이전트는 실제 파일 변경을 `record_daily_activity`로 요약한다. 설정이 켜진 복잡 작업 또는 사용자의 명시 요청에만 계획·heartbeat·상태를 추가 전송하며, 요청 범위가 완료·실패·중단되면 최종 상태 후 추적을 끝낸다. 이전 그래프 요청에는 `list_workflows`, `get_plan`, `get_state`를 사용한다.

전역 훅은 기존 `~/.codex/hooks.json` 항목을 보존하고 SMSR 소유 항목만 병합한다. 훅 정의 해시를 최초 한 번 신뢰하면 같은 PC·Windows 사용자에서는 재부팅 후에도 유지되며, SMSR 업데이트로 훅 정의가 바뀔 때만 다시 검토한다. 저장소별 훅 복사, `$smsr-tracking` 지정, Node.js·npm·CLI 또는 마켓플레이스 설치는 필요 없다. `.agents/skills/smsr-tracking`은 에이전트가 자동으로 따를 세부 데이터 계약의 저장소 사본이다. 상세 동작은 [SMSR Codex 로컬 추적](smsr-codex-local.md)을 따른다.

실시간 활동은 `%LOCALAPPDATA%\SMSR\activity` 아래 워크플로우별 해시 파일에 JSONL로 추가되고 5MB에서 한 세대 회전한다. 내보내기 ZIP에는 읽기 쉬운 이름 `activity.jsonl`로 포함된다. 훅의 로컬 POST는 DPAPI로 보호된 전용 토큰을 사용하며 서버가 꺼져 있으면 파일에 직접 안전하게 추가한 뒤 다음 서버 시작에서 읽는다.
