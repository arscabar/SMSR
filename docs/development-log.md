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
