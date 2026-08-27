# WPF MCP 작업 관제 앱 개발 계획서

## 1. 프로젝트 범위와 목표

Windows 전용 로컬 앱으로, AI 에이전트가 MCP를 통해 기록한 작업 이벤트를 SQLite에 저장하고 기본 브라우저의 대시보드에서 프로젝트별 진행 상태를 확인한다.

### 목표

- WPF + WPF-UI 기반의 가벼운 Windows 앱 제공
- 앱에서 로컬 웹서버 시작/중지와 상태 확인
- 에이전트는 MCP로 작업 이벤트만 기록
- 앱은 프로젝트, 워크플로우, 이벤트, 요약, export를 관리
- 대시보드는 Edge/Chrome 같은 기본 브라우저에서 표시
- 앱 내 LLM은 선택 기능으로 제공하고, 미설정 시 호출 에이전트가 요약

### Out of Scope

- 원격 접속
- 클라우드 동기화
- 다중 사용자 권한 관리
- 에이전트 실행/중단 제어
- WebView 내장 대시보드
- WebSocket 양방향 제어

## 2. 확정된 제품 방향

| 항목 | 결정 |
|---|---|
| OS | Windows 전용 |
| 앱 UI | WPF + WPF-UI |
| 앱 역할 | 서버/프로젝트/대시보드 관리 |
| 대시보드 | 기본 브라우저에서 열기 |
| 서버 | 앱이 관리하는 localhost 웹서버 |
| 저장소 | SQLite |
| 실시간 갱신 | SSE, 실패 시 polling fallback |
| MCP 역할 | 기록 입력 및 상태 조회 |
| 요약 | 앱 LLM 옵션 또는 호출 에이전트 fallback |
| Export | HTML, Markdown, JSON, ZIP |

## 3. 시스템 구조

```text
WPF App
  - WPF-UI 화면
  - 트레이 상태 표시
  - 서버 시작/중지
  - 프로젝트 목록 관리
  - 기본 브라우저로 대시보드 열기

Local Web/MCP Server
  - MCP tool endpoint
  - HTTP dashboard endpoint
  - SSE stream
  - SQLite read/write
  - export 생성

Agents
  - record_event 호출
  - get_summary_context 호출
  - 필요 시 save_summary 호출

Browser Dashboard
  - project state 조회
  - SSE 구독
  - 작업 그래프/이벤트/요약 표시
```
## 4. 주요 기능

### WPF 앱

- 서버 실행 상태 표시
- 포트 표시 및 충돌 시 자동 증가
- 프로젝트 목록 표시
- 프로젝트 선택 시 기본 브라우저로 대시보드 열기
- 로그 폴더 열기
- DB 위치 열기
- 설정 화면 제공
- LLM 요약 옵션 관리

### 로컬 서버

- localhost 전용 bind
- MCP tool 처리
- SQLite 저장
- 대시보드 정적 파일 제공
- 프로젝트별 state API 제공
- SSE 제공
- export 파일 생성

### 대시보드

- 프로젝트 목록
- 워크플로우 진행률
- 활성 에이전트
- 작업 그래프
- 노드 상세
- 이벤트 타임라인
- 요약 표시
- export 버튼

## 5. MCP Tool 설계

### 필수

```text
record_event
get_state
get_summary_context
save_summary
export_workflow
```

### 선택

```text
list_projects
list_workflows
list_events
create_project
init_workflow
```

### record_event 입력

```json
{
  "event_id": "evt_unique",
  "project_id": "mqtt_update",
  "workflow_id": "wf_001",
  "node_id": "STEP_02",
  "agent_id": "WORKER_01",
  "event_type": "NODE_STATUS_CHANGED",
  "status": "IN_PROGRESS",
  "summary": "로그인 API 구현을 시작했습니다.",
  "commands": ["dotnet test"],
  "artifacts": ["src/AuthService.cs"],
  "error": null
}
```

## 6. 데이터 모델

```text
projects
- id
- name
- root_path
- created_at
- updated_at

workflows
- id
- project_id
- title
- status
- created_at
- updated_at

nodes
- id
- workflow_id
- parent_id
- title
- status
- agent_id
- progress
- retry_count
- required
- updated_at

agents
- id
- workflow_id
- role
- status
- active_node_id
- last_seen_at

events
- id
- project_id
- workflow_id
- node_id
- agent_id
- event_type
- payload_json
- created_at

summaries
- id
- project_id
- workflow_id
- source_last_event_id
- content_markdown
- generated_by
- created_at
```

## 7. 요약 처리

앱 설정에서 LLM 요약을 켠 경우 앱이 요약을 생성한다. API 키가 없거나 옵션이 꺼져 있으면 MCP가 요약 재료를 반환하고 호출한 에이전트가 요약 후 save_summary로 저장한다.

```text
if app llm enabled:
  generate summary in app
else:
  return summary context
  caller agent writes summary
```

요약에는 raw thought를 저장하지 않는다. decision, reason_summary, evidence, next_action만 저장한다.

## 8. Export

지원 포맷:

- HTML
- Markdown
- JSON
- ZIP

ZIP 구성:

```text
dashboard.html
workflow-state.json
events.jsonl
summary.md
artifacts-index.json
```

## 9. 보안 규칙

- 서버는 127.0.0.1에만 bind
- local token으로 MCP 호출 검증
- API key는 Windows DPAPI 또는 Credential Manager에 저장
- artifact 경로는 프로젝트 root 밖으로 나갈 수 없음
- HTML 렌더링은 textContent 또는 escaping 사용
- 이벤트 payload는 신뢰하지 않는 입력으로 처리

## 10. 개발 일정

### Phase 1: MVP 서버와 DB

- SQLite schema 생성
- record_event 구현
- get_state 구현
- state reducer 구현
- 기본 dashboard 제공

완료 기준: MCP 호출 후 브라우저 대시보드에 상태가 표시된다.

### Phase 2: WPF 앱

- WPF-UI shell 구성
- 서버 start/stop
- 프로젝트 목록
- 기본 브라우저 열기
- 설정 화면

완료 기준: 앱에서 서버와 프로젝트 대시보드를 관리할 수 있다.

### Phase 3: 실시간과 요약

- SSE stream
- get_summary_context
- save_summary
- 앱 LLM 옵션
- summary stale 표시

완료 기준: 이벤트 발생 시 대시보드가 갱신되고 요약이 저장/표시된다.

### Phase 4: Export와 안정화

- export_workflow
- ZIP export
- 로그 rotation
- 서버 crash 복구
- installer 생성

완료 기준: 프로젝트 기록을 독립 파일로 내보낼 수 있다.

## 11. QA 계획

- record_event 중복 호출 시 event_id 기준으로 중복 저장되지 않는지 확인
- 여러 에이전트 동시 호출 시 SQLite 쓰기 충돌이 없는지 확인
- 서버 재시작 후 events에서 state가 복구되는지 확인
- 대시보드 SSE 끊김 시 polling fallback이 동작하는지 확인
- export 결과에 summary, state, events가 포함되는지 확인
- artifact path traversal이 차단되는지 확인

## 12. 작업 분할

| 작업 | 산출물 | 선행 조건 | 완료 기준 |
|---|---|---|---|
| 요구사항 고정 | 기능 범위 문서 | 없음 | 범위와 제외 범위 확정 |
| DB 설계 | SQLite schema | 요구사항 | 마이그레이션 실행 가능 |
| MCP 서버 | record_event/get_state | DB 설계 | 이벤트 기록과 조회 가능 |
| State reducer | workflow-state 생성 | MCP 서버 | 이벤트 기반 상태 계산 |
| Dashboard | HTML/JS UI | State API | 브라우저에서 상태 표시 |
| WPF 앱 | WPF-UI shell | 서버 | 서버 start/stop 가능 |
| 프로젝트 관리 | 프로젝트 목록/열기 | WPF 앱 | 브라우저 대시보드 열림 |
| 요약 기능 | context/save_summary | 이벤트 저장 | 요약 표시 가능 |
| Export | HTML/MD/JSON/ZIP | state/summary | 파일 생성 가능 |
| 패키징 | installer | 전체 기능 | Windows 설치 가능 |

## 13. 기술 기본값

```text
.NET 8
WPF
WPF-UI
ASP.NET Core Kestrel
SQLite
SSE
Windows DPAPI
Markdown export
Static HTML dashboard
```
