# SMSR

WPF 기반의 로컬 Codex 작업 관제 앱입니다. Codex가 구조화된 계획과 노드 상태를 MCP로 기록하면, SMSR은 SQLite에 저장하고 WPF·웹 대시보드에서 진행률과 최근 이벤트를 표시합니다.

## 실행

```powershell
dotnet run --project src/SMSR.App/SMSR.App.csproj
```

서버는 외부에 노출하지 않고 `http://127.0.0.1:49783`에서만 실행됩니다. 앱에서 워크플로우를 선택한 뒤 `대시보드 열기`를 누르면 웹 화면을 볼 수 있습니다.

## Codex 연결

1. 앱의 `서버 · 연결` 탭에서 `초기 연결`을 누른다.
2. 앱이 고정 실행 파일 경로의 stdio MCP와 `smsr-codex` 플러그인을 한 번 등록한다.
3. Codex를 재시작하고 `/hooks`에서 SMSR 훅을 검토·신뢰한다.
4. 앱에서 `확인했고 계속`을 누르고 새 Codex task를 시작한다.

MCP 등록에는 토큰이 저장되지 않습니다. Codex가 stdio 브리지를 실행하면 브리지는 현재 Windows 사용자의 DPAPI 토큰으로 `127.0.0.1` 서버에만 전달합니다. 플러그인 훅은 사용자 요청과 턴 종료만 기록하며, 계획 진행률은 Codex가 `save_plan`과 `record_event`를 호출할 때만 바뀝니다.

앱을 완전 종료했다가 다시 열어도 SQLite의 계획·이벤트는 유지됩니다. 마지막으로 보던 프로젝트·워크플로우를 복원하고 저장된 진행도와 최근 이벤트를 자동으로 표시합니다.

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
