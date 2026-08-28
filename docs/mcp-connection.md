# MCP 연결 및 이벤트 기록

## Codex 직접 연결

1. SMSR 앱을 실행해 `http://127.0.0.1:49783` 서버가 실행 중인지 확인한다.
2. `서버 · 연결` 탭에서 `초기 연결`을 누른다.
3. Codex를 완전히 종료한 뒤 다시 실행한다.
4. Codex 설정의 MCP 서버 목록에서 `smsr`의 `인증`을 누른다.
5. 브라우저의 `SMSR MCP 연결 승인` 화면에서 `연결 승인`을 누른다.
6. 새 task에서 `/mcp`를 열어 `smsr` 도구가 표시되는지 확인한다.

SMSR은 별도 Codex CLI, Node.js, npm을 사용하지 않는다. 현재 사용자의 Codex 공유 설정 `~/.codex/config.toml`에는 다음 Streamable HTTP 항목만 등록한다.

```toml
[mcp_servers.smsr]
url = "http://127.0.0.1:49783/mcp"
auth = "oauth"
```

Codex는 인증되지 않은 `/mcp` 요청의 `401 WWW-Authenticate` 응답에서 OAuth 메타데이터를 찾는다. SMSR은 OAuth protected resource metadata, authorization server metadata, DCR, authorization code, PKCE S256, refresh token rotation을 제공한다. 승인 후 발급된 액세스 토큰으로만 MCP 요청을 처리한다.

서버는 `127.0.0.1`에만 bind한다. OAuth 상태 파일은 `%LocalAppData%\SMSR\oauth-state.bin`에 DPAPI로 암호화하며, 액세스·갱신 토큰 원문은 서버 저장소에 남기지 않고 SHA-256 해시만 보관한다. Codex가 받은 자격 증명은 Codex의 OAuth 저장소에서 관리한다.

## 제공 도구

| 도구 | 입력 | 결과 |
|---|---|---|
| `save_plan` | ID·제목·가중치·의존성, 부모 노드, 담당 에이전트·역할, 완료 조건 | 계층형 계획 그래프 저장 |
| `get_plan` | 프로젝트 ID, 워크플로우 ID | 계획 노드에 적용된 최신 상태 |
| `record_event` | 이벤트·노드·에이전트 ID, 상태, 진행률, 재시도, 다음 작업, 산출물 | 저장 결과와 중복 여부 |
| `record_heartbeat` | 프로젝트·워크플로우·에이전트 ID, 역할, 상태, 현재 노드 | 에이전트 최신 생존 상태 |
| `get_state` | 프로젝트 ID, 워크플로우 ID | 노드 및 에이전트 최신 상태 |
| `record_lifecycle` | 세션·경로·이벤트, 선택 에이전트 ID·역할 | 훅용 heartbeat 기록 |
| `generate_summary` | 프로젝트 ID, 워크플로우 ID | 로컬 상태 기반 요약 생성·저장 |
| `save_summary` | 프로젝트 ID, 워크플로우 ID, 요약 내용 | 외부 생성 요약 저장 |
| `export_workflow` | 프로젝트 ID, 워크플로우 ID | HTML·Markdown·JSON·ZIP 내보내기 |

`eventType`은 `NODE_STATUS_CHANGED`만 가능하며 `status`는 `PENDING`, `IN_PROGRESS`, `VALIDATING`, `SUCCESS`, `FAILED`, `RETRYING`, `BLOCKED` 중 하나여야 한다. 같은 `eventId`는 다시 저장하지 않고 `duplicate: true`를 반환한다.

SMSR은 에이전트를 능동 호출하거나 polling하지 않는다. 각 에이전트가 시작 시, 약 30초 이상 이어지는 작업 중, 종료 전에 `record_heartbeat`를 호출하고 의미 있는 상태 변화는 `record_event`로 전송한다. 활성 heartbeat가 90초 넘게 갱신되지 않으면 대시보드에서 `STALE`로 표시한다. WPF 화면은 이벤트 발생 시 SSE로 즉시 갱신하고 연결이 끊기면 2초 polling으로 전환하며, 웹 대시보드는 현재 URL을 2초마다 다시 읽는다.

## 운영 점검

- OAuth 검색 확인: `Invoke-RestMethod http://127.0.0.1:49783/.well-known/oauth-authorization-server`
- 보호 상태 확인: 인증 없이 `/mcp`를 요청하면 `401`과 `resource_metadata`가 포함된 `WWW-Authenticate` 헤더가 반환되어야 한다.
- 인증 버튼이 없다면 SMSR 서버 실행 상태와 `~/.codex/config.toml`의 `url`, `auth = "oauth"`를 확인한 뒤 Codex를 완전히 재시작한다.
- 실제 대화에서 `save_plan`과 `record_event`를 호출하고 SMSR 대시보드에서 계획·상태가 반영되는지 확인한다.
- 포트 충돌은 `Get-NetTCPConnection -LocalPort 49783`으로 확인한다.

SQLite 데이터와 마지막 선택 항목은 앱을 재시작해도 `%LocalAppData%\SMSR`에 유지된다. 기존 설정 파일은 등록 시 `config.toml.smsr.bak`으로 백업한다.

OAuth 토큰 발급이 완료되면 SMSR의 초기 연결·연결 확인 버튼은 숨겨지고 `Codex 연결됨` 상태로 전환된다. 갱신 토큰이 만료되거나 새 사용자 환경에서는 초기 연결 영역이 다시 표시된다.

## MCP 지침과 선택형 로컬 추적

SMSR MCP는 초기화 응답의 서버 지침으로 `save_plan`, `record_event`, `get_plan`, `get_state` 사용 규칙을 항상 제공한다. 따라서 기본 계획·상태 추적에는 별도 스킬이나 플러그인 설치가 필요하지 않다.

저장소의 `.codex/hooks.json`과 `.agents/skills/smsr-tracking`은 MCP 도구 기반의 선택형 저장소 로컬 기능이다. Node.js·npm·CLI 실행 훅이나 마켓플레이스 설치 과정은 없다. Codex에서 저장소를 신뢰하고 `/hooks`에서 정의를 검토·신뢰하면 세션·하위 에이전트 lifecycle 기록이 활성화된다. 상세 절차는 [SMSR Codex 로컬 추적](smsr-codex-local.md)을 따른다.
