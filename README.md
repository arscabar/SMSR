# SMSR

WPF 기반의 로컬 Codex 작업 관제 앱입니다. Codex가 구조화된 계획과 노드 상태를 MCP로 기록하면, SMSR은 SQLite에 저장하고 WPF·웹 대시보드에서 진행률과 최근 이벤트를 표시합니다.

## 실행

```powershell
dotnet run --project src/SMSR.App/SMSR.App.csproj
```

서버는 외부에 노출하지 않고 `http://127.0.0.1:49783`에서만 실행됩니다. 앱에서 워크플로우를 선택한 뒤 `대시보드 열기`를 누르면 웹 화면을 볼 수 있습니다.

## Codex 연결

1. SMSR 앱을 실행하고 `접속 토큰 복사`를 누른다.
2. 토큰을 현재 PowerShell 세션에만 설정한 뒤 MCP 서버를 등록한다.
3. 저장소의 로컬 마켓플레이스에서 `smsr-codex` 플러그인을 설치한다.
4. 새 Codex task에서 훅을 신뢰하고, 계획 생성·노드 상태 변경을 실행한다.

```powershell
$env:SMSR_MCP_TOKEN = '<앱에서 복사한 토큰>'
codex mcp add smsr --url http://127.0.0.1:49783/mcp --bearer-token-env-var SMSR_MCP_TOKEN
codex plugin marketplace add D:\Gitsource\개인\SMSR
codex plugin add smsr-codex@personal
```

토큰은 DPAPI로 보관되며, 저장소·플러그인·로그에는 기록하지 않습니다. 플러그인 훅은 사용자 요청과 턴 종료만 기록합니다. 계획 진행률은 Codex가 `save_plan`과 `record_event`를 호출할 때만 바뀝니다.

포트 `49783`을 이미 사용하는지 확인하려면 `Get-NetTCPConnection -LocalPort 49783`을 실행합니다. 충돌한 프로세스를 종료한 뒤 SMSR을 다시 시작해야 하며, MCP 등록 주소와 같은 포트로 변경해야 합니다.

## 검증

```powershell
dotnet build SMSR.slnx --no-restore --verbosity:minimal
dotnet run --project src/SMSR.App/SMSR.App.csproj --no-build -- --self-test
```

Self-check는 MCP `save_plan`·`record_event`·`record_lifecycle`, `/api/plan`, `/api/state`, `/dashboard` 반영을 검사합니다.

## Documents

- [MCP 연결 및 이벤트 기록](docs/mcp-connection.md)
- [개발 이력](docs/development-log.md)
- [WPF MCP 작업 관제 앱 계획서](docs/wpf-mcp-dashboard-project-plan.md)
- [WPF MCP 작업 관제 앱 HTML 계획서](docs/wpf-mcp-dashboard-project-plan.html)
