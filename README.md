# SMSR

WPF 기반의 로컬 Codex 작업 관제 앱입니다. Codex가 구조화된 계획과 노드 상태를 MCP로 기록하면, SMSR은 SQLite에 저장하고 WPF·웹 대시보드에서 진행률과 최근 이벤트를 표시합니다.

## 실행

```powershell
dotnet run --project src/SMSR.App/SMSR.App.csproj
```

서버는 외부에 노출하지 않고 `http://127.0.0.1:49783`에서만 실행됩니다. 앱에서 워크플로우를 선택한 뒤 `대시보드 열기`를 누르면 좌측 에이전트, 중앙 의존성 순서도, 우측 작업 상세로 구성된 웹 화면을 볼 수 있습니다.

## Codex 연결

1. 앱의 `서버 · 연결` 탭에서 `초기 연결`을 누른다.
2. 앱이 현재 Windows 사용자에게 설치된 `OpenAI.Codex` 데스크톱 패키지를 찾는다.
3. 공유 설정 `~/.codex/config.toml`에 `http://127.0.0.1:49783/mcp`를 OAuth HTTP MCP로 등록한다.
4. Codex를 완전히 재시작하고 MCP 설정에서 `smsr`의 `인증`을 누른다.
5. 브라우저에 표시되는 SMSR 승인 화면에서 `연결 승인`을 누른 뒤 `/mcp`에서 연결을 확인한다.

별도 Codex CLI, Node.js, npm은 필요하지 않습니다. SMSR은 `WindowsApps` 내부 실행 파일을 호출하지 않고 Codex가 공유하는 설정 파일을 직접 갱신합니다. 기존 설정 파일은 변경 전에 `config.toml.smsr.bak`으로 백업합니다.

등록되는 설정은 다음과 같습니다.

```toml
[mcp_servers.smsr]
url = "http://127.0.0.1:49783/mcp"
auth = "oauth"
```

SMSR은 OAuth DCR과 PKCE를 지원하며 Codex에 15분 액세스 토큰과 회전형 갱신 토큰을 발급합니다. 등록 클라이언트와 토큰 해시는 현재 Windows 사용자만 복호화할 수 있는 DPAPI 파일에 저장합니다. Codex 설정에는 토큰을 기록하지 않습니다. SMSR 서버가 실행 중이어야 인증과 MCP 연결이 가능합니다.

추적 규칙은 MCP `instructions`로 제공됩니다. SMSR은 에이전트를 호출하지 않으며, 메인·하위 에이전트가 `save_plan`, `record_heartbeat`, `record_event`로 자신의 계층 계획과 상태를 직접 전송할 때만 화면이 바뀝니다. `parentNodeId`가 있는 계획 노드는 웹 순서도에서 클릭해 하위 그래프로 드릴다운할 수 있습니다.

`--self-test`는 DPAPI 사용자 프로필이 로드된 일반 사용자 세션에서 실행해야 합니다. 조건이 맞지 않으면 앱이 충돌하지 않고 상세 오류를 표시합니다.

앱을 완전 종료했다가 다시 열어도 SQLite의 계획·이벤트는 유지됩니다. 마지막으로 보던 프로젝트·워크플로우를 복원하고 저장된 진행도와 최근 이벤트를 자동으로 표시합니다.

## 설정

`설정` 탭에서 앱 시작 시 서버 자동 시작, 닫기 버튼의 트레이 숨김 동작, 앱과 웹 대시보드의 밝은·어두운 테마를 선택할 수 있습니다. 테마는 즉시 반영됩니다. 설정은 현재 사용자의 `%LocalAppData%\SMSR\settings.json`에 저장되며 데이터·로그 폴더도 바로 열 수 있습니다.

OAuth 인증이 완료되면 `서버 · 연결` 탭의 초기 연결 버튼 영역은 자동으로 사라지고 `Codex 연결됨` 상태만 표시됩니다. 이 상태는 암호화된 갱신 토큰의 유효 여부를 기준으로 앱 재시작 후에도 복원됩니다.

포트 `49783`을 이미 사용하는지 확인하려면 `Get-NetTCPConnection -LocalPort 49783`을 실행합니다. 충돌한 프로세스를 종료한 뒤 SMSR을 다시 시작해야 하며, MCP 등록 주소와 같은 포트로 변경해야 합니다.

## 검증

```powershell
dotnet build SMSR.slnx --no-restore --verbosity:minimal
dotnet run --project src/SMSR.App/SMSR.App.csproj --no-build -- --tracking-self-test
dotnet run --project src/SMSR.App/SMSR.App.csproj --no-build -- --self-test
```

Self-check는 MCP `save_plan`·`record_event`·`record_heartbeat`·`record_lifecycle`, 계층 계획, `/api/plan`, `/api/state`, `/dashboard` 반영을 검사합니다.

## Documents

- [MCP 연결 및 이벤트 기록](docs/mcp-connection.md)
- [선택형 smsr-codex 플러그인](docs/smsr-codex-plugin.md)
- [개발 이력](docs/development-log.md)
- [WPF MCP 작업 관제 앱 계획서](docs/wpf-mcp-dashboard-project-plan.md)
- [WPF MCP 작업 관제 앱 HTML 계획서](docs/wpf-mcp-dashboard-project-plan.html)
