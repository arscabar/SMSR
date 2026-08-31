# SMSR

WPF 기반의 로컬 Codex 작업 관제 앱입니다. Codex가 구조화된 계획과 노드 상태를 MCP로 기록하면, SMSR은 SQLite에 저장하고 WPF·웹 대시보드에서 진행률과 최근 이벤트를 표시합니다.

## 실행

```powershell
dotnet run --project src/SMSR.App/SMSR.App.csproj
```

서버는 외부에 노출하지 않고 `http://127.0.0.1:49783`에서만 실행됩니다. 앱에서 워크플로우를 선택한 뒤 `대시보드 열기`를 누르면 좌측 에이전트, 중앙 의존성 순서도, 우측 작업 상세로 구성된 웹 화면을 볼 수 있습니다.

## Codex 연결

1. SMSR을 한 번 실행한다. 서버, Windows 자동 시작, Codex MCP와 전역 요청형 그래프 훅이 자동 구성된다.
2. 처음 등록한 환경에서는 Codex를 다시 열고 OAuth 인증과 전역 훅 신뢰를 한 번 승인한다.
3. 이후에는 그래프 추적을 명시적으로 요청한 작업만 계획·heartbeat·상태를 전송한다. 일반 작업은 SMSR에 기록하지 않는다.

`서버 · 연결` 탭의 `연결·그래프 추적 설정 복구` 버튼은 자동 설정이 실패했거나 실행 파일을 옮겼을 때만 사용한다. 작업 그래프는 사용자가 요청한 작업에만 생성되고 해당 작업이 끝날 때까지만 갱신된다.

별도 Codex CLI, Node.js, npm은 필요하지 않습니다. SMSR은 `WindowsApps` 내부 실행 파일을 호출하지 않고 Codex가 공유하는 설정 파일을 직접 갱신합니다. 기존 설정 파일은 변경 전에 `config.toml.smsr.bak`으로 백업합니다.

## 다른 Windows PC에 설치

다른 PC에는 자체 포함형 단일 설치 프로그램을 전달합니다.

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\build-installer.ps1
```

결과는 `artifacts\installer\SMSR-Setup-버전-win-x64.exe`에 생성됩니다. 설치 프로그램은 관리자 권한 없이 현재 사용자에게 설치하며 시작 메뉴, Windows 자동 시작과 제거 프로그램을 등록합니다. 설치된 SMSR을 처음 실행하면 해당 PC의 Codex 공유 MCP 설정과 전역 요청형 그래프 훅을 구성합니다. 대상 PC에는 .NET SDK가 필요하지 않습니다. 자세한 설치·업데이트·제거·무인 설치 방법은 [Windows 설치 프로그램 안내](docs/installer-quickstart.md)를 참고하세요.

설치 Wizard는 SMSR 브랜드 배너와 Windows 테마를 따르는 라이트·다크 화면을 제공합니다.

ZIP 휴대용 배포가 필요한 개발자는 [휴대용 배포 빠른 시작](docs/portable-quickstart.md)을 참고할 수 있지만, 일반 사용자 배포 기준은 설치 프로그램입니다.

등록되는 설정은 다음과 같습니다.

```toml
[mcp_servers.smsr]
url = "http://127.0.0.1:49783/mcp"
auth = "oauth"
enabled = true
```

SMSR은 OAuth DCR과 PKCE를 지원하며 Codex에 15분 액세스 토큰과 회전형 갱신 토큰을 발급합니다. 등록 클라이언트와 토큰 해시는 현재 Windows 사용자만 복호화할 수 있는 DPAPI 파일에 저장합니다. Codex 설정에는 토큰을 기록하지 않습니다. SMSR 서버가 실행 중이어야 인증과 MCP 연결이 가능합니다.

추적 규칙은 MCP `instructions`와 전역 `UserPromptSubmit` 훅으로 제공됩니다. 훅은 정확한 프로젝트·작업 ID만 개발자 컨텍스트에 추가하며 프롬프트 원문이나 활동 정보를 SMSR에 보내지 않습니다. SMSR은 에이전트를 호출하지 않고 그래프 요청 중인 메인·하위 에이전트만 `save_plan`, `record_heartbeat`, `record_event`로 상태를 직접 전송합니다. 이전 그래프는 `list_workflows`로 찾고 `get_plan`·`get_state`로 불러올 수 있습니다.

`--self-test`는 DPAPI 사용자 프로필이 로드된 일반 사용자 세션에서 실행해야 합니다. 조건이 맞지 않으면 앱이 충돌하지 않고 상세 오류를 표시합니다.

앱을 완전 종료했다가 다시 열어도 SQLite의 계획·이벤트는 유지됩니다. 마지막으로 보던 프로젝트·워크플로우를 복원하고 저장된 진행도와 최근 이벤트를 자동으로 표시합니다.

## 설정

`설정` 탭에서 Codex 연결·추적 자동 유지, Windows 로그인 시 SMSR 시작, SMSR 시작 시 서버 자동 시작, 트레이 동작과 테마를 선택할 수 있습니다. 자동 시작과 전역 훅 명령은 현재 실행 파일 경로로 생성되며 소스에 컴퓨터별 절대 경로를 하드코딩하지 않습니다. 설정은 현재 사용자의 `%LocalAppData%\SMSR\settings.json`에 저장됩니다.

시스템 트레이 아이콘을 우클릭하면 서버·Codex 연결 상태를 확인하고 SMSR 열기, 현재 선택된 대시보드 열기, 서버 시작·중지, 설정 열기와 완전 종료를 실행할 수 있습니다. 대시보드나 서버 명령은 현재 상태에 따라 사용할 수 있을 때만 활성화됩니다. 아이콘을 더블클릭하면 SMSR 창을 복원합니다.

OAuth 토큰 보유 여부만으로는 연결 완료로 표시하지 않습니다. SMSR 서버가 현재 실행된 뒤 인증된 MCP 요청을 실제로 한 번 받아야 설정 버튼이 사라지고 `Codex 연결됨 · 도구 9개`가 표시됩니다.

포트 `49783`을 이미 사용하는지 확인하려면 `Get-NetTCPConnection -LocalPort 49783`을 실행합니다. 충돌한 프로세스를 종료한 뒤 SMSR을 다시 시작해야 하며, MCP 등록 주소와 같은 포트로 변경해야 합니다.

## 검증

```powershell
dotnet build SMSR.slnx --no-restore --verbosity:minimal
dotnet run --project src/SMSR.App/SMSR.App.csproj --no-build -- --tracking-self-test
dotnet run --project src/SMSR.App/SMSR.App.csproj --no-build -- --self-test
```

Self-check는 MCP `save_plan`·`record_event`·`record_heartbeat`·`list_workflows`, 계층 계획, 이전 그래프 조회와 `/api/plan`, `/api/state`, `/dashboard` 반영을 검사합니다.

## Documents

- [요청형 그래프 사용 안내](docs/graph-tracking-guide.md)
- [Windows 설치 프로그램](docs/installer-quickstart.md)
- [MCP 연결 및 이벤트 기록](docs/mcp-connection.md)
- [선택형 SMSR Codex 로컬 추적](docs/smsr-codex-local.md)
- [개발 이력](docs/development-log.md)
- [WPF MCP 작업 관제 앱 계획서](docs/wpf-mcp-dashboard-project-plan.md)
- [WPF MCP 작업 관제 앱 HTML 계획서](docs/wpf-mcp-dashboard-project-plan.html)
