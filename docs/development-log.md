## 2026-08-27 - WPF MCP 작업 관제 앱 계획 반영

- 변경 파일:
  - `AGENTS.md`
  - `README.md`
  - `docs/development-log.md`
  - `docs/wpf-mcp-dashboard-project-plan.md`
  - `docs/wpf-mcp-dashboard-project-plan.html`
  - `prompts/workflow-graph-multi-agent-instructions.md`
  - `samples/dashboard-sample.html`
- 변경 사유:
  - WPF 기반 로컬 MCP 작업 관제 앱 계획과 원본 지시 자료를 저장소에 보존하고, 이후 개발 작업 기준을 명시하기 위해 추가했다.
- 실행 명령:
  - `multica issue get 01a040fc-db04-78b7-b3f7-6bc952dab5a8 --output json`
  - `multica issue comment list 01a040fc-db04-78b7-b3f7-6bc952dab5a8 --roots-only --summary --compact --output json`
  - `multica repo checkout https://github.com/arscabar/SMSR.git`
  - `git clone https://github.com/arscabar/SMSR.git SMSR`
  - `multica attachment download <attachment-id> -o ./attachments`
  - `git diff --check`
  - `git status --short`
  - `git commit -m "MQTT-1: add WPF MCP dashboard planning docs"`
  - `git push origin HEAD:refs/heads/mika/MQTT-1-wpf-mcp-dashboard-docs`
  - `gh pr create --repo arscabar/SMSR --base main --head mika/MQTT-1-wpf-mcp-dashboard-docs ...`
- 검증 결과:
  - 저장소가 비어 있어 기존 문서/프롬프트 패턴은 없었다.
  - 첨부 파일 4개를 기본 위치에 추가했다.
  - `git diff --check` 통과.
  - 변경 브랜치 `mika/MQTT-1-wpf-mcp-dashboard-docs`를 원격 저장소에 push했다.
- 남은 위험:
  - 아직 애플리케이션 코드가 없어 빌드/테스트 명령을 실행할 대상이 없다.
  - GitHub CLI 인증이 없어 PR 생성 명령이 실패했다.
  - 원격 저장소가 비어 있어 `main` 기준 브랜치가 아직 없다.
- 다음 조치:
  - GitHub CLI 인증과 기준 브랜치를 준비한 뒤 `mika/MQTT-1-wpf-mcp-dashboard-docs` 브랜치에서 PR을 생성한다.

## 2026-08-27 - MVP 범위 확정

- 변경 파일:
  - `docs/wpf-mcp-dashboard-project-plan.md`
  - `docs/development-log.md`
- 변경 사유:
  - 단계적 구현을 위해 SQLite 단일 원본, 최소 MCP 도구, polling 대시보드 범위를 확정하고 문서의 병합 표식을 제거했다.
- 실행 명령:
  - `rg -n '^(<<<<<<<|=======|>>>>>>>)' -g '!bin' -g '!obj'`
  - `dotnet build SMSR.slnx --no-restore --verbosity:minimal`
- 검증 결과:
  - 병합 표식이 계획서에서 제거되었다.
  - `dotnet build SMSR.slnx --no-restore --verbosity:minimal` 통과(경고 0, 오류 0).
- 남은 위험:
  - local token의 발급·전달 방식과 MCP 전송 규약은 서버 구현 전에 구체화해야 한다.
- 다음 조치:
  - SQLite 스키마와 `record_event`/`get_state`의 입출력 계약을 정의한다.

## 2026-08-27 - 단계적 개발계획 구체화

- 변경 파일:
  - `docs/wpf-mcp-dashboard-project-plan.md`
  - `docs/wpf-mcp-dashboard-project-plan.html`
  - `docs/development-log.md`
- 변경 사유:
  - 1인 4주 MVP 가정을 명시하고, 일정·역할·자원·리스크·QA·커뮤니케이션·에이전트 작업 분할을 실행 가능한 기준으로 보강했다.
- 실행 명령:
  - `rg -n '^(<<<<<<<|=======|>>>>>>>)' -g '!bin' -g '!obj'`
  - `dotnet build SMSR.slnx --no-restore --verbosity:minimal`
- 검증 결과:
  - Markdown과 HTML 계획서에 동일한 MVP 범위와 4주 실행 계획을 반영했다.
  - 계획서에 병합 표식이 없고 애플리케이션 빌드가 통과했다.
- 남은 위험:
  - 실제 MCP 클라이언트 연결 규약과 local token 발급·전달 방식은 1주차 계약 작업에서 확정해야 한다.
- 다음 조치:
  - `record_event`와 `get_state`의 JSON 계약 및 SQLite schema를 작성한다.

## 2026-08-27 - MVP 이벤트 저장·상태 조회 기반 구현

- 변경 파일:
  - `src/SMSR.App/SMSR.App.csproj`
  - `src/SMSR.App/App.xaml`
  - `src/SMSR.App/App.xaml.cs`
  - `src/SMSR.App/MainWindow.xaml`
  - `src/SMSR.App/MainWindow.xaml.cs`
  - `src/SMSR.App/Mvp/Contracts.cs`
  - `src/SMSR.App/Mvp/EventStore.cs`
  - `src/SMSR.App/Mvp/EventStoreStateQueries.cs`
  - `src/SMSR.App/Mvp/EventStoreCatalog.cs`
  - `src/SMSR.App/Mvp/LocalServer.cs`
  - `src/SMSR.App/Mvp/LocalServerEndpoints.cs`
  - `src/SMSR.App/Mvp/LocalTokenStore.cs`
  - `src/SMSR.App/Mvp/MvpSelfCheck.cs`
  - `docs/wpf-mcp-dashboard-project-plan.md`
  - `docs/wpf-mcp-dashboard-project-plan.html`
  - `docs/development-log.md`
- 변경 사유:
  - 주간 계획을 제거하고, SQLite 이벤트 저장·상태 조회·127.0.0.1 도구 API·DPAPI 토큰 보관을 실제 구현했다.
- 실행 명령:
  - `dotnet build SMSR.slnx --verbosity:minimal`
  - `dotnet run --project src/SMSR.App/SMSR.App.csproj --no-build -- --self-test`
- 검증 결과:
  - 빌드가 경고 0, 오류 0으로 통과했다.
  - self-check가 event_id 중복 무시, 최신 노드 상태 계산, 잘못된 상태 거부, bearer token 비교를 확인했다.
  - 계획서에서 주차 기반 일정 표를 제거했다.
- 남은 위험:
  - 현재는 표준 MCP Streamable HTTP 전송 전의 로컬 HTTP 도구 계약이다. 외부 MCP 클라이언트 연결 전에는 공식 MCP 전송 어댑터와 인증 미들웨어를 추가해야 한다.
- 다음 조치:
  - 표준 Streamable HTTP MCP 어댑터를 `record_event`와 `get_state` 계약에 연결하고, 브라우저 상태 대시보드를 추가한다.

## 2026-08-27 - 표준 MCP Streamable HTTP 연결

- 변경 파일:
  - `src/SMSR.App/SMSR.App.csproj`
  - `src/SMSR.App/MainWindow.xaml`
  - `src/SMSR.App/Mvp/LocalServer.cs`
  - `src/SMSR.App/Mvp/MvpSelfCheck.cs`
  - `src/SMSR.App/Mvp/WorkflowTools.cs`
  - `docs/wpf-mcp-dashboard-project-plan.md`
  - `docs/wpf-mcp-dashboard-project-plan.html`
  - `docs/development-log.md`
- 변경 사유:
  - 로컬 HTTP 계약을 공식 MCP SDK의 `/mcp` Streamable HTTP 전송으로 노출해 MCP 클라이언트가 표준 도구 목록과 호출 계약을 사용할 수 있게 했다.
- 실행 명령:
  - `dotnet build SMSR.slnx --verbosity:minimal`
  - `dotnet run --project src/SMSR.App/SMSR.App.csproj --no-build -- --self-test`
- 검증 결과:
  - 빌드가 경고 0, 오류 0으로 통과했다.
  - self-check가 미인증 `/mcp` 요청의 401 응답, `/api/state`의 200 응답, 표준 JSON-RPC `tools/list`의 `record_event`·`get_state` 노출을 확인했다.
- 남은 위험:
  - 실제 사용할 MCP 클라이언트가 bearer token 사용자 지정 헤더를 지원하는지 수동 연결에서 확인해야 한다.
- 다음 조치:
  - `/api/state`를 사용하는 정적 브라우저 대시보드를 추가한다.

## 2026-08-27 - 브라우저 상태 대시보드 추가

- 변경 파일:
  - `src/SMSR.App/MainWindow.xaml`
  - `src/SMSR.App/MainWindow.xaml.cs`
  - `src/SMSR.App/Mvp/DashboardPage.cs`
  - `src/SMSR.App/Mvp/LocalServer.cs`
  - `src/SMSR.App/Mvp/MvpSelfCheck.cs`
  - `docs/wpf-mcp-dashboard-project-plan.md`
  - `docs/wpf-mcp-dashboard-project-plan.html`
  - `docs/development-log.md`
- 변경 사유:
  - 프로젝트·워크플로우 ID로 최신 노드 상태를 기본 브라우저에서 확인할 수 있도록 최소 대시보드와 WPF 열기 동작을 추가했다.
- 실행 명령:
  - `dotnet build SMSR.slnx --no-restore --verbosity:minimal`
  - `dotnet run --project src/SMSR.App/SMSR.App.csproj --no-build -- --self-test`
- 검증 결과:
  - 빌드가 경고 0, 오류 0으로 통과했다.
  - self-check가 `/dashboard`의 200 응답과 신뢰하지 않는 문자열의 HTML 이스케이프를 확인했다.
- 남은 위험:
  - 현재 대시보드는 표·전체 새로 고침 방식이며 그래프, 상세 타임라인, SSE는 아직 제공하지 않는다.
- 다음 조치:
  - MCP `record_event` 호출 예제와 실제 클라이언트 연결 절차를 문서화한다.

## 2026-08-27 - MCP 연결 절차 문서화

- 변경 파일:
  - `README.md`
  - `docs/mcp-connection.md`
  - `docs/development-log.md`
- 변경 사유:
  - 앱의 `/mcp` endpoint, bearer token 전달, `record_event` 입력 규칙, 수동 연결 점검 예제를 제공하기 위해 추가했다.
- 실행 명령:
  - `git diff --check`
  - `dotnet build SMSR.slnx --no-restore --verbosity:minimal`
  - `dotnet run --project src/SMSR.App/SMSR.App.csproj --no-build -- --self-test`
- 검증 결과:
  - README에서 연결 문서로 이동할 수 있다.
  - 문서의 도구 이름과 입력 필드는 현재 `WorkflowTools` 구현과 일치한다.
- 남은 위험:
  - 사용하는 MCP 클라이언트의 endpoint·헤더 설정 UI는 제품마다 다르다.
- 다음 조치:
  - 실제 사용할 MCP 클라이언트에서 endpoint와 bearer token을 설정해 `record_event` 호출을 점검한다.

## 2026-08-27 - MCP record_event 실제 호출 검증

- 변경 파일:
  - `src/SMSR.App/Mvp/MvpSelfCheck.cs`
  - `docs/mcp-connection.md`
  - `docs/development-log.md`
- 변경 사유:
  - 표준 MCP `tools/call`로 `record_event`를 호출하고 상태 API 반영까지 검증하도록 self-check를 확장했다. `MCP-Name` 헤더는 호출 도구명과 일치하도록 예제를 수정했다.
- 실행 명령:
  - `dotnet build SMSR.slnx --no-restore --verbosity:minimal`
  - `dotnet run --project src/SMSR.App/SMSR.App.csproj --no-build -- --self-test`
- 검증 결과:
  - 임시 MCP 서버에서 `tools/call`의 `record_event`가 HTTP 200으로 완료됐다.
  - 이어진 `/api/state` 조회에서 기록한 `mcp-node`를 확인했다.
- 남은 위험:
  - 실제 MCP 클라이언트의 사용자 지정 bearer header 설정은 해당 클라이언트에서 별도로 확인해야 한다.
- 다음 조치:
  - 대시보드에서 상태별 요약과 최근 이벤트를 표시한다.

## 2026-08-27 - 대시보드 상태 요약 및 최근 이벤트

- 변경 파일:
  - `src/SMSR.App/Mvp/Contracts.cs`
  - `src/SMSR.App/Mvp/EventStore.cs`
  - `src/SMSR.App/Mvp/DashboardPage.cs`
  - `src/SMSR.App/Mvp/LocalServer.cs`
  - `src/SMSR.App/Mvp/MvpSelfCheck.cs`
  - `docs/development-log.md`
- 변경 사유:
  - 현재 노드 상태의 상태별 집계와 최근 이벤트 10건을 기본 대시보드에서 함께 확인하도록 했다.
- 실행 명령:
  - `dotnet build SMSR.slnx --no-restore --verbosity:minimal`
  - `dotnet run --project src/SMSR.App/SMSR.App.csproj --no-build -- --self-test`
- 검증 결과:
  - 빌드가 경고 0, 오류 0으로 통과했다.
  - self-check가 최근 이벤트의 최신순 조회, HTML 이스케이프, MCP 기록 이벤트의 대시보드 표시를 확인했다.
- 남은 위험:
  - 전체 새로 고침 방식이므로 실시간 스트림과 대량 이벤트 탐색에는 적합하지 않다.
- 다음 조치:
  - WPF에서 로컬 서버 상태와 프로젝트·워크플로우 목록을 관리한다.

## 2026-08-27 - WPF MVVM 화면 구조화

- 변경 파일:
  - `src/SMSR.App/Views/MainWindow.xaml`
  - `src/SMSR.App/Views/MainWindow.xaml.cs`
  - `src/SMSR.App/ViewModels/MainWindowViewModel.cs`
  - `src/SMSR.App/Infrastructure/IPlatformActions.cs`
  - `src/SMSR.App/Infrastructure/WindowsPlatformActions.cs`
  - `src/SMSR.App/Infrastructure/RelayCommand.cs`
  - `src/SMSR.App/Themes/FlatTheme.xaml`
  - `src/SMSR.App/Themes/Controls.xaml`
  - `src/SMSR.App/App.xaml`
  - `src/SMSR.App/App.xaml.cs`
  - `src/SMSR.App/Mvp/MvpSelfCheck.cs`
  - `docs/development-log.md`
- 변경 사유:
  - WPF XAML, 화면 상태·명령, Windows 플랫폼 연동을 분리해 코드비하인드 없이 MVVM 방식으로 화면을 구성하고, 테마 리소스에 flat 2D 스타일을 적용했다.
- 실행 명령:
  - `dotnet build SMSR.slnx --no-restore --verbosity:minimal`
  - `dotnet run --project src/SMSR.App/SMSR.App.csproj --no-build -- --self-test`
- 검증 결과:
  - 빌드가 경고 0, 오류 0으로 통과했다.
  - self-check가 ViewModel의 토큰 복사·대시보드 열기 명령과 기존 MCP 기록 흐름을 확인했다.
- 남은 위험:
  - 현재 프로젝트·워크플로우 목록을 위한 별도 저장 모델은 아직 없다.
- 다음 조치:
  - 이벤트 저장소에서 프로젝트·워크플로우 목록을 조회하고 WPF 선택 UI에 연결한다.

## 2026-08-27 - WPF 프로젝트·워크플로우 선택

- 변경 파일:
  - `src/SMSR.App/Mvp/EventStore.cs`
  - `src/SMSR.App/Mvp/LocalServer.cs`
  - `src/SMSR.App/ViewModels/ViewModelBase.cs`
  - `src/SMSR.App/ViewModels/WorkflowSelectionViewModel.cs`
  - `src/SMSR.App/ViewModels/MainWindowViewModel.cs`
  - `src/SMSR.App/Views/MainWindow.xaml`
  - `src/SMSR.App/Themes/Controls.xaml`
  - `src/SMSR.App/App.xaml.cs`
  - `src/SMSR.App/Mvp/MvpSelfCheck.cs`
  - `docs/development-log.md`
- 변경 사유:
  - SQLite 이벤트에서 고유 프로젝트·워크플로우 ID를 조회해 WPF에서 선택하거나 직접 입력할 수 있도록 했다.
- 실행 명령:
  - `dotnet build SMSR.slnx --no-restore --verbosity:minimal`
  - `dotnet run --project src/SMSR.App/SMSR.App.csproj --no-build -- --self-test`
- 검증 결과:
  - 빌드가 경고 0, 오류 0으로 통과했다.
  - self-check가 이벤트 저장소의 고유 ID 조회와 MCP 기록 뒤 WPF 선택 목록 갱신을 확인했다.
- 남은 위험:
  - 워크플로우 목록은 프로젝트 변경 뒤 목록 새로 고침을 눌러 갱신한다.
- 다음 조치:
  - WPF에서 선택한 워크플로우의 최신 상태와 최근 이벤트를 표시한다.

## 2026-08-27 - WPF 관제·실시간·요약·내보내기 확장

- 변경 파일:
  - `src/SMSR.App/App.xaml.cs`
  - `src/SMSR.App/Services/LocalServerHost.cs`
  - `src/SMSR.App/ViewModels/MainWindowViewModel.cs`
  - `src/SMSR.App/ViewModels/ServerControlViewModel.cs`
  - `src/SMSR.App/ViewModels/WorkflowMonitorViewModel.cs`
  - `src/SMSR.App/ViewModels/WorkflowWorkspaceViewModel.cs`
  - `src/SMSR.App/ViewModels/WorkflowSelectionViewModel.cs`
  - `src/SMSR.App/Views/MainWindow.xaml`
  - `src/SMSR.App/Views/ServerPanel.xaml`
  - `src/SMSR.App/Views/WorkflowPanel.xaml`
  - `src/SMSR.App/Mvp/Contracts.cs`
  - `src/SMSR.App/Mvp/EventStore.cs`
  - `src/SMSR.App/Mvp/EventStoreEvents.cs`
  - `src/SMSR.App/Mvp/EventStoreSummaries.cs`
  - `src/SMSR.App/Mvp/LocalServer.cs`
  - `src/SMSR.App/Mvp/LocalServerEndpoints.cs`
  - `src/SMSR.App/Mvp/WorkflowEventNotifier.cs`
  - `src/SMSR.App/Mvp/WorkflowSummaryService.cs`
  - `src/SMSR.App/Mvp/WorkflowExportService.cs`
  - `src/SMSR.App/Mvp/WorkflowTools.cs`
  - `src/SMSR.App/Mvp/MvpSelfCheck.cs`
  - `docs/development-log.md`
- 변경 사유:
  - WPF에서 최신 노드 상태·최근 이벤트를 표시하고, 로컬 서버 시작·중지, SSE 상태 스트림, 로컬 요약 저장, HTML·Markdown·JSON·ZIP 내보내기를 제공한다.
- 실행 명령:
  - `dotnet build SMSR.slnx --no-restore --verbosity:minimal`
  - `dotnet run --project src/SMSR.App/SMSR.App.csproj --no-build -- --self-test`
  - `git diff --check`
- 검증 결과:
  - 빌드가 경고 0, 오류 0으로 통과했다.
  - self-check가 실제 MCP 기록, SSE 초기·변경 이벤트, 서버 재시작·중지, WPF 상태 조회, 요약 생성·저장, ZIP 내보내기를 확인했다.
- 남은 위험:
  - 대시보드의 기본 폴링은 2초 전체 새로 고침이며, SSE 소비 UI는 후속 개선 대상이다.
  - 요약은 외부 LLM이 아닌 로컬 상태·이벤트 기반 결정형 문장이다.
- 다음 조치:
  - WPF에서 SSE를 직접 구독하는 선택적 갱신과 SQLite 동시 쓰기 부하 검증을 추가한다.

## 2026-08-27 - WPF SSE 구독·SQLite 부하·서버 복구 검증

- 변경 파일:
  - `src/SMSR.App/Services/SseStateClient.cs`
  - `src/SMSR.App/Services/LocalActivityLog.cs`
  - `src/SMSR.App/Services/LocalServerHost.cs`
  - `src/SMSR.App/ViewModels/WorkflowMonitorViewModel.cs`
  - `src/SMSR.App/ViewModels/WorkflowWorkspaceViewModel.cs`
  - `src/SMSR.App/Views/WorkflowPanel.xaml`
  - `src/SMSR.App/Mvp/EventStore.cs`
  - `src/SMSR.App/Mvp/MvpSelfCheck.cs`
  - `docs/development-log.md`
- 변경 사유:
  - WPF가 SSE 상태 스트림을 직접 구독하고 연결 해제 시 2초 polling으로 전환하게 했으며, SQLite 동시 기록 안정성과 서버 재시작 후 상태·로그 복구를 점검한다.
- 실행 명령:
  - `dotnet build SMSR.slnx --no-restore --verbosity:minimal`
  - `dotnet run --project src/SMSR.App/SMSR.App.csproj --no-build -- --self-test`
  - `git diff --check`
- 검증 결과:
  - 빌드가 경고 0, 오류 0으로 통과했다.
  - self-check가 동시 16건 SQLite 기록, SSE 초기·변경 이벤트, 서버 재시작·중지, `server started`·`server stopped` 로컬 로그를 확인했다.
- 남은 위험:
  - 단일 로컬 쓰기 게이트는 높은 처리량에서는 병목이 될 수 있다.
  - 실제 장시간 SSE 연결과 대규모 동시 에이전트 부하는 별도 수동 부하 시험이 필요하다.
- 다음 조치:
  - 설치 패키징과 장시간 연결·대용량 이벤트 수용 시험을 수행한다.

## 2026-08-27 - 운영 로그 회전과 서버 오류 안내

- 변경 파일:
  - `src/SMSR.App/Services/LocalActivityLog.cs`
  - `src/SMSR.App/ViewModels/ServerControlViewModel.cs`
  - `src/SMSR.App/Mvp/MvpSelfCheck.cs`
  - `docs/development-log.md`
- 변경 사유:
  - 무제한 활동 로그 증가를 막고, 서버 제어 실패 시 WPF 화면에서 원인을 확인할 수 있게 한다.
- 실행 명령:
  - `dotnet build SMSR.slnx --no-restore --verbosity:minimal`
  - `dotnet run --project src/SMSR.App/SMSR.App.csproj --no-build -- --self-test`
  - `git diff --check`
- 검증 결과:
  - self-check가 1MB 활동 로그의 이전 파일 보관과 새 로그 기록을 확인한다.
  - `dotnet build`가 경고 0, 오류 0으로 통과했고 전체 self-check도 통과했다.
- 남은 위험:
  - 최근 로그 파일 2개만 보관하므로 장기 감사 보관이 필요하면 Windows 이벤트 로그 또는 외부 수집기를 연동해야 한다.
- 다음 조치:
  - 설치 패키징과 장시간 연결·대용량 이벤트 수용 시험을 수행한다.

## 2026-08-27 - 저자원 관제 안정화

- 변경 파일:
  - `src/SMSR.App/Mvp/EventStoreSummaries.cs`
  - `src/SMSR.App/Mvp/Contracts.cs`
  - `src/SMSR.App/Mvp/WorkflowTools.cs`
  - `src/SMSR.App/Mvp/WorkflowEventNotifier.cs`
  - `src/SMSR.App/Mvp/LocalServerEndpoints.cs`
  - `src/SMSR.App/Mvp/LocalServer.cs`
  - `src/SMSR.App/Services/LocalActivityLog.cs`
  - `src/SMSR.App/Services/LocalServerHost.cs`
  - `src/SMSR.App/ViewModels/WorkflowWorkspaceViewModel.cs`
  - `src/SMSR.App/Mvp/MvpSelfCheck.cs`
  - `docs/development-log.md`
- 변경 사유:
  - LLM 작업 현황 관제의 CPU·스레드·SQLite 사용량을 낮추고, 서버 종료·동시 저장·입력 검증의 경계 조건을 보완한다.
- 실행 명령:
  - `dotnet build SMSR.slnx --no-restore --verbosity:minimal`
  - `dotnet run --project src/SMSR.App/SMSR.App.csproj --no-build -- --self-test`
  - `git diff --check`
- 검증 결과:
  - `dotnet build`가 경고 0, 오류 0으로 통과했고 전체 self-check도 통과했다.
  - self-check가 워크플로우별 SSE 신호 분리, 이벤트·요약 동시 SQLite 저장, 배열 크기·식별자 검증, 서버 중지 후 명령 비활성화를 확인했다.
- 남은 위험:
  - 관측한 워크플로우별 SSE 신호는 메모리에 유지된다. 매우 많은 서로 다른 워크플로우를 감시할 때만 만료 정책을 추가한다.
- 다음 조치:
  - 실제 LLM 에이전트 연결로 장시간 SSE와 대용량 이벤트 수용량을 측정한다.

## 2026-08-27 - 저자원 데이터 경로 최적화

- 변경 파일:
  - `src/SMSR.App/Mvp/EventStore.cs`
  - `src/SMSR.App/Mvp/EventStoreWrites.cs`
  - `src/SMSR.App/Mvp/EventStoreStateQueries.cs`
  - `src/SMSR.App/Mvp/EventStoreEvents.cs`
  - `src/SMSR.App/Mvp/EventStoreCatalog.cs`
  - `src/SMSR.App/Mvp/LocalServerEndpoints.cs`
  - `src/SMSR.App/Mvp/WorkflowSummaryService.cs`
  - `src/SMSR.App/Mvp/WorkflowExportService.cs`
  - `src/SMSR.App/ViewModels/WorkflowMonitorViewModel.cs`
  - `src/SMSR.App/Mvp/MvpSelfCheck.cs`
  - `docs/development-log.md`
- 변경 사유:
  - 상태 조회를 활성 노드 테이블로 전환하고, SSE 중복 상태 전송과 요약·내보내기의 전체 이벤트 메모리 적재를 없앤다.
- 실행 명령:
  - `dotnet build SMSR.slnx --no-restore --verbosity:minimal`
  - `dotnet run --project src/SMSR.App/SMSR.App.csproj --no-build -- --self-test`
  - `git diff --check`
- 검증 결과:
  - `dotnet build`가 경고 0, 오류 0으로 통과했고 전체 self-check도 통과했다.
  - self-check가 현재 상태 테이블 갱신, SSE 변경 알림, 최신 이벤트 조회, JSONL 내보내기 파일 생성을 확인했다.
- 남은 위험:
  - 내보내기 ZIP 생성은 사용자 요청 시 단일 백그라운드 작업을 사용하며, 운영 환경의 실제 데이터량에서 소요 시간 측정이 필요하다.
- 다음 조치:
  - SQLite 파일·내보내기 보관 기간과 장시간 LLM 이벤트 부하의 측정 기준을 정한다.

## 2026-08-27 - Codex 계획 노드·훅 연동

- 변경 파일:
  - `src/SMSR.App/Mvp/PlanContracts.cs`
  - `src/SMSR.App/Mvp/EventStorePlanWrites.cs`
  - `src/SMSR.App/Mvp/EventStorePlanQueries.cs`
  - `src/SMSR.App/Mvp/PlanTools.cs`
  - `src/SMSR.App/Mvp/EventStore.cs`
  - `src/SMSR.App/Mvp/EventStoreCatalog.cs`
  - `src/SMSR.App/Mvp/WorkflowTools.cs`
  - `src/SMSR.App/Mvp/LocalServer.cs`
  - `src/SMSR.App/Mvp/LocalServerEndpoints.cs`
  - `src/SMSR.App/Mvp/DashboardPage.cs`
  - `src/SMSR.App/Mvp/MvpSelfCheck.cs`
  - `plugins/smsr-codex/`
  - `docs/mcp-connection.md`
  - `docs/development-log.md`
- 변경 사유:
  - Codex가 구조화한 계획 노드·의존성·가중치를 SQLite에 저장하고, 동일 노드 ID의 상태 이벤트를 웹 그래프와 API에 적용하도록 했다. Codex 플러그인은 세션 ID를 계획 ID로 안내하고 요청 접수·턴 종료만 최소 활동으로 기록한다.
- 실행 명령:
  - `dotnet build SMSR.slnx --no-restore --verbosity:minimal`
  - `dotnet run --project src/SMSR.App/SMSR.App.csproj --no-build -- --self-test`
  - `node plugins/smsr-codex/hooks/session-context.js`
  - `py -3 C:\Users\surromind\.codex\skills\.system\plugin-creator\scripts\validate_plugin.py plugins/smsr-codex`
  - `git diff --check`
- 검증 결과:
  - 실제 Streamable HTTP MCP 호출로 `save_plan`, `record_event`, `record_lifecycle`를 수행한 뒤 `/api/plan`, `/api/state`, `/dashboard`에서 계획 제목·적용 상태·세션 활동을 확인했다.
  - 순환 의존성은 저장 전에 거부하며, 플러그인 훅 컨텍스트와 플러그인 구조 검증이 통과했다.
  - 고정 로컬 주소 `http://127.0.0.1:49783`에서만 수신하며 빌드 경고·오류가 없다.
- 남은 위험:
  - 사용자가 SMSR 토큰을 환경 변수에 설정하고 Codex MCP 서버·플러그인을 설치하기 전에는 실제 Codex 세션 훅이 실행되지 않는다.
  - 고정 포트가 다른 프로세스에 사용 중이면 앱 시작이 실패한다.
- 다음 조치:
  - Codex 설정에 `smsr` MCP 연결을 추가하고 플러그인 훅 신뢰 후 실제 대화 한 건을 점검한다.

## 2026-08-27 - Codex 설치·운영 절차 정리

- 변경 파일:
  - `.agents/plugins/marketplace.json`
  - `plugins/smsr-codex/.codex-plugin/plugin.json`
  - `README.md`
  - `docs/mcp-connection.md`
  - `docs/development-log.md`
- 변경 사유:
  - 저장소의 플러그인을 Codex 로컬 마켓플레이스에서 설치할 수 있게 하고, 보안 토큰 전달·훅 신뢰·포트 충돌·실제 대화 점검 절차를 문서화했다.
- 실행 명령:
  - `codex plugin marketplace add D:\Gitsource\개인\SMSR`
  - `codex plugin add smsr-codex@personal`
  - `codex plugin list`
- 검증 결과:
  - 로컬 마켓플레이스가 `plugins/smsr-codex`을 가리키며 플러그인 구조 검증을 통과했다.
  - `smsr-codex@personal`을 현재 Codex에 설치·활성화했다.
- 남은 위험:
  - DPAPI 토큰은 사용자가 앱에서 복사해 현재 세션에 설정해야 하므로, 실제 MCP 등록·훅 신뢰는 해당 사용자 단계가 필요하다.
- 다음 조치:
  - 새 Codex task에서 실제 계획 한 건을 기록해 웹 대시보드 갱신을 점검한다.
## 2026-08-28 - 앱 보조 제목 변경

- 변경 파일:
  - `src/SMSR.App/Views/MainWindow.xaml`
  - `docs/development-log.md`
- 변경 사유:
  - 앱의 기존 보조 제목을 사용자가 지정한 `Show Me Status Report`로 바꿨다.
- 실행 명령:
  - `dotnet build SMSR.slnx --no-restore --verbosity:minimal`
- 검증 결과:
  - `dotnet build SMSR.slnx --no-restore --verbosity:minimal` 통과(경고 0, 오류 0).
- 남은 위험:
  - 없음.
- 다음 조치:
  - 탭 기반 화면 및 초기 설정 흐름은 별도 변경으로 진행한다.

## 2026-08-28 - 탭 UI와 트레이 종료 동작 개선

- 변경 파일:
  - `src/SMSR.App/Views/MainWindow.xaml`
  - `src/SMSR.App/Views/MainWindow.xaml.cs`
  - `src/SMSR.App/ViewModels/MainWindowViewModel.cs`
  - `src/SMSR.App/Infrastructure/TrayStatusIcon.cs`
  - `src/SMSR.App/App.xaml.cs`
  - `src/SMSR.App/SMSR.App.csproj`
  - `src/SMSR.App/Themes/FlatTheme.xaml`
  - `src/SMSR.App/Themes/Controls.xaml`
  - `docs/development-log.md`
- 변경 사유:
  - 긴 단일 화면을 작업 현황·서버 연결 탭으로 나누고, 벡터 상태 로고와 평면 테마를 적용했다. 일반 닫기는 트레이 이동, 완전 종료는 명시 명령으로 처리한다.
- 실행 명령:
  - `dotnet build SMSR.slnx --no-restore --verbosity:minimal`
  - `dotnet run --project src/SMSR.App/SMSR.App.csproj --no-build -- --self-test`
- 검증 결과:
  - 빌드 통과(경고 0, 오류 0). 기존 MCP·저장소·WPF ViewModel self-check도 통과했다. 실제 WPF 첫 탭의 로고·탭·완전 종료 버튼 배치를 확인했다.
- 남은 위험:
  - 트레이 메뉴·창 숨김 동작은 실제 Windows 셸에서 한 번 수동 확인이 필요하다.
- 다음 조치:
  - DPAPI 토큰을 평문 저장 없이 Codex에 자동 연결하려면 별도 로컬 stdio 브리지 설계가 필요하다.

## 2026-08-28 - 커스텀 상단 바와 상태 표기 보완

- 변경 파일:
  - `src/SMSR.App/Views/MainWindow.xaml`
  - `src/SMSR.App/Views/MainWindow.xaml.cs`
  - `src/SMSR.App/Views/WorkflowPanel.xaml`
  - `src/SMSR.App/Views/WorkflowHistoryPanel.xaml`
  - `src/SMSR.App/Views/WorkflowHistoryPanel.xaml.cs`
  - `src/SMSR.App/Views/ServerPanel.xaml`
  - `src/SMSR.App/ViewModels/ServerControlViewModel.cs`
  - `src/SMSR.App/Themes/Controls.xaml`
  - `docs/development-log.md`
- 변경 사유:
  - Windows 기본 제목 표시줄을 제거하고 최소화·트레이 닫기·완전 종료를 담은 커스텀 상단 바로 바꿨다. 긴 워크플로우 화면은 이벤트·요약 탭으로 분리하고, 서버 자동 시작 상태를 명확한 실행/중지 문구로 표시한다.
- 실행 명령:
  - `dotnet build SMSR.slnx --no-restore --verbosity:minimal -p:OutputPath=<임시 경로>`
  - `dotnet run --project src/SMSR.App/SMSR.App.csproj --no-build -- --self-test -p:OutputPath=<임시 경로>`
- 검증 결과:
  - 임시 출력 빌드 통과(경고 0, 오류 0). 이미 실행 중인 앱이 `49783`을 점유해 self-check의 별도 서버 시작은 실행하지 못했다.
- 남은 위험:
  - 현재 실행 중인 이전 버전을 완전 종료한 뒤, 새 상단 바와 탭 분리 화면을 수동 확인해야 한다.
- 다음 조치:
  - 앱 종료 후 self-check를 다시 실행한다.

## 2026-08-28 - 명확한 동작 아이콘 적용

- 변경 파일:
  - `src/SMSR.App/Themes/Controls.xaml`
  - `src/SMSR.App/Views/ServerPanel.xaml`
  - `src/SMSR.App/Views/WorkflowPanel.xaml`
  - `src/SMSR.App/Views/WorkflowHistoryPanel.xaml`
  - `docs/development-log.md`
- 변경 사유:
  - 시작·중지·토큰 복사·대시보드 열기·내보내기처럼 의미가 보편적인 동작을 아이콘 버튼과 툴팁으로 간결하게 표시했다.
- 실행 명령:
  - `dotnet build SMSR.slnx --no-restore --verbosity:minimal -p:OutputPath=<임시 경로>`
- 검증 결과:
  - 임시 출력 빌드 통과(경고 0, 오류 0).
- 남은 위험:
  - 아이콘 글리프의 Windows 글꼴 표시를 새 빌드 실행 화면에서 확인해야 한다.
- 다음 조치:
  - 새 빌드로 화면과 툴팁을 확인한다.

## 2026-08-28 - Codex 초기 연결과 DPAPI stdio 브리지

- 변경 파일:
  - `src/SMSR.App/Mvp/McpHttpGateway.cs`
  - `src/SMSR.App/Mvp/StdioMcpHost.cs`
  - `src/SMSR.App/Mvp/StdioWorkflowTools.cs`
  - `src/SMSR.App/Mvp/StdioPlanTools.cs`
  - `src/SMSR.App/Services/CodexConnectionService.cs`
  - `src/SMSR.App/ViewModels/ServerControlViewModel.cs`
  - `src/SMSR.App/Views/ServerPanel.xaml`
  - `src/SMSR.App/App.xaml.cs`
  - `src/SMSR.App/SMSR.App.csproj`
  - `src/SMSR.App/Mvp/MvpSelfCheck.cs`
  - `README.md`, `docs/mcp-connection.md`, `docs/development-log.md`
- 변경 사유:
  - 토큰 평문 저장 없이 앱의 초기 연결 버튼으로 Codex stdio MCP와 플러그인을 1회 등록하고, 기존 HTTP 서버를 통해 SSE 갱신을 유지한다.
- 실행 명령:
  - `dotnet build SMSR.slnx --no-restore --verbosity:minimal -p:OutputPath=<임시 경로>`
  - `<임시 경로>\\SMSR.App.exe --self-test`
  - `<임시 경로>\\SMSR.App.exe --mcp-stdio` JSON-RPC initialize·tools/list 점검
  - `git diff --check`
- 검증 결과:
  - 임시 출력 빌드가 경고 0, 오류 0으로 통과했다.
  - self-check가 stdio 브리지의 HTTP 전달, SQLite 기록, SSE 변경 알림을 확인했다.
  - stdio MCP가 `record_event`, `save_plan`, `record_lifecycle`을 포함한 8개 도구를 노출했다.
- 남은 위험:
  - 훅 신뢰는 Codex 보안 정책상 사용자가 `/hooks`에서 직접 승인해야 한다.
  - 이미 다른 `smsr` MCP가 등록된 환경에서는 사용자 검토 후 교체해야 한다.
- 다음 조치:
  - 앱에서 초기 연결을 실행하고 Codex 재시작·훅 신뢰 후 실제 task 이벤트 수신을 수동 확인한다.

## 2026-08-28 - 재진입 진행도 복원과 앱 아이콘

- 변경 파일:
  - `src/SMSR.App/Services/LocalServerHost.cs`
  - `src/SMSR.App/ViewModels/WorkflowSelectionViewModel.cs`
  - `src/SMSR.App/ViewModels/WorkflowWorkspaceViewModel.cs`
  - `src/SMSR.App/Mvp/MvpSelfCheck.cs`
  - `src/SMSR.App/Assets/SMSR.png`, `src/SMSR.App/Assets/SMSR.ico`
  - `src/SMSR.App/SMSR.App.csproj`
  - `src/SMSR.App/Views/MainWindow.xaml`
  - `src/SMSR.App/Infrastructure/TrayStatusIcon.cs`
  - `README.md`, `docs/mcp-connection.md`, `docs/development-log.md`
- 변경 사유:
  - 작업 도중 앱을 닫고 다시 열어도 마지막 선택과 SQLite 진행도를 복원하며, 창·트레이·실행 파일에 통일된 SMSR 아이콘을 적용한다.
- 실행 명령:
  - `dotnet build SMSR.slnx --no-restore --verbosity:minimal -p:OutputPath=<임시 경로>`
  - `<임시 경로>\\SMSR.App.exe --self-test`
- 검증 결과:
  - self-check가 서버 재시작 뒤 `demo/wf-1` 선택과 저장된 노드 상태 복원을 확인했다.
- 남은 위험:
  - 새 아이콘의 작업 표시줄·탐색기 표시 캐시는 Windows 셸이 갱신되기 전까지 이전 아이콘을 보일 수 있다.
- 다음 조치:
  - 새 빌드 실행 후 창·트레이·작업 표시줄 아이콘을 수동 확인한다.

## 2026-08-28 - 창 밀도와 트레이 아이콘 보완

- 변경 파일:
  - `src/SMSR.App/Views/MainWindow.xaml`
  - `src/SMSR.App/Views/WorkflowPanel.xaml`
  - `src/SMSR.App/Views/WorkflowHistoryPanel.xaml`
  - `src/SMSR.App/Infrastructure/TrayStatusIcon.cs`
  - `src/SMSR.App/SMSR.App.csproj`
  - `docs/development-log.md`
- 변경 사유:
  - 기본 창의 불필요한 높이와 고정 폭 텍스트 버튼을 줄이고, 상단 로고가 열 폭을 넘지 않게 했다. 트레이는 실행 파일 캐시 대신 배포 아이콘 파일을 직접 사용한다.
- 실행 명령:
  - `dotnet build SMSR.slnx --no-restore --verbosity:minimal -p:OutputPath=<임시 경로>`
  - `<임시 경로>\\SMSR.App.exe` 실행 화면 확인
- 검증 결과:
  - 새 창에서 640px 높이, 내용 폭 버튼, 상단 아이콘의 잘림 없음이 확인됐다.
- 남은 위험:
  - 기존에 실행 중인 앱은 완전 종료 후 새 빌드로 다시 실행해야 새 트레이 아이콘을 사용한다.
- 다음 조치:
  - 트레이 이동 뒤 Windows 알림 영역의 아이콘을 수동 확인한다.

## 2026-08-28 - 텍스트 버튼 내부 여백 수정

- 변경 파일:
  - `src/SMSR.App/Themes/Controls.xaml`
  - `docs/development-log.md`
- 변경 사유:
  - 공통 Button 템플릿의 `ContentPresenter`가 `Padding`을 사용하지 않아 텍스트가 버튼 가장자리에 붙던 문제를 해결한다.
- 실행 명령:
  - `dotnet build SMSR.slnx --no-restore --verbosity:minimal -p:OutputPath=<임시 경로>`
- 검증 결과:
  - 기본·보조 텍스트 버튼 템플릿이 각각 설정된 내부 여백을 콘텐츠에 적용한다.
- 남은 위험:
  - 없음.
- 다음 조치:
  - 새 빌드에서 텍스트 버튼의 좌우 여백을 확인한다.

## 2026-08-28 - Codex CLI PATH 탐색 보완

- 변경 파일:
  - `src/SMSR.App/Services/CodexConnectionService.cs`
  - `src/SMSR.App/Services/CodexCliLocator.cs`
  - `README.md`
  - `docs/mcp-connection.md`
- 변경 사유:
  - Visual Studio나 데스크톱 앱에서 실행할 때 앱 프로세스가 Codex CLI의 PATH를 상속하지 않아 초기 MCP 등록이 실패하는 문제를 보완했다.
- 실행 명령:
  - `dotnet build SMSR.slnx --no-restore --verbosity:minimal`
  - `dotnet run --project src/SMSR.App/SMSR.App.csproj --no-build -- --self-test`
  - `git diff --check`
- 검증 결과:
  - 현재 Windows 사용자의 PATH와 시스템 PATH를 앱 실행 시 합쳐 Codex CLI 탐색에 사용하도록 했다.
  - Codex 데스크톱 패키지의 `WindowsApps` 내부 실행 파일을 외부 CLI 후보에서 제외했다.
  - Windows npm 설치에서 생성되는 `codex.cmd` 실행 래퍼도 처리하도록 했다.
  - standalone CLI 미설치·권한 오류를 사용자에게 구분해 안내하도록 했다.
  - self-check 예외를 앱에서 처리해 CLR 충돌창 대신 상세 오류를 표시하도록 했다.
- 남은 위험:
  - standalone Codex CLI 설치와 PATH 등록은 사용자 환경에서 별도로 필요하다.
- 다음 조치:
  - 새 빌드에서 `초기 연결`을 눌러 실제 Codex MCP 등록과 플러그인 설치를 확인한다.

## 2026-08-28 - Codex 데스크톱 탐지와 CLI 의존성 제거

- 변경 파일:
  - `src/SMSR.App/Services/CodexConnectionService.cs`
  - `src/SMSR.App/Services/CodexDesktopLocator.cs`
  - `src/SMSR.App/Services/CodexMcpConfig.cs`
  - `src/SMSR.App/Services/CodexMcpConfigSelfCheck.cs`
  - `src/SMSR.App/Mvp/MvpSelfCheck.cs`
  - `src/SMSR.App/Mvp/SmsrMcpInstructions.cs`
  - `src/SMSR.App/Mvp/StdioMcpHost.cs`
  - `src/SMSR.App/Mvp/LocalServer.cs`
  - `src/SMSR.App/ViewModels/ServerControlViewModel.cs`
  - `src/SMSR.App/Views/ServerPanel.xaml`
  - `src/SMSR.App/SMSR.App.csproj`
  - `README.md`, `docs/mcp-connection.md`, `docs/development-log.md`
- 변경 사유:
  - 같은 초기 연결 실패가 반복되어 CLI 탐색 재시도를 중단했다. 원인은 설치된 Codex 데스크톱 앱과 PowerShell에서 호출 가능한 standalone CLI를 동일하게 취급한 설계였다.
  - 기존 시도는 `codex` 직접 실행, 사용자·시스템 PATH 병합, 실행 경로 환경 변수, standalone CLI 설치 안내였다. 데스크톱 패키지의 비공개 실행 파일에는 적용할 수 없었다.
  - 새 계획으로 현재 사용자의 `OpenAI.Codex` 패키지를 탐지하고, 공식 공유 설정 `~/.codex/config.toml`에 stdio MCP 항목을 직접 등록하도록 교체했다.
  - CLI로 설치하던 플러그인의 핵심 추적 규칙은 MCP 초기화 `instructions`로 옮겼다.
- 실행 명령:
  - `Get-AppxPackage`와 사용자 AppModel 패키지 레지스트리로 설치 상태 확인
  - `dotnet build SMSR.slnx --no-restore --verbosity:minimal -p:OutputPath=<임시 경로>`
  - `<임시 경로>\SMSR.App.exe --codex-config-self-test`
  - `rg`로 소스·사용자 문서의 CLI 참조 확인
  - `git diff --check`
- 검증 결과:
  - 이 PC에서 `OpenAI.Codex_26.818.8289.0_x64__2p2nqsd0c76g0` 패키지와 공유 설정을 탐지했다.
  - 임시 출력 빌드가 경고 0, 오류 0으로 통과했다.
  - 분리 self-check가 다른 MCP 항목 보존, `smsr` 항목 등록·갱신, 중복 방지와 `config.toml.smsr.bak` 생성을 확인했다.
  - 실행 코드와 사용자 연결 문서에서 `codex` CLI, npm, `SMSR_CODEX_PATH` 의존 참조가 제거됐다.
  - stdio·HTTP MCP 서버가 같은 계획·상태 기록 지침을 초기화 응답으로 제공한다.
- 남은 위험:
  - Codex가 설정을 다시 읽도록 초기 등록 뒤 데스크톱 앱을 완전히 재시작해야 한다.
  - Codex 패키지 식별자가 `OpenAI.Codex`에서 바뀌면 탐지 규칙을 갱신해야 한다.
  - 전체 `--self-test`는 숨김 실행에서 기존 오류 대화상자 대기 상태가 되어, 이번 변경은 `--codex-config-self-test`로 분리 검증했다.
- 다음 조치:
  - 새 빌드의 `초기 연결`을 누른 뒤 Codex `/mcp`에서 `smsr` 연결을 수동 확인한다.

## 2026-08-28 - STDIO 인증 미지원 표시 확인

- 변경 파일:
  - `src/SMSR.App/Services/CodexConnectionService.cs`
  - `README.md`, `docs/mcp-connection.md`, `docs/development-log.md`
- 변경 사유:
  - Codex 설정의 `인증 미지원` 문구가 MCP 서버 비활성화처럼 보이는 혼동을 해소한다.
- 실행 명령:
  - `~/.codex/config.toml`의 `smsr` 섹션과 실행 파일 존재 확인
  - `~/.codex/logs_2.sqlite`에서 `smsr` MCP 초기화 로그 조회
  - 현재 Codex 세션에서 `mcp__smsr__get_state` 호출
  - 현재 Codex 세션에서 `save_plan`, `record_event`, `get_plan` 진단 호출
  - `dotnet build SMSR.slnx --no-restore --verbosity:minimal -p:OutputPath=<임시 경로>`
  - `<임시 경로>\SMSR.App.exe --codex-config-self-test`
- 검증 결과:
  - `smsr`는 인증 설정이 없는 STDIO 서버로 등록되어 있으며 `enabled = false`도 없다.
  - Codex 런타임은 `smsr`를 프로토콜 `2025-06-18`로 초기화했고 전체 MCP 서버를 `available_server_count=4`, `unavailable_server_count=0`으로 기록했다.
  - 현재 세션의 `get_state` 호출이 성공해 SMSR 서버까지 왕복 연결됨을 확인했다.
  - `SMSR / connection-test-20260828` 진단 워크플로우의 두 노드가 모두 `SUCCESS`로 저장·조회됐다.
  - 임시 출력 빌드가 경고 0, 오류 0으로 통과했고 분리 self-check가 종료 코드 0을 반환했다.
- 남은 위험:
  - Codex 설정 UI의 인증 문구와 버튼 표현은 SMSR에서 변경할 수 없다.
  - 기본 출력 파일은 실행 중인 SMSR 본체와 Codex STDIO 브리지가 사용 중이어서 종료 전까지 새 안내 문구로 덮어쓸 수 없다.
- 다음 조치:
  - 연결 여부는 인증 표시가 아니라 `/mcp`의 `smsr` 도구 노출로 판단한다.

## 2026-08-28 - Codex 직접 HTTP MCP OAuth 인증

- 변경 파일:
  - `src/SMSR.App/Mvp/OAuth*.cs`, `LocalOAuthStore.cs`, `LocalServer.cs`, `LocalServerEndpoints.cs`
  - `src/SMSR.App/Services/CodexMcpConfig.cs`, `CodexConnectionService.cs`, `CodexMcpConfigSelfCheck.cs`
  - `src/SMSR.App/App.xaml.cs`, `LocalServerHost.cs`, 관련 view model·화면·self-check
  - `README.md`, `docs/mcp-connection.md`, `docs/development-log.md`
  - 삭제: STDIO 호스트·도구·HTTP gateway·고정 토큰 저장소
- 변경 사유:
  - Codex의 `인증 미지원` 상태를 없애고, 실행 파일을 STDIO로 시작하는 브리지 대신 `http://127.0.0.1:49783/mcp`에 직접 연결하는 OAuth 인증 MCP를 제공한다.
  - 공식 Codex MCP 규격의 Streamable HTTP OAuth, DCR, PKCE 및 MCP authorization 규격의 protected resource metadata와 resource audience 검증을 적용한다.
- 실행 명령:
  - 공식 Codex MCP 문서와 MCP 2025-06-18 authorization 규격 확인
  - `dotnet build SMSR.slnx --no-restore --verbosity:minimal -p:OutputPath=<임시 경로>`
  - `<임시 경로>\SMSR.App.exe --oauth-self-test`
  - `<임시 경로>\SMSR.App.exe --codex-config-self-test`
  - 기존 SMSR GUI·STDIO 프로세스의 실행 경로 확인 후 종료, 기본 출력 재빌드와 SMSR 재실행
  - 실서버의 `/.well-known/oauth-authorization-server`와 인증 없는 `/mcp` 응답 확인
- 검증 결과:
  - 임시 출력 빌드가 경고 0, 오류 0으로 통과했다.
  - OAuth self-check가 `401 WWW-Authenticate` 챌린지, protected resource·authorization server metadata, DCR, loopback callback 포트 허용, PKCE S256 승인, authorization code 교환, OAuth 액세스 토큰을 사용한 MCP 초기화를 순서대로 통과했다.
  - Codex 설정 self-check가 다른 MCP 항목 보존, 기존 `smsr` 블록의 HTTP OAuth 전환, 백업과 중복 방지를 통과했다.
  - 액세스 토큰은 15분, 갱신 토큰은 30일로 제한하고 갱신 시 회전한다. 서버에는 토큰 원문 대신 해시를 DPAPI 암호화 상태 파일에 저장한다.
  - 기본 출력 빌드가 경고 0, 오류 0으로 통과했고 새 SMSR PID 70720이 포트 49783을 사용한다.
  - 실서버가 DCR·authorization·token endpoint 메타데이터와 `resource_metadata`·`smsr:mcp` scope가 포함된 `401 WWW-Authenticate`를 반환했다.
- 남은 위험:
  - 실행 중인 Codex는 설정을 다시 읽지 않으므로 완전 재시작 뒤 사용자가 `인증`과 SMSR의 `연결 승인`을 한 번 눌러야 한다.
  - 전체 레거시 `--self-test`는 WPF 실시간 모니터의 종료 대기 문제로 분리 검증보다 늦게 종료될 수 있다.
- 다음 조치:
  - 새 SMSR 서버를 실행한 상태에서 Codex를 재시작하고 `smsr` OAuth 인증을 승인한 뒤 `/mcp` 도구 목록을 확인한다.

## 2026-08-28 - OAuth 승인 반복 실패 재계획

- 변경 파일:
  - `src/SMSR.App/Mvp/OAuthAuditLog.cs`
  - `src/SMSR.App/Mvp/LocalServer.cs`, `LocalServerEndpoints.cs`
  - `src/SMSR.App/Mvp/OAuthRegistrationEndpoints.cs`, `OAuthAuthorizationEndpoints.cs`, `OAuthTokenEndpoints.cs`, `OAuthEndpoints.cs`
  - `src/SMSR.App/Mvp/OAuthSelfCheck.cs`
  - `docs/development-log.md`
- 변경 사유:
  - Codex의 `인증`과 SMSR의 `연결 승인` 이후 같은 실패가 3회 반복되어 추가 수동 재시도를 중단했다.
  - DPAPI OAuth 상태에는 Codex DCR 클라이언트 2개가 등록됐지만 액세스·갱신 토큰은 각각 0개였다. Codex 로그도 metadata GET과 DCR POST까지만 기록해 승인 요청, callback, token 교환 중 어느 단계에서 멈췄는지 기존 로그만으로 구분할 수 없다.
  - 통제 재현에서 승인 페이지의 `form-action 'self'` CSP가 SMSR 포트에서 Codex의 다른 loopback callback 포트로 이동하는 브라우저 리디렉션을 차단하는 원인을 확인했다.
- 실행 명령:
  - `~/.codex/logs_2.sqlite`의 `mcpServer/oauth/login` 요청 조회
  - `%LocalAppData%\SMSR\oauth-state.bin`을 현재 사용자 DPAPI로 복호화해 클라이언트·토큰 개수만 점검
  - `dotnet build SMSR.slnx --no-restore --verbosity:minimal`
  - `SMSR.App.exe --oauth-self-test`
  - 실서버 재실행 후 authorization server metadata와 인증 없는 `/mcp` 응답 확인
  - CSP callback origin 수정본을 임시 출력으로 빌드하고 `--oauth-self-test` 실행
  - 기존 SMSR PID 34900·66044 종료, 기본 출력 재빌드 및 SMSR 재실행
- 검증 결과:
  - 등록 callback은 `http://127.0.0.1:57894/callback`과 `http://127.0.0.1:61904/callback`으로 Codex의 loopback 형식과 일치한다.
  - 서버와 DCR까지는 정상이며 실패 범위는 authorization 요청 이후로 좁혀졌다.
  - query, authorization code, state, access/refresh token을 기록하지 않고 `register`, `authorize`, `consent`, `token`의 성공·거절 단계만 `%LocalAppData%\SMSR\logs\oauth.log`에 남기도록 했다.
  - Codex가 `scope` 또는 `resource`를 생략하면 로컬 MCP의 `smsr:mcp` scope와 resource audience를 적용하되, 명시된 잘못된 값은 계속 거부한다.
  - 기본 출력 빌드가 경고 0, 오류 0으로 통과했고 OAuth self-check가 종료 코드 0을 반환했다.
  - SMSR를 PID 34900으로 다시 실행했다. authorization server metadata가 정상이고 `/mcp`는 `resource_metadata`와 `smsr:mcp` scope가 포함된 401 challenge를 반환한다.
  - 통제 재현 로그는 `register accepted` → `authorize consent_shown` → `consent approved_redirected`까지 진행했지만 `token` 요청이 없었고, Codex callback listener 60832는 계속 대기했다. Edge 창도 `SMSR MCP 인증`에 머물러 브라우저 리디렉션 차단과 일치했다.
  - 검증된 callback URI의 origin을 승인 페이지 CSP `form-action`에 추가하고 self-check가 이를 검사하도록 했다.
  - 수정본 임시 빌드와 기본 빌드가 모두 경고 0, 오류 0으로 통과했고 OAuth self-check는 종료 코드 0을 반환했다.
  - 수정본 SMSR 하나만 PID 48320으로 실행했으며 포트 49783과 issuer-bound OAuth metadata를 확인했다.
  - 수정 후 실제 인증에서 `register accepted` → `authorize consent_shown` → `consent approved_redirected` → `token access_issued`가 순서대로 기록됐다.
  - DPAPI 상태에 액세스 토큰 1개와 갱신 토큰 1개가 생성됐고, Edge callback 화면에 `Authentication complete`가 표시됐다.
  - Codex 로그에서 `smsr`가 MCP 프로토콜 `2025-06-18`과 tools capability를 가진 `SMSR.App 1.0.0.0` 서버로 초기화된 것을 확인했다.
- 남은 위험:
  - Codex는 tools-only 서버에도 `resources/list`와 `resources/templates/list`를 조회해 미지원 경고를 남기지만 도구 연결과 인증에는 영향이 없다.
- 다음 조치:
  - 완료. 이후 SMSR가 종료된 경우 앱을 다시 실행하면 Codex가 저장된 OAuth 자격 증명으로 재연결한다.

## 2026-08-28 - 연결 완료 UI와 사용자 설정

- 변경 파일:
  - `src/SMSR.App/Mvp/LocalOAuthStore.cs`, `LocalServer.cs`, 관련 self-check
  - `src/SMSR.App/Services/LocalServerHost.cs`, `AppSettingsService.cs`
  - `src/SMSR.App/ViewModels/ServerControlViewModel.cs`, `SettingsViewModel.cs`, `MainWindowViewModel.cs`
  - `src/SMSR.App/Views/ServerPanel.xaml`, `SettingsPanel.xaml`, `MainWindow.xaml`과 code-behind
  - `src/SMSR.App/App.xaml.cs`, 플랫폼 동작 인터페이스·구현
  - `README.md`, `docs/mcp-connection.md`, `docs/development-log.md`
- 변경 사유:
  - OAuth 연결 완료 후에도 초기 연결과 연결 확인 버튼이 계속 보이는 혼동을 제거한다.
  - 서버 자동 시작, 닫기 버튼 동작, 데이터·로그 위치를 사용자가 앱에서 관리할 설정 화면을 제공한다.
- 실행 명령:
  - 임시 출력 경로로 `dotnet build SMSR.slnx --no-restore --verbosity:minimal`
  - 임시 빌드의 `SMSR.App.exe --oauth-self-test`
  - 임시 빌드의 `SMSR.App.exe --self-test`와 생성된 설정·OAuth·SQLite 상태 확인
- 검증 결과:
  - 임시 출력 빌드가 경고 0, 오류 0으로 통과했다.
  - OAuth self-check가 종료 코드 0을 반환했고 유효한 갱신 토큰 기반 연결 상태를 확인했다.
  - 전체 self-check가 OAuth, 설정 저장, 작업 복원, 내보내기까지 진행해 `StartServerAutomatically=false`, `MinimizeToTray=false` 설정 파일과 결과물을 생성했다.
- 남은 위험:
  - 전체 self-check 프로세스는 기존 실시간 모니터 종료 대기로 자동 종료되지 않아 결과 생성 확인 후 해당 임시 프로세스만 종료했다.
  - 설정의 서버 자동 시작 변경은 다음 앱 실행부터 적용된다.
- 다음 조치:
  - 기본 출력 빌드로 실제 앱을 재시작해 연결 완료 화면과 설정 탭을 육안 확인한다.

## 2026-08-28 - 순서도 대시보드, 탭 레이아웃, 테마 완성

- 변경 파일:
  - `src/SMSR.App/Mvp/Dashboard*.cs`, `WorkflowExportService.cs`, 서버 endpoint
  - `src/SMSR.App/Services/AppSettingsService.cs`, `AppThemeService.cs`, `LocalServerHost.cs`
  - `src/SMSR.App/Themes/*.xaml`, 설정·워크플로우·메인 창 XAML
  - `README.md`, `docs/mcp-connection.md`, `docs/development-log.md`
- 변경 사유:
  - 기존 `DashboardPage`가 참조 샘플을 연결하지 않고 `dependsOn`을 글자로만 표시하는 MVP 카드 그리드여서, 사용자가 제공한 3단 순서도 형식과 달랐다.
  - 넓힌 탭 헤더가 기본 `TabPanel`에서 잘리고 선택 강조가 약했으며, 초기 어두운 테마는 웹에만 적용되어 WPF 기본 컨트롤이 부분적으로 흰색·검정 텍스트를 유지했다.
- 실행 명령:
  - 기본 출력 `dotnet build src/SMSR.App/SMSR.App.csproj --nologo`
  - `SMSR.App.exe --oauth-self-test`, `SMSR.App.exe --codex-config-self-test`
  - 실제 앱 재시작 후 UI Automation으로 탭 경계·본문·테마 선택·설정 저장 확인
  - `/dashboard` HTML에서 3단 grid, SVG 노드·의존 간선, 밝은·어두운 팔레트 확인
  - 실행 창 캡처로 상단바, 편집형 ComboBox, CheckBox, 상태 행, 설정 카드 확인
- 검증 결과:
  - 기본 빌드가 경고 0, 오류 0으로 통과했고 OAuth·Codex 설정 self-check가 각각 종료 코드 0을 반환했다. OAuth self-check는 인증 뒤 `tools/list`에서 8개 SMSR 도구를 모두 확인한다.
  - 대시보드는 좌측 에이전트, 중앙 SVG 의존성 순서도, 우측 상세·최근 기록으로 렌더링하며 진단 계획 2개 노드와 간선 1개를 확인했다.
  - 앱과 웹 대시보드가 설정의 밝은·어두운 테마를 즉시 공유하고 `%LocalAppData%\SMSR\settings.json`에 저장한다. 내보낸 HTML에도 계획 그래프와 선택 테마가 포함된다.
  - 네 개 탭이 모두 창 경계 안에 표시되고 선택 탭의 강조·본문이 유지된다. 프로젝트·워크플로우 선택값과 설정의 체크박스·콤보·스크롤바도 테마 팔레트를 사용한다.
- 남은 위험:
  - 참조 샘플의 계층형 드릴다운, 에이전트 역할·heartbeat, 진행률 %, 재시도 횟수, 산출물·다음 조치는 현재 MCP 데이터 계약에 필드가 없어 아직 제공하지 않는다.
  - `plugins/smsr-codex` 선택형 스킬·라이프사이클 훅은 현재 사용자 Codex에 설치되어 있지 않고 기존 훅의 패키징 재검증이 필요하다. 기본 MCP 서버 지침과 도구에는 영향이 없다.
  - 전체 `--self-test`의 기존 실시간 모니터 종료 대기 문제는 별도 수정이 필요하다.
- 다음 조치:
  - 계층형 그래프 데이터 계약과 선택형 Codex 플러그인을 별도 작업으로 정리한 뒤 배포 패키지와 깨끗한 사용자 환경 설치 시험을 진행한다.

## 2026-08-28 - 에이전트 직접 전송 계약과 계층형 추적 완성

- 변경 파일:
  - `src/SMSR.App/Mvp/Contracts.cs`, `PlanContracts.cs`, `EventValidation.cs`, `AgentTools.cs`
  - `src/SMSR.App/Mvp/EventStore*.cs`, `EventMetadata.cs`, `EventPayload.cs`
  - `src/SMSR.App/Mvp/Dashboard*.cs`, `LocalServer*.cs`, `SmsrMcpInstructions.cs`
  - `src/SMSR.App/Mvp/TrackingContractSelfCheck.cs`, `OAuthSelfCheck.cs`, `MvpSelfCheck.cs`
  - `plugins/smsr-codex/**`, `.agents/plugins/marketplace.json`
  - `.gitignore`, `README.md`, `docs/mcp-connection.md`, `docs/smsr-codex-plugin.md`
  - Git 추적 제거: `src/SMSR.App/obj/**`
- 변경 사유:
  - SMSR이 에이전트를 호출하는 방향이 아니라 메인·하위 에이전트가 각자의 상태를 SMSR MCP로 직접 전송하도록 계약과 지침을 명확히 했다.
  - `parentNodeId`, 에이전트 역할, heartbeat, 진행률, 재시도 횟수, 다음 작업, 완료 조건, 산출물을 저장·조회·표시하고 계층형 SVG 그래프를 클릭해 드릴다운하도록 확장했다.
  - 기존 Node 기반 SessionStart 훅을 제거하고 연결된 `smsr` 서버의 `mcp_tool` 훅만 사용하는 선택형 플러그인으로 재작성했다.
  - Visual Studio·.NET 생성물을 추적하지 않도록 하고 이미 추적 중인 `obj` 파일을 인덱스에서 정리했다.
- 실행 명령:
  - 공식 OpenAI 플러그인·Codex Hooks 문서 확인
  - `dotnet build src/SMSR.App/SMSR.App.csproj -o <임시 경로> --no-restore --verbosity:minimal`
  - 임시 빌드의 `SMSR.App.exe --tracking-self-test`, `--oauth-self-test`, `--codex-config-self-test`
  - plugin-creator의 `read_marketplace_name.py`, `update_plugin_cachebuster.py`, `validate_plugin.py`
  - skill-creator의 `quick_validate.py`
  - `git rm -r --cached -- src/SMSR.App/obj`
  - 인앱 브라우저에서 로컬 대시보드 루트 노드 클릭 및 하위 그래프 DOM·화면 확인
- 검증 결과:
  - 프로젝트 빌드가 경고 0, 오류 0으로 통과했다.
  - 추적 계약, OAuth MCP 도구 9개, Codex 설정 self-check가 모두 종료 코드 0을 반환했다.
  - 기존 DB를 유지하면서 계획·현재 상태에 메타데이터 컬럼을 추가하고 `agent_heartbeats` 테이블을 만드는 마이그레이션을 추가했다.
  - 실제 화면에서 루트 `구현` 노드를 클릭하면 `데이터 계약` 하위 그래프, 역할, 60% 진행률, 재시도 2회, 다음 작업, 완료 조건, 산출물이 표시됨을 확인했다.
  - 플러그인과 스킬 validator가 통과했고 manifest 버전을 `0.1.0+codex.20260828084822`로 갱신했다. 훅 JSON에는 Node/npm/컴퓨터별 절대 경로가 없다.
  - 기본 `python` 명령으로 실행한 세 검증이 Microsoft Store 실행 별칭 때문에 같은 원인으로 실패해 즉시 재시도를 중단했다. Codex 번들 Python과 임시 `PyYAML` 폴더를 사용하는 새 계획으로 전환해 검증을 완료했다.
- 남은 위험:
  - 선택형 플러그인은 각 환경에서 로컬 마켓플레이스 위치를 선택하고 변경된 훅 해시를 `/hooks`에서 신뢰해야 한다. 기본 MCP 추적은 플러그인 없이도 서버 지침으로 동작한다.
  - 수정 후 브라우저 재로딩은 로컬 URL 보안 정책이 차단해 우회하지 않았다. 열 너비 수정본은 최종 빌드와 HTML 계약 self-check로 확인했다.
  - 기존 전체 `--self-test`의 SSE 모니터 종료 대기 문제는 이번 범위 밖이며, 독립된 세 self-check로 변경 범위를 검증했다.
- 다음 조치:
  - 다른 컴퓨터에서 저장소를 받은 뒤 SMSR OAuth 연결, 로컬 마켓플레이스 설치, 훅 신뢰, 새 task의 하위 에이전트 heartbeat까지 한 번 통합 확인한다.

## 2026-08-28 - 비공개 저장소와 마켓플레이스 없는 로컬 추적 전환

- 변경 파일:
  - 삭제: `.agents/plugins/marketplace.json`, `plugins/smsr-codex/**`, `docs/smsr-codex-plugin.md`
  - 추가: `.codex/hooks.json`, `.agents/skills/smsr-tracking/SKILL.md`, `docs/smsr-codex-local.md`
  - 수정: `README.md`, `docs/mcp-connection.md`, `docs/development-log.md`
- 변경 사유:
  - 공개 배포 목적이 없는 로컬 앱에 플러그인·마켓플레이스 패키징이 불필요하고 설치 의미를 혼동시켰다.
  - 공식 Codex 저장소 로컬 훅·스킬 위치를 사용해 절대경로나 별도 CLI 없이 동일한 MCP 직접 전송 기능을 유지한다.
- 실행 명령:
  - GitHub API로 `arscabar/SMSR` 저장소를 private으로 전환하고 결과를 재조회했다.
  - 공식 OpenAI Hooks·Skills 문서에서 `.codex/hooks.json`, `.agents/skills` 로딩 규칙을 확인했다.
  - JSON 구문·절대경로 검사와 `skill-creator`의 `quick_validate.py`를 실행했다.
  - 임시 출력 경로에서 .NET 빌드 후 `--tracking-self-test`, `--oauth-self-test`, `--codex-config-self-test`를 실행했다.
- 검증 결과:
  - GitHub 응답에서 `visibility=private`, `private=true`를 확인했다.
  - 사용자 Codex 설정에는 SMSR 로컬 마켓플레이스 등록이 없고 저장소 신뢰 설정만 존재한다.
  - 저장소 로컬 훅은 유효한 JSON이며 컴퓨터별 절대경로가 없고, `smsr-tracking` 스킬 검증을 통과했다.
  - 앱 빌드가 경고 0, 오류 0으로 통과했고 세 self-check가 모두 종료 코드 0을 반환했다.
- 남은 위험:
  - 저장소는 이전에 public이었으므로 제3자가 이미 복제한 사본까지 회수할 수는 없다.
  - 변경된 저장소 로컬 훅은 새 작업에서 `/hooks`를 열어 다시 신뢰해야 한다.
- 다음 조치:
  - 수정본을 커밋·푸시하고 다른 환경에서는 SMSR OAuth 연결과 훅 신뢰만 수행한다.

## 2026-08-28 - 실서버 MCP 재검증과 self-test 종료 교착 수정

- 변경 파일:
  - `src/SMSR.App/Services/LocalServerHost.cs`
  - `src/SMSR.App/ViewModels/WorkflowWorkspaceViewModel.cs`
  - `src/SMSR.App/Mvp/MvpSelfCheck.cs`
  - `docs/development-log.md`
- 변경 사유:
  - 전체 `--self-test`가 서버 중지 시 활성 SSE 연결의 종료를 기다리고, 모니터는 서버 중지 완료 알림을 기다리는 순서 역전 때문에 종료되지 않았다.
  - 실행 중인 SMSR가 최신 소스보다 오래되어 새 `record_heartbeat` 도구가 Codex 캐시에 반영되지 않은 상태를 실제 서버 기준으로 다시 확인할 필요가 있었다.
- 실행 명령:
  - 실행 중인 SMSR PID와 실행 경로 및 `127.0.0.1:49783` listener 확인
  - `dotnet build src/SMSR.App/SMSR.App.csproj --no-restore --verbosity:minimal`
  - 최신 SMSR 재실행 후 OAuth metadata 확인
  - DCR, PKCE 승인, token 교환, MCP `initialize`, `tools/list` 실서버 통합 검사
  - `SMSR.App.exe --codex-config-self-test`, `--tracking-self-test`, `--oauth-self-test`, `--self-test`
  - `.codex/hooks.json` JSON 구문과 5개 lifecycle 이벤트의 `smsr.record_lifecycle` 매핑 검사
- 검증 결과:
  - 서버 폐기 전에 `Stopping` 이벤트로 실시간 모니터의 SSE 연결을 먼저 취소하도록 종료 순서를 수정했다.
  - self-test의 서버 중지 대기에 5초 제한을 추가해 같은 회귀가 무한 대기 대신 명시적 실패로 드러나도록 했다.
  - 빌드는 경고 0, 오류 0으로 통과했다. 네 self-check는 각각 종료 코드 0이며 전체 `--self-test`는 5.84초에 종료됐다.
  - 최신 앱을 PID 73716으로 재실행했고 issuer `http://127.0.0.1:49783`, scope `smsr:mcp`를 확인했다.
  - 실서버 OAuth 전체 흐름과 MCP 도구 9개(`record_heartbeat` 포함)가 통과했다.
  - 저장소 훅 JSON은 유효하며 `SessionStart`, `UserPromptSubmit`, `Stop`, `SubagentStart`, `SubagentStop`이 모두 연결된 `smsr` 서버의 `record_lifecycle` 도구를 사용한다.
- 남은 위험:
  - 현재 실행 중인 Codex 작업의 MCP sidebar 캐시는 이전 8개 도구 목록이며 이 작업 ID에 lifecycle 데이터가 없다. 앱 자체 문제는 해소됐지만 Codex가 서버 목록과 변경된 저장소 훅을 다시 읽어야 한다.
  - 저장소 훅은 Codex의 `/hooks`에서 사용자가 직접 신뢰해야 하며, 현재 작업을 실행 중인 상태에서 자동 승인할 수 없다.
- 다음 조치:
  - Codex를 재시작하거나 새 작업을 연 뒤 `/mcp`에서 `record_heartbeat` 포함 9개 도구를 확인하고, `/hooks`에서 저장소 훅을 신뢰한 다음 프롬프트 1회를 보내 SMSR lifecycle 표시를 확인한다.

## 2026-08-28 - Codex 재시작 시 SMSR 동반 종료 원인 확인

- 변경 파일:
  - 사용자 설정: `%USERPROFILE%/.codex/config.toml`
  - `docs/development-log.md`
- 변경 사유:
  - SMSR 서버를 켠 뒤 Codex를 재시작해도 `/mcp` 도구가 나타나지 않는 현상이 반복됐다.
  - Codex 로그는 매 재시작마다 `127.0.0.1:49783` 연결 거부와 `smsr` 도구 0개를 기록했다.
- 실행 명령:
  - Codex 재시작 직후 ChatGPT·SMSR 프로세스와 port 49783 listener 확인
  - `~/.codex/logs_2.sqlite`에서 `server_name=smsr` 연결 로그와 MCP catalog 확인
  - Explorer shell을 통해 `SMSR.App.exe` 독립 실행 후 부모 PID와 listener 확인
  - `[mcp_servers.smsr]`의 지원 옵션 `enabled`를 false에서 true로 전환해 설정 새로고침 시도
- 검증 결과:
  - 이전 SMSR는 Codex 도구 실행 프로세스의 자식이라 Codex 종료 시 함께 종료됐다. 따라서 재시작된 Codex는 정상 서버가 아닌 닫힌 포트에 접속하고 있었다.
  - SMSR를 Explorer 소유 프로세스 PID 30608로 다시 실행했으며 부모 프로세스는 `explorer`, port 49783 listener는 정상이다.
  - 현재 Codex Desktop은 실행 중 설정 변경을 즉시 다시 읽지 않아 false/true 전환만으로는 현재 작업의 도구 catalog가 갱신되지 않았다. 최종 설정은 `enabled = true`로 유지했다.
  - 독립 실행 후 Codex 재시작에서 `SMSR.App 1.0.0.0` 초기화와 `record_heartbeat` 포함 MCP 도구 9개가 확인됐고 lifecycle agent가 기록됐다.
  - 완료 이벤트에 `COMPLETED`를 사용한 동일 계약 오류가 4건 발생해 재시도를 중단했다. `EventValidation`의 허용 상태가 `PENDING`, `IN_PROGRESS`, `VALIDATING`, `SUCCESS`, `FAILED`, `RETRYING`, `BLOCKED`임을 확인하고 완료 상태를 `SUCCESS`로 수정하는 계획으로 전환했다.
  - `SUCCESS`로 수정한 계획 노드 4개가 모두 중복 없이 기록됐고, coordinator heartbeat `STOPPED`와 `get_state` 왕복 조회까지 통과했다.
- 남은 위험:
  - 현재 Codex 작업은 시작 시 만들어진 `smsr` unavailable 상태를 유지하므로 독립 SMSR가 실행된 상태에서 Codex 프로세스를 한 번 새로 시작해야 한다.
- 다음 조치:
  - 완료. SMSR 앱을 종료하지 않는 동안 Codex가 저장된 OAuth 자격 증명으로 9개 도구에 재연결한다.

## 2026-08-28 - Codex MCP 원클릭 설정과 실제 연결 표시

- 변경 파일:
  - `src/SMSR.App/Services/CodexConnectionService.cs`, `CodexMcpConfig.cs`, `WindowsStartupRegistration.cs`, `LocalServerHost.cs`
  - `src/SMSR.App/Mvp/LocalServer.cs`, `LocalServerEndpoints.cs`, `McpConnectionTracker.cs`, `MvpSelfCheck.cs`
  - `src/SMSR.App/ViewModels/ServerControlViewModel.cs`, `ServerControlViewModel.Codex.cs`, `SettingsViewModel.cs`, `SettingsViewModel.Startup.cs`
  - `src/SMSR.App/Views/ServerPanel.xaml`, `SettingsGeneralPanel.xaml`, `App.xaml.cs`
  - `README.md`, `docs/mcp-connection.md`
- 변경 사유:
  - MCP 설정, 서버 생존, OAuth 승인, 연결 확인이 여러 단계로 분리돼 반복 설정이 필요했고 토큰 보유 상태가 실제 MCP 연결처럼 표시됐다.
  - 사용자가 한 버튼으로 자동화 가능한 설정을 모두 적용하고 실제 인증 요청을 받은 뒤에만 도구 연결 상태를 표시할 필요가 있었다.
- 실행 명령:
  - `dotnet build src/SMSR.App/SMSR.App.csproj -o <임시 경로> --no-restore --verbosity:minimal`
  - 임시 빌드의 `SMSR.App.exe --codex-config-self-test`, `--tracking-self-test`, `--oauth-self-test`, `--self-test`
  - 실행 중인 기존 SMSR의 경로와 PID를 확인한 뒤 기본 출력 빌드로 교체하고 Explorer를 통해 독립 실행
  - UI Automation으로 `Codex 연결 한 번에 설정`을 호출하고 Windows 자동 시작, 설정 파일, 연결 완료 화면 확인
- 검증 결과:
  - 빌드가 경고 0, 오류 0으로 통과했다.
  - 네 self-check가 모두 종료 코드 0을 반환했고 OAuth MCP `tools/list`에서 9개 도구를 검증했다.
  - 한 번에 설정이 서버 시작, 현재 실행 파일의 Windows 로그인 자동 시작, 앱 시작 시 서버 시작, `auth = "oauth"`, `enabled = true` 등록을 함께 수행한다.
  - 인증된 `/mcp` 요청이 실제로 들어온 뒤에만 `Codex 연결됨 · 도구 9개` 상태로 전환한다.
  - 새 앱은 Explorer 소유 PID 7964로 port 49783을 유지하며 현재 실행 파일의 `--background` 자동 시작 등록이 생성됐다.
  - 현재 Codex 작업의 `get_state` 실호출 후 연결 완료 문구가 표시되고 원클릭 설정 버튼이 숨겨지는 것을 확인했다.
- 남은 위험:
  - Codex가 실행 중 설정을 다시 읽지 않는 버전에서는 최초 등록 후 Codex를 한 번 다시 열어야 한다.
  - OAuth 동의와 저장소 훅 신뢰는 보안 경계이므로 사용자가 직접 승인해야 한다.
- 다음 조치:
  - 완료. 다른 컴퓨터에서는 그 컴퓨터에서 한 번에 설정 버튼과 최초 OAuth 승인만 수행한다.

## 2026-08-31 - 무지정 Codex 자동 설정과 전역 작업 추적

- 변경 파일:
  - 추가: `src/SMSR.App/Services/CodexAutoTrackingHook.cs`, `CodexAutoTrackingHook.Definitions.cs`, `CodexAutoTrackingContext.cs`
  - 수정: `src/SMSR.App/App.xaml.cs`, `Services/CodexConnectionService.cs`, `Services/AppSettingsService.cs`, `Services/CodexMcpConfigSelfCheck.cs`
  - 수정: `ViewModels/SettingsViewModel.cs`, `SettingsViewModel.Startup.cs`, `ServerControlViewModel.Codex.cs`, 관련 XAML과 문서
  - 삭제: 저장소별 중복 신뢰를 요구하던 `.codex/hooks.json`
- 변경 사유:
  - 사용자가 매 컴퓨터·저장소·작업에서 연결 버튼과 `$smsr-tracking`을 지정하지 않아도 SMSR 연결과 의미 기반 추적이 자동 적용돼야 했다.
  - 저장소 훅과 전역 훅이 동시에 실행되는 중복 및 저장소별 반복 신뢰를 제거할 필요가 있었다.
- 실행 명령:
  - 공식 OpenAI MCP·Hooks 문서에서 공유 `config.toml`, 전역 `~/.codex/hooks.json`, `UserPromptSubmit` 추가 컨텍스트와 MCP tool hook 동작 확인
  - `dotnet build src/SMSR.App/SMSR.App.csproj -o <임시 경로> --no-restore --verbosity:minimal`
  - 임시 앱의 `--smsr-auto-track-hook` 표준 입출력 검사
  - 임시 앱의 `--codex-config-self-test`, `--tracking-self-test`, `--oauth-self-test`, `--self-test`
- 검증 결과:
  - 빌드는 경고 0, 오류 0이며 네 self-check가 모두 종료 코드 0을 반환했다.
  - 자동 추적 훅이 기존 전역 훅을 보존하고 SMSR 소유 항목만 병합하며 재등록 시 중복되지 않음을 확인했다.
  - 훅 컨텍스트가 프로젝트 폴더명과 세션 ID를 제공하고 테스트 프롬프트 원문은 출력하지 않음을 확인했다.
  - SMSR 실행 시 서버·Windows 자동 시작·MCP 설정·전역 lifecycle 및 추적 컨텍스트 훅을 자동 복구하도록 변경했다.
  - 기본 출력 앱을 Explorer 소유 PID 18832로 교체했고 `~/.codex/hooks.json`에 SMSR 전역 훅 5개와 컨텍스트 명령 1개가 생성됐다.
  - 실사용 실행 파일의 훅 표준입출력에서 세션·프로젝트 ID를 확인했으며 테스트 프롬프트 원문은 포함되지 않았다.
- 남은 위험:
  - OAuth 승인과 비관리 전역 훅의 최초 신뢰는 Codex 보안 경계이므로 사용자 확인을 우회할 수 없다.
  - 모델이 의미 기반 계획·상태를 생성하므로 단순 lifecycle은 훅이 보장하지만 세부 계획 품질은 작업 문맥과 모델 판단에 영향을 받는다.
- 다음 조치:
  - Codex를 한 번 다시 열어 사용자 전역 SMSR 훅을 신뢰한 뒤, 새 작업의 무지정 계획·heartbeat·상태 전송을 확인한다.

## 2026-08-31 - 경로 독립형 Codex 연동과 휴대용 Windows 배포

- 변경 파일:
  - `src/SMSR.App/Services/CodexDesktopLocator.cs`, `CodexConnectionService.cs`, `App.xaml.cs`
  - `src/SMSR.App/Properties/PublishProfiles/Portable.pubxml`
  - `scripts/publish-portable.ps1`, `scripts/test-portable.ps1`
  - `README.md`, `docs/portable-quickstart.md`
- 변경 사유:
  - 저장소의 Debug 출력이나 특정 PC의 Codex Store 패키지 탐지에 의존하지 않고 다른 폴더·저장소·Windows PC에서도 SMSR을 실행하고 자동 설정할 수 있어야 했다.
  - Codex 데스크톱·CLI·IDE가 같은 호스트에서 공유하는 사용자 `config.toml`을 기준으로 연동하고, 대상 PC에 .NET SDK가 없어도 실행되는 배포물이 필요했다.
- 실행 명령:
  - 공식 OpenAI MCP·Hooks 문서에서 사용자 공유 MCP 설정, 전역 훅 위치와 훅 해시별 신뢰 동작 확인
  - 제품 코드·스크립트의 개발 PC 절대경로 검색과 `git diff --check`
  - `dotnet build SMSR.slnx --configuration Release --no-restore --nologo`
  - `powershell -ExecutionPolicy Bypass -File .\scripts\publish-portable.ps1 -Runtime win-x64`
  - `powershell -ExecutionPolicy Bypass -File .\scripts\test-portable.ps1`
- 검증 결과:
  - 제품 코드와 배포 설정에서 `C:\gitsource\SMSR`, 개발 사용자명, Debug 출력 경로 하드코딩이 발견되지 않았다.
  - Microsoft Store판 Codex 탐지가 실패해도 현재 사용자의 표준 `~/.codex/config.toml`과 `hooks.json`을 구성하도록 변경했다.
  - Release 빌드가 경고 0, 오류 0으로 통과했다. 실행 중인 기존 Debug 앱 잠금 때문에 Debug 기본 출력 빌드는 실패했으나 Release 출력과 배포에는 영향이 없었다.
  - `SMSR-win-x64-20260831-102924.zip`을 생성했고 SHA-256은 `5F72391F4304CEFF0C3AA065BA78FDB9D762B8305C63918520A4E9EF027F936C`이다.
  - 저장소 밖 임시 폴더의 자체 포함형 EXE에서 config, tracking, OAuth, 전체 self-check가 모두 종료 코드 0을 반환했고 자동 훅이 세션 ID를 출력하되 테스트 프롬프트는 제외했다.
- 남은 위험:
  - WPF 앱이므로 Windows 전용이다. macOS·Linux 지원은 UI 프레임워크 교체가 필요한 별도 작업이다.
  - OAuth 자격증명과 비관리 훅 신뢰는 Windows 사용자별 보안 상태이므로 새 PC·새 사용자에서는 한 번 직접 승인해야 한다.
  - 배포본은 코드 서명되지 않아 새 PC에서 SmartScreen 경고가 나타날 수 있다. ARM64 생성 경로는 제공하지만 실제 장치 실행 검증은 하지 않았다.
- 다음 조치:
  - 정식 배포가 필요해지면 코드 서명과 설치 관리자 또는 릴리스 자동화를 추가하고, 깨끗한 Windows x64 PC에서 최초 OAuth·훅 승인까지 수동 수용 시험한다.

## 2026-08-31 - 다른 PC용 Windows 설치 프로그램

- 변경 파일:
  - `installer/SMSR.iss`, `installer/SMSR-Setup.ico`
  - `scripts/build-installer.ps1`, `docs/installer-quickstart.md`, `README.md`
  - `src/SMSR.App/App.xaml.cs`, `SMSR.App.csproj`, `Assets/SMSR.ico`
  - `src/SMSR.App/Services/CodexIntegrationCleanup.cs`, `CodexMcpConfig.Unregister.cs`, `CodexAutoTrackingHook.Unregister.cs`, `CodexMcpConfigSelfCheck.cs`
- 변경 사유:
  - 다른 Windows PC에는 ZIP이나 개발 환경이 아니라 단일 설치 프로그램만 전달하고, 안정된 설치 경로에서 설치·업그레이드·제거가 가능해야 했다.
  - 제거 후 삭제된 EXE를 가리키는 Windows 자동 시작과 Codex MCP·전역 훅이 남지 않도록 SMSR 소유 설정만 정리할 필요가 있었다.
- 실행 명령:
  - 공식 Inno Setup 문서에서 비관리 설치, 단일 Setup EXE, 아키텍처, 앱 종료, 설치·제거 순서 확인
  - `winget install --id JRSoftware.InnoSetup -e --source winget --scope user`
  - `dotnet build SMSR.slnx --configuration Release --no-restore --nologo`
  - `powershell -ExecutionPolicy Bypass -File .\scripts\build-installer.ps1`
  - 생성된 Setup의 `/VERYSILENT /SUPPRESSMSGBOXES /NORESTART` 설치·제거·재설치 및 `/FORCECLOSEAPPLICATIONS` 업그레이드 검사
- 검증 결과:
  - Release 빌드가 경고 0, 오류 0이며 config 제거 self-check와 설치된 앱의 config, tracking, OAuth, 전체 self-check가 모두 종료 코드 0을 반환했다.
  - Inno Setup 6.7.3에서 `SMSR-Setup-1.0.0.0-win-x64.exe` 단일 파일을 생성했다. 최종 SHA-256은 `3E3A0B1EED10106DD9BAB502C762485809A15DF3B3D333D5E3ED4F396C999DA1`이다.
  - 실제 현재 사용자 설치에서 `%LOCALAPPDATA%\Programs\SMSR`, 시작 메뉴, HKCU 자동 시작, 앱 및 기능 제거 항목 생성을 확인했다.
  - 제거 시 설치 폴더·자동 시작·제거 항목·SMSR Codex MCP·훅이 사라지고 다른 Codex 설정과 `%LOCALAPPDATA%\SMSR` 데이터는 유지됐다.
  - 최종 재설치와 실행 중 업그레이드에서 설치 EXE가 publish EXE와 같은 해시로 교체됐고 설치 경로 앱이 port 49783을 유지하며 MCP가 재연결됐다.
  - 기존 230px 단일 ICO를 Windows 표준 16·32·48·64·256px 다중 해상도 ICO로 교체해 앱·트레이·설치 프로그램 아이콘을 검증했다.
  - 첫 컴파일의 Inno 7 전용 옵션과 두 번째 컴파일의 비표준 ICO 실패는 원인이 달라 각각 6.7 호환 명령과 표준 ICO로 전환했다. 일반 종료가 트레이 숨김으로 처리되는 업그레이드 문제는 설치 시 강제 종료로 해결했다.
- 남은 위험:
  - 설치 파일은 Authenticode 코드 서명되지 않아 다른 PC에서 Windows SmartScreen의 알 수 없는 게시자 경고가 나타날 수 있다.
  - 최초 OAuth 승인과 변경된 설치 경로의 전역 훅 신뢰는 대상 Windows 사용자마다 한 번 필요하다.
  - 현재 설치 프로그램은 win-x64용이며 깨끗한 별도 PC에서의 최종 사용자 수용 시험은 남아 있다.
- 다음 조치:
  - 코드 서명 인증서가 준비되면 앱과 Setup EXE 서명을 빌드 단계에 연결하고, 필요하면 GitHub Release에 설치 파일과 SHA-256을 게시한다.

## 2026-08-31 - SMSR 설치 Wizard 브랜드 UI

- 변경 파일:
  - `installer/SMSR.iss`, `installer/SMSR.UI.iss`, `installer/SMSR-Wizard.png`
  - `README.md`, `docs/installer-quickstart.md`
- 변경 사유:
  - 검증된 Inno Setup 설치 엔진과 설치 계약을 유지하면서 다른 PC 사용자에게 일관된 SMSR 브랜드 경험을 제공할 필요가 있었다.
- 실행 명령:
  - `powershell -ExecutionPolicy Bypass -File .\scripts\build-installer.ps1`
  - 최종 Setup의 무인 업그레이드와 설치 경로 앱 self-check 4종 실행
  - 실제 한국어 Wizard 창 캡처와 레이아웃 확인
- 검증 결과:
  - Inno Setup 6.7.3에서 동적 polar 라이트·다크 스타일, 브랜드 세로 배너와 헤더 아이콘, 한·영 환영·완료 문구가 정상 컴파일됐다.
  - 1018x773 실제 다크 모드 Wizard 캡처에서 텍스트·배너·버튼 잘림이 없었다.
  - 최종 Setup 업그레이드 종료 코드 0, self-check 4종 종료 코드 0, 127.0.0.1:49783 리스너와 MCP 재연결을 확인했다.
  - 최종 파일은 `SMSR-Setup-1.0.0.0-win-x64.exe`, SHA-256 `14DAA76505229F015531B4FC929B87734C77B2E9B3A9945312CD0C3B62AFF22B`이다.
- 남은 위험:
  - 라이트 모드는 Inno Setup 내장 동적 스타일로 제공되지만 이번 실제 화면 캡처는 현재 Windows 다크 모드에서 수행했다.
  - Setup은 여전히 Authenticode 미서명이라 다른 PC에서 SmartScreen 경고가 나타날 수 있다.
- 다음 조치:
  - 별도 PC 수용 시험에서 라이트·다크 화면과 최초 OAuth·훅 승인 흐름을 확인하고, 코드 서명 인증서가 준비되면 배포 파일에 서명한다.
