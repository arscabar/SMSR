# MCP 연결 및 이벤트 기록

실제 요청 문구와 이전 그래프 재개 절차는 [요청형 그래프 사용 안내](graph-tracking-guide.md)를 따른다.

## Codex 직접 연결

1. SMSR을 실행한다. 앱이 서버·Windows 자동 시작·Codex 공유 MCP·사용자 전역 요청형 그래프 훅을 자동 구성한다.
2. 처음 등록한 환경이면 Codex를 다시 열고 OAuth 인증과 `~/.codex/hooks.json` 신뢰를 한 번 승인한다.
3. 이후 일반 작업은 기록하지 않고 계획 그래프는 사용자가 요청한 작업에만 적용된다.

SMSR은 별도 Codex CLI, Node.js, npm을 사용하지 않는다. 현재 사용자의 Codex 공유 설정 `~/.codex/config.toml`에는 다음 Streamable HTTP 항목만 등록한다.

```toml
[mcp_servers.smsr]
url = "http://127.0.0.1:49783/mcp"
auth = "oauth"
enabled = true
```

Codex는 인증되지 않은 `/mcp` 요청의 `401 WWW-Authenticate` 응답에서 OAuth 메타데이터를 찾는다. SMSR은 OAuth protected resource metadata, authorization server metadata, DCR, authorization code, PKCE S256, refresh token rotation을 제공한다. 승인 후 발급된 액세스 토큰으로만 MCP 요청을 처리한다.

서버는 `127.0.0.1`에만 bind한다. OAuth 상태 파일은 `%LocalAppData%\SMSR\oauth-state.bin`에 DPAPI로 암호화하며, 액세스·갱신 토큰 원문은 서버 저장소에 남기지 않고 SHA-256 해시만 보관한다. Codex가 받은 자격 증명은 Codex의 OAuth 저장소에서 관리한다.

## 제공 도구

| 도구 | 입력 | 결과 |
|---|---|---|
| `save_plan` | 프로젝트 ID, 선택형 workflow ID, 제목·가중치·의존성, 부모 노드, 담당 에이전트·역할, 완료 조건 | 새 그래프는 workflow ID 생략 시 `프로젝트명__날짜시간`으로 생성하고 계층형 계획 저장 |
| `get_plan` | 프로젝트 ID, 워크플로우 ID | 계획 노드에 적용된 최신 상태 |
| `list_workflows` | 프로젝트 ID | 기존 그래프를 최근 활동 순서와 상태로 조회 |
| `record_event` | 이벤트·노드·에이전트 ID, 상태, 진행률, 재시도, 다음 작업, 산출물 | 저장 결과와 중복 여부 |
| `record_heartbeat` | 프로젝트·워크플로우·에이전트 ID, 역할, 상태, 현재 노드 | 에이전트 최신 생존 상태 |
| `get_state` | 프로젝트 ID, 워크플로우 ID | 노드 및 에이전트 최신 상태 |
| `generate_summary` | 프로젝트 ID, 워크플로우 ID | 로컬 상태 기반 요약 생성·저장 |
| `save_summary` | 프로젝트 ID, 워크플로우 ID, 요약 내용 | 외부 생성 요약 저장 |
| `export_workflow` | 프로젝트 ID, 워크플로우 ID | HTML·Markdown·JSON·ZIP 내보내기 |

`eventType`은 `NODE_STATUS_CHANGED`만 가능하며 `status`는 `PENDING`, `IN_PROGRESS`, `VALIDATING`, `SUCCESS`, `FAILED`, `RETRYING`, `BLOCKED` 중 하나여야 한다. 같은 `eventId`는 다시 저장하지 않고 `duplicate: true`를 반환한다.

SMSR은 에이전트를 능동 호출하거나 polling하지 않는다. 그래프 추적이 요청된 동안 각 에이전트가 시작 시, 약 30초 이상 이어지는 작업 중, 종료 전에 `record_heartbeat`를 호출하고 의미 있는 상태 변화는 `record_event`로 전송한다. 활성 heartbeat가 90초 넘게 갱신되지 않으면 대시보드에서 `STALE`로 표시한다.

## 운영 점검

- OAuth 검색 확인: `Invoke-RestMethod http://127.0.0.1:49783/.well-known/oauth-authorization-server`
- 보호 상태 확인: 인증 없이 `/mcp`를 요청하면 `401`과 `resource_metadata`가 포함된 `WWW-Authenticate` 헤더가 반환되어야 한다.
- 인증 또는 그래프 추적 훅이 없다면 `연결·그래프 추적 설정 복구`를 눌러 설정을 일괄 복구한 뒤 Codex를 다시 연다.
- 실제 대화에서 `save_plan`과 `record_event`를 호출하고 SMSR 대시보드에서 계획·상태가 반영되는지 확인한다.
- 포트 충돌은 `Get-NetTCPConnection -LocalPort 49783`으로 확인한다.

SQLite 데이터와 마지막 선택 항목은 앱을 재시작해도 `%LocalAppData%\SMSR`에 유지된다. 기존 설정 파일은 등록 시 `config.toml.smsr.bak`으로 백업한다.

OAuth 토큰 발급만으로 연결 완료를 추정하지 않는다. 서버 시작 후 인증된 MCP 요청이 실제로 확인되면 설정 버튼이 숨겨지고 `Codex 연결됨 · 도구 9개` 상태로 전환된다.

자동 설정은 현재 SMSR 실행 파일 경로를 Windows 자동 시작과 전역 훅 명령에 등록한다. 다른 컴퓨터에서는 그 컴퓨터의 실제 경로로 자동 재생성된다. OAuth 동의와 비관리 훅의 최초 신뢰는 Codex 보안 경계라 앱이 대신 승인하지 않는다.

## MCP 지침과 요청형 그래프 추적

SMSR MCP는 초기화 응답에서 요청형 그래프 규칙을 제공하고, 앱이 병합한 사용자 전역 `UserPromptSubmit` 훅은 매 요청에 프로젝트 폴더명과 현재 Codex 세션 ID를 자동 주입하되 SMSR에는 저장하지 않는다. 위임 래퍼의 원본·부모 ID는 새 workflow ID로 사용하지 않는다. 에이전트는 사용자가 그래프 추적을 명시적으로 요청한 경우에만 계획·heartbeat·상태를 전송한다. 이전 그래프 요청에는 `list_workflows`, `get_plan`, `get_state`를 사용하며 요청 범위가 완료·실패·중단되면 최종 상태 후 추적을 끝낸다.

전역 훅은 기존 `~/.codex/hooks.json` 항목을 보존하고 SMSR 소유 항목만 병합한다. 저장소별 훅 복사, `$smsr-tracking` 지정, Node.js·npm·CLI 또는 마켓플레이스 설치는 필요 없다. `.agents/skills/smsr-tracking`은 에이전트가 자동으로 따를 세부 데이터 계약의 저장소 사본이다. 상세 동작은 [SMSR Codex 로컬 추적](smsr-codex-local.md)을 따른다.
