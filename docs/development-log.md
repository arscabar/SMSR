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

## 2026-08-31 - 그래프 더블클릭 흐림과 중복 이동 수정

- 변경 파일:
  - `src/SMSR.App/Mvp/DashboardPage.cs`, `DashboardLiveUpdates.cs`, `DashboardGraphStyles.cs`
  - `src/SMSR.App/Mvp/MvpSelfCheck.cs`
- 변경 사유:
  - 웹 대시보드의 2초 `meta refresh`가 전체 화면을 반복해서 다시 그렸고, SVG 링크 더블클릭의 두 번째 클릭이 재렌더링된 다른 노드에 전달될 수 있었다.
- 실행 명령:
  - Release 빌드와 config·tracking·OAuth·전체 self-check 실행
  - 설치 프로그램 재빌드와 현재 설치본 무인 업그레이드
- 검증 결과:
  - 2초 전체 새로고침을 제거하고 기존 `/api/events/stream`의 상태 이벤트로 헤더·그래프·상세 영역만 교체하도록 변경했다.
  - 그래프 텍스트 선택을 막고 `sessionStorage` 기반 600ms 중복 이동 잠금과 더블클릭 기본 동작 차단을 추가했다.
  - self-check에서 `meta refresh` 부재, `EventSource`와 중복 클릭 잠금 포함을 회귀 검사한다.
  - Release 빌드 경고 0·오류 0, 소스 및 설치 앱 self-check 4종이 모두 종료 코드 0을 반환했다.
  - 첫 수정본에서 SVG `<a>`의 `href`를 일반 문자열로 취급해 `[object SVGAnimatedString]`으로 이동하는 회귀가 발견됐다. `getAttribute('href')`로 실제 URL을 읽도록 수정하고 self-check에 해당 계약을 추가했다.
  - 최종 Setup SHA-256은 `61C7456074FDE5D1FD29BF89167ABE380D13EED26265B8C910E6CEFEC5279564`이다.
- 남은 위험:
  - 설치본 교체 후 로컬 URL 재방문이 브라우저 안전 정책에 차단되어 새 설치본의 실제 더블클릭 자동화 재현은 수행하지 못했다. 기존 화면에서 원인과 잘못된 이동을 재현했고 변경된 HTML 계약은 설치 앱 self-check로 검증했다.
- 다음 조치:
  - 현재 열린 대시보드를 한 번 새로고침하고 동일 노드를 더블클릭해 화면이 유지되는지 수동 확인한다.

## 2026-08-31 - 그래프 안내 문구 제거

- 변경 파일:
  - `src/SMSR.App/Mvp/DashboardPage.cs`
- 변경 사유:
  - 계층형 그래프 상단의 선행 관계·드릴다운 설명이 불필요해 화면에서 제거했다.
- 실행 명령:
  - Release 빌드와 전체 self-check 실행
- 검증 결과:
  - 빌드 경고 0·오류 0, 전체 self-check 종료 코드 0을 확인했다.
- 남은 위험:
  - 없음.
- 다음 조치:
  - 설치본 갱신 후 대시보드를 새로고침한다.

## 2026-08-31 - 설치본 설정 보존·완전 초기화 실제 검증

- 변경 파일:
  - `docs/development-log.md`
- 변경 사유:
  - 일반 재설치와 사용자 데이터까지 지운 완전 초기화에서 설치본이 설정·인증·Codex 연동을 어떻게 처리하는지 실제 설치 환경으로 확인했다.
- 실행 명령:
  - 현재 사용자 무인 제거·재설치, 설치 앱 실행, 데이터 파일 SHA-256 비교
  - 사용자 데이터 격리 후 무인 재설치, 신규 데이터 생성 확인
  - 원본 데이터·Codex 설정 복원 후 설치 앱 self-check 4종 실행
- 검증 결과:
  - 일반 제거는 설치 폴더와 SMSR 소유 자동 시작·MCP·훅만 제거했고 `%LOCALAPPDATA%\SMSR`의 27개 파일은 해시 차이 없이 보존했다.
  - 일반 재설치 후 앱 첫 실행이 `~/.codex/config.toml`의 SMSR MCP와 `~/.codex/hooks.json`의 전역 훅을 현재 PC의 설치 경로로 다시 생성했다.
  - 완전 초기화에서는 `smsr.db`와 `logs/activity.log`만 새로 생성됐고 기본 설정은 파일 없이 메모리 기본값으로 적용됐다. OAuth 상태와 MCP 토큰은 생성되지 않아 새 PC처럼 최초 인증이 필요했다.
  - 원본 `settings.json`, `mcp-token.bin`, `oauth-state.bin`, Codex config·hooks를 해시 일치 상태로 복원했다.
  - 자동 시작은 시작프로그램 바로가기가 아니라 `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`의 `SMSR` 값으로 설치 실행 파일과 `--background`가 정상 등록됐다.
  - 설치 앱의 config·tracking·OAuth·전체 self-check 4종이 모두 통과했고 설치 실행 파일이 127.0.0.1:49783을 수신 중이다.
- 남은 위험:
  - 완전 초기화나 새 PC에서는 보안상 OAuth 동의와 Codex의 비관리 훅 최초 신뢰를 사용자가 한 번 승인해야 한다.
  - 복구용 테스트 사본은 안전 정책상 `%TEMP%\SMSR-reset-test-55cc5270819e4b61a536602c12ee6d59`에 보존했다.
- 다음 조치:
  - 별도 PC에서 설치 후 최초 OAuth·훅 승인까지 한 차례 수용 시험한다.

## 2026-08-31 - 작업 그래프를 명시적 요청 범위로 제한

- 변경 파일:
  - `src/SMSR.App/Services/CodexAutoTrackingContext.cs`, `CodexConnectionService.cs`, `CodexMcpConfigSelfCheck.cs`
  - `src/SMSR.App/Mvp/SmsrMcpInstructions.cs`, `TrackingContractSelfCheck.cs`
  - `src/SMSR.App/Views/ServerPanel.xaml`, `SettingsGeneralPanel.xaml`, `ViewModels/ServerControlViewModel.Codex.cs`
  - `.agents/skills/smsr-tracking/SKILL.md`, `README.md`, `docs/mcp-connection.md`, `docs/smsr-codex-local.md`, `docs/installer-quickstart.md`
- 변경 사유:
  - 모든 실질 작업에 계획 그래프를 자동 생성하지 않고 사용자가 그래프 추적을 명시적으로 요청한 작업만 시각화해야 했다.
- 실행 명령:
  - Release 빌드, config·tracking·OAuth·전체 self-check 실행
  - 설치 프로그램 재빌드, 현재 사용자 무인 업그레이드, 설치 앱 self-check 4종 실행
- 검증 결과:
  - 일반 요청에서는 `save_plan`, `record_heartbeat`, `record_event`를 호출하지 않고 lifecycle만 기록하도록 훅 컨텍스트와 MCP 초기화 지침을 변경했다.
  - 그래프 요청 이후에는 관련 후속 턴을 같은 워크플로우로 유지하고 SUCCESS·FAILED·BLOCKED 최종 이벤트 후 heartbeat를 끝내며 이후 무관한 요청을 붙이지 않도록 계약을 고정했다.
  - 앱 상태와 설정 화면을 `요청형 그래프` 용어로 통일하고 tracking self-check에서 해당 MCP 지침을 회귀 검사한다.
  - 소스와 설치 앱 모두 빌드 경고 0·오류 0 및 self-check 4종 통과를 확인했다.
  - 설치 앱이 `C:\Users\pkm11\AppData\Local\Programs\SMSR\SMSR.App.exe`에서 127.0.0.1:49783을 수신 중이다.
  - 최종 Setup SHA-256은 `DCAE192D53192EAFF4875E078BAF0B206A67EE521B960C3A754E0DDD90875458`이다.
- 남은 위험:
  - 그래프 요청 여부는 에이전트가 사용자 표현의 의미를 판단하므로 모호한 표현보다 `이 작업을 그래프로 추적해줘`처럼 명시하는 것이 가장 확실하다.
  - 기존에 저장된 그래프 데이터는 삭제하지 않으며 변경된 규칙은 이후 요청부터 적용된다.
- 다음 조치:
  - 새 Codex 작업에서 일반 요청과 명시적 그래프 요청을 각각 한 번 실행해 대시보드 생성 차이를 수용 확인한다.

## 2026-08-31 - 상시 lifecycle 제거와 이전 그래프 불러오기

- 변경 파일:
  - `src/SMSR.App/Services/CodexAutoTrackingHook.cs`, `CodexAutoTrackingHook.Definitions.cs`, `CodexAutoTrackingHook.Unregister.cs`, `CodexAutoTrackingContext.cs`, `CodexMcpConfigSelfCheck.cs`
  - `src/SMSR.App/Mvp/PlanTools.cs`, `PlanContracts.cs`, `EventStoreWorkflowCatalog.cs`, `SmsrMcpInstructions.cs`
  - `src/SMSR.App/Mvp/MvpSelfCheck.cs`, `OAuthSelfCheck.cs`, `TrackingContractSelfCheck.cs`
  - `src/SMSR.App/Views/ServerPanel.xaml`, `.agents/skills/smsr-tracking/SKILL.md`, `README.md`, 관련 안내 문서
- 변경 사유:
  - 그래프를 요청하지 않은 일반 작업의 세션·turn·에이전트 lifecycle은 활용 대상이 없으므로 SMSR에 저장하지 않아야 했다.
  - 사용자가 기존 그래프를 다시 사용하려 할 때 에이전트가 이전 workflowId를 발견하고 계획·상태를 불러올 수 있어야 했다.
- 실행 명령:
  - Release 빌드와 config·tracking·OAuth·전체 self-check 실행
  - 설치 프로그램 재빌드, 현재 사용자 무인 업그레이드, 설치 앱 self-check 4종 실행
  - 실제 `~/.codex/hooks.json`의 SMSR 소유 훅과 `record_lifecycle` 잔존 여부 검사
- 검증 결과:
  - SessionStart·Stop·SubagentStart·SubagentStop의 SMSR MCP 훅과 `record_lifecycle` 도구를 제거했다.
  - `UserPromptSubmit` 훅 하나만 남겨 프로젝트·task ID와 요청형 그래프 규칙을 에이전트 컨텍스트에 제공하며 DB에는 기록하지 않는다.
  - 기존 9개 도구 수를 유지하면서 `record_lifecycle` 자리를 `list_workflows`로 교체했다.
  - `list_workflows`는 기존 그래프를 최근 활동 순으로 workflowId·노드 수·ACTIVE/TERMINAL 상태와 함께 반환하고, 선택 후 `get_plan`·`get_state`로 이어갈 수 있다.
  - 소스와 설치 앱의 self-check 4종이 모두 통과했다. 실제 사용자 훅은 SMSR marker 1개, `record_lifecycle` 없음, 컨텍스트 명령 있음으로 확인됐다.
  - 설치 앱은 `C:\Users\pkm11\AppData\Local\Programs\SMSR\SMSR.App.exe`에서 127.0.0.1:49783을 수신 중이다.
  - 최종 Setup SHA-256은 `D3A1E4AA52DE0E3B3E7E33997D0FA7E69998AD726FB0FD77ABF8182FAD8EA7F0`이다.
- 남은 위험:
  - 후보 그래프가 여러 개이고 사용자의 지칭이 모호하면 에이전트가 임의 선택하지 않고 workflowId 선택을 요청한다.
  - 기존에 저장된 lifecycle 전용 워크플로우 데이터는 호환성과 복구 가능성을 위해 자동 삭제하지 않았다.
- 다음 조치:
  - 실제 Codex 요청에서 `이전 그래프를 불러와 이어서 추적해줘`를 실행해 후보 선택과 재개 흐름을 수용 확인한다.

## 2026-08-31 - 요청형 그래프 사용자 문서 정리

- 변경 파일:
  - `docs/graph-tracking-guide.md`, `README.md`
  - `docs/mcp-connection.md`, `docs/smsr-codex-local.md`, `docs/portable-quickstart.md`
  - `docs/wpf-mcp-dashboard-project-plan.md`, `docs/wpf-mcp-dashboard-project-plan.html`
- 변경 사유:
  - 요청형 그래프 시작·종료와 이전 workflow 불러오기 절차가 여러 문서에 흩어져 있어 변경된 동작을 바로 확인하기 어려웠다.
- 실행 명령:
  - 저장소 전체 Markdown·HTML의 lifecycle·MCP 도구 명칭 검색, `git diff --check`
- 검증 결과:
  - 새 그래프 요청 문구, 종료 조건, `list_workflows` 조회와 `get_plan`·`get_state` 재개 절차를 전용 안내서로 정리했다.
  - README와 휴대용·MCP 문서에서 전용 안내서를 연결했다.
  - 최초 프로젝트 계획서는 설계 이력임을 표시하고 MCP 도구 목록과 요약 도구 명칭을 현재 계약으로 갱신했다.
- 남은 위험:
  - 최초 계획서 본문의 과거 일정·구현 전략은 역사적 기록으로 유지한다.
- 다음 조치:
  - 없음.

## 2026-08-31 - 시스템 트레이 제어 메뉴 확장

- 변경 파일:
  - `src/SMSR.App/Infrastructure/TrayStatusIcon.cs`, `TrayMenuState.cs`
  - `src/SMSR.App/App.xaml.cs`, `Views/MainWindow.xaml`, `MainWindow.xaml.cs`
  - `src/SMSR.App/Mvp/MvpSelfCheck.cs`, `README.md`, 설치·설계 문서
- 변경 사유:
  - 트레이 아이콘에서 앱 열기와 종료 외에도 일상적인 서버·대시보드·설정 작업을 바로 수행할 필요가 있었다.
- 실행 명령:
  - Release 빌드와 config·tracking·OAuth·전체 self-check 실행
  - 설치 프로그램 재빌드, 현재 사용자 무인 업그레이드, 설치 앱 self-check 4종 실행
- 검증 결과:
  - 트레이 메뉴에 서버·Codex 상태, SMSR 열기, 현재 대시보드 열기, 서버 시작·중지, 설정 열기, 완전 종료를 추가했다.
  - 선택된 워크플로우와 서버 실행 상태에 맞춰 대시보드·시작·중지 메뉴의 활성 상태가 갱신된다.
  - 설정 열기는 메인 창을 복원하고 설정 탭을 바로 선택하며 더블클릭은 기존처럼 창을 복원한다.
  - 빌드 경고 0·오류 0, 소스와 설치 앱 self-check 4종 통과, 설치 앱의 127.0.0.1:49783 수신을 확인했다.
  - 최종 Setup SHA-256은 `2D3C5B3EE1E6E15BB25F29D3ABFE9569664EFF2299E48B9BC0ED31E7E6F8C87F`이다.
- 남은 위험:
  - NotifyIcon 컨텍스트 메뉴의 실제 클릭 동작은 Windows 데스크톱 사용자 세션에서 최종 수동 확인이 필요하다.
- 다음 조치:
  - 트레이 아이콘을 우클릭해 각 메뉴의 활성 상태와 창·대시보드 열기를 확인한다.

## 2026-08-31 - 트레이 상태 의미 색상 적용

- 변경 파일:
  - `src/SMSR.App/Infrastructure/TrayMenuState.cs`, `TrayStatusIcon.cs`
  - `src/SMSR.App/Mvp/MvpSelfCheck.cs`, `README.md`, `docs/installer-quickstart.md`
- 변경 사유:
  - 트레이 메뉴의 서버·Codex 상태와 시작·중지 동작을 텍스트만 읽지 않고 빠르게 구분할 필요가 있었다.
- 실행 명령:
  - Release 빌드와 self-check 4종, 설치 프로그램 재빌드·업그레이드, 설치 앱 self-check 4종 실행
- 검증 결과:
  - Codex 연결은 SeaGreen, 연결 대기는 DarkOrange, 서버 중지는 Firebrick으로 표시한다.
  - 상태 줄을 굵게 표시하고 서버 시작·중지 메뉴에도 각각 녹색·빨간색을 적용했다.
  - 빌드 경고 0·오류 0, 소스·설치 앱 self-check 4종 통과와 127.0.0.1:49783 수신을 확인했다.
  - 최종 Setup SHA-256은 `B1D08609100D7CFB1E69CCE04BB75DDCDBC2B950FA1B31EABAE26156E050DE20`이다.
- 남은 위험:
  - Windows 고대비 테마에서는 시스템 접근성 색상 정책이 사용자 지정 전경색보다 우선할 수 있다.
- 다음 조치:
  - 실제 트레이 메뉴에서 현재 Windows 테마 대비를 확인한다.

## 2026-08-31 - 다른 프로젝트의 새 그래프 자동 선택

- 변경 파일:
  - `src/SMSR.App/Mvp/WorkflowEventNotifier.cs`, `LocalServer.cs`
  - `src/SMSR.App/Services/LocalServerHost.cs`, `CodexAutoTrackingContext.cs`, `CodexMcpConfigSelfCheck.cs`
  - `src/SMSR.App/ViewModels/WorkflowSelectionViewModel.cs`, `WorkflowWorkspaceViewModel.cs`
  - `src/SMSR.App/Mvp/MvpSelfCheck.cs`, `README.md`, 그래프·설치 안내 문서
- 변경 사유:
  - 다른 Codex 프로젝트에서 그래프가 생성돼도 SMSR 선택 목록과 이미 열린 이전 대시보드가 바뀌지 않아 새 그래프가 없는 것처럼 보였다.
  - 위임 작업이 현재 task ID 대신 래퍼의 원본 `source_thread_id`를 workflow ID로 사용할 수 있었다.
- 실행 명령:
  - `dotnet build SMSR.slnx -c Release`
  - Release 앱의 config·tracking·전체 self-check 실행
  - 설치 프로그램 재빌드·무인 업그레이드, 설치 EXE와 publish EXE SHA-256 비교, 설치 앱 self-check 4종 실행
  - 별도 Tetris Codex 작업에서 현재 task ID로 계획·최종 이벤트 재전송 후 자동 선택 파일 확인
- 검증 결과:
  - 서버가 처음 관찰한 프로젝트·workflow를 앱에 전달하고, 앱이 목록을 다시 읽어 해당 그래프와 실시간 모니터를 자동 선택하도록 했다.
  - 훅 컨텍스트가 현재 session ID를 정확히 사용하고 원본·부모·위임 래퍼 ID를 무시하도록 계약과 회귀 검사를 보강했다.
  - 백그라운드 트레이 프로세스가 업그레이드 파일 교체를 막지 않도록 설치 직전 `SMSR.App.exe` 프로세스 트리를 종료한다.
  - Release 빌드 경고 0·오류 0, config·tracking·전체 self-check 종료 코드 0을 확인했다.
  - 설치 EXE와 publish EXE 해시가 일치하고 설치 앱의 config·tracking·OAuth·전체 self-check가 모두 종료 코드 0으로 통과했다.
  - `smsr-tetris-e2e / 01a0565c-d062-7912-9f47-bf1f21365a8f`의 15개 노드가 모두 SUCCESS이며 `%LocalAppData%\SMSR\last-workflow.json`이 해당 새 그래프로 자동 변경됨을 확인했다.
  - 최종 Setup SHA-256은 `5716EF8E7C69FA47DBE232EFB3692BE667C5FE7B01BF335E6E06020CB4029287`이다.
- 남은 위험:
  - 특정 workflow가 들어 있는 기존 브라우저 URL은 사용자의 조회 문맥을 보존하기 위해 자동 이동하지 않는다. 앱 또는 트레이에서 현재 대시보드를 다시 열어야 한다.
  - 수정 전 잘못된 원본 task ID로 저장된 기존 그래프는 복구 가능성을 위해 자동 삭제하지 않는다.
- 다음 조치:
  - 없음.

## 2026-08-31 - 날짜 기반 workflow ID와 사과게임 대시보드 수정

- 변경 파일:
  - `src/SMSR.App/Mvp/SmsrMcpInstructions.cs`, `TrackingContractSelfCheck.cs`
  - `src/SMSR.App/Mvp/WorkflowIdGenerator.cs`, `WorkflowProgress.cs`, 대시보드 그래프·진행률 파일
  - `src/SMSR.App/Services/CodexAutoTrackingContext.cs`, `CodexMcpConfigSelfCheck.cs`
  - `.agents/skills/smsr-tracking/SKILL.md`, `README.md`, `docs/graph-tracking-guide.md`
- 변경 사유:
  - 새 그래프 ID를 프로젝트명과 생성 날짜시간으로 읽기 쉽게 만들 필요가 있었다.
  - 사과게임 수용 시험에서 긴 agent ID의 SVG 넘침, 계층 아래 의존선 누락, SUCCESS와 55~88% 진행률의 불일치가 발견됐다.
- 실행 명령:
  - 별도 projectless Codex 작업에서 사과게임 구현·테스트·브라우저 QA·Git 커밋 및 SMSR 추적
  - Release 빌드와 self-check, 설치 프로그램 갱신, 날짜 기반 workflow ID 재기록과 자동 선택 확인
- 검증 결과:
  - 사과게임의 합계 10 제거, 정답·오답 피드백, 점수, 60초 타이머, 재시작, 모바일 화면과 콘솔 오류 부재를 실제 브라우저에서 확인했다.
  - 첫 `save_plan`에서 workflow ID를 생략하면 서버가 `프로젝트명__yyyyMMdd-HHmmssfff` 형식으로 생성하고 이후 이벤트가 반환된 ID를 재사용하도록 계약을 변경했다.
  - 현재 계층에 숨은 의존성을 보이는 조상 노드로 투영하고, SVG 텍스트를 축약하되 전체 값은 툴팁·상세에 유지한다.
  - 드릴다운 화면에 현재 부모 노드를 기준점으로 함께 렌더링해 부모에서 직계 하위 작업으로 이어지는 선을 모든 계층에서 표시한다.
  - SUCCESS 상태는 기존 저장 데이터와 신규 이벤트 모두 100%로 정규화하고 상단 완료 수는 전체 계획 노드 기준으로 표시한다.
  - 실제 자동 생성 ID `smsr-apple-game-e2e__20260831-162316084`와 앱 자동 선택을 확인했다.
  - 브라우저에서 루트 선 2개, 첫 드릴다운 선 1개·노드 2개, 다음 드릴다운 선 2개·노드 3개와 모든 하위 진행률 100%를 확인했다.
  - 소스·설치 앱의 config·tracking·OAuth·전체 self-check가 모두 종료 코드 0으로 통과했다.
  - 최종 Setup SHA-256은 `F08B8577B87E6961F7216ACBAD377B6CB39262249EDD7096BA5BC0BCA740B764`이다.
- 남은 위험:
  - 수정 전 ID로 저장된 사과게임 그래프는 복구 가능성을 위해 자동 삭제하지 않는다.
- 다음 조치:
  - 없음.
## 2026-08-31 - 사용자 편집형 작업계획서 검토 정책

- 변경 파일: `AppSettingsService.cs`, `PlanningPromptSettings.cs`, `CodexAutoTrackingContext.cs`, 설정 ViewModel·XAML, self-check, README와 계획 프롬프트 문서
- 변경 사유: 비단순 구현 전에 Codex가 작업계획서를 먼저 제시하고 사용자가 검토하거나, 계획 생성 문구 자체를 설정에서 수정할 수 있어야 했다.
- 실행 명령: `dotnet build src/SMSR.App/SMSR.App.csproj -c Release --no-restore`, 설치본 self-check, 설치 프로그램 빌드
- 검증 결과: Release 빌드 경고 0·오류 0, 소스와 설치본의 config·tracking·OAuth·전체 self-check가 모두 종료 코드 0을 반환했다. 설치 UI에서 검토 체크박스, 편집기와 기본값 복원 버튼을 확인하고 `사용자 검토 계획 {projectId} {taskId}` 저장, 훅의 `DemoProject task-77` 치환, 요청 원문 제외, 기본값 복원까지 왕복 검증했다. 설치 프로그램 SHA-256은 `9282283520092795D8AEBBD75254704EDE6FC1F68F85919E3BBAA4F47964B826`이다.
- 남은 위험: Codex가 이미 처리 중인 요청에는 설정 변경이 소급 적용되지 않으며 다음 사용자 요청부터 적용된다.
- 다음 조치: 설정 UI 육안 확인 후 `main` 커밋과 원격 푸시.
## 2026-08-31 - 작업계획서 테마와 기본 문구 보정

- 변경 파일: `PlanningPromptSettings.cs`, `Controls.xaml`, 계획 프롬프트 문서와 self-check
- 변경 사유: 초기 기본 문구가 기존 작업지시에 없는 계획 형식을 추가했고, 어두운 테마에서 다중행 편집기의 전경색과 커서가 테마를 따르지 않았다.
- 실행 명령: Release 빌드, config·tracking·OAuth·전체 self-check, 설치 프로그램 빌드와 설치본 UI 확인
- 검증 결과: Release 빌드 경고 0·오류 0, 소스와 설치본의 config·tracking·OAuth·전체 self-check가 모두 종료 코드 0을 반환했다. 이전 기본 문구가 새 비확장 기본 문구로 표시됐고 어두운 테마 편집기 전경색은 `#E8EDF6`으로 확인했다. 설치 프로그램 SHA-256은 `181A8BF109A489C653FEA97E702274561409D4783FD4731ABFB6E80D13B95F7B`이다.
- 남은 위험: 사용자가 직접 저장한 사용자 정의 프롬프트는 제품이 임의 변경하지 않는다.
- 다음 조치: 설치본 검증 후 `main` 커밋과 푸시.
## 2026-08-31 - 최초 프롬프트 기반 계획 형식 반영

- 변경 파일: `PlanningPromptSettings.cs`, 계획 프롬프트 문서와 config self-check
- 변경 사유: 최초 제공된 작업 그래프 프롬프트를 보존만 하지 않고 현재 작업계획서 설정의 기본 문구에 유효한 계획 형식을 반영해야 했다.
- 실행 명령: Release 빌드, config·tracking·OAuth·전체 self-check, 설치 프로그램 빌드와 설치본 설정 확인
- 검증 결과: Release 빌드 경고 0·오류 0, 소스와 설치본의 config·tracking·OAuth·전체 self-check가 모두 종료 코드 0을 반환했다. 설치 UI의 515자 기본 문구에서 계층 작업, 선행 작업, 완료 기준, 3회 실패와 요청형 그래프 제한을 확인했다. 설치 프로그램 SHA-256은 `E41479A14D690B5E302CAFE141EEB5FFD2C63D4A6564E6AE524833558BC5F920`이다.
- 남은 위험: 사용자가 직접 작성한 사용자 정의 프롬프트는 자동 교체하지 않는다.
- 다음 조치: 설치본 검증 후 `main` 커밋과 푸시.
## 2026-09-01 - 재부팅 후 MCP OAuth 연결 복구 검증

- 변경 파일: `src/SMSR.App/Services/CodexMcpConfig.cs`, `CodexConnectionService.cs`, `CodexMcpConfigSelfCheck.cs`, `src/SMSR.App/Mvp/OAuthSelfCheck.cs`, `OAuthPersistenceSelfCheck.cs`, `MvpSelfCheck.cs`, `docs/mcp-connection.md`, `docs/installer-quickstart.md`
- 변경 사유: PC 재부팅 후 서버 시작과 Codex MCP 초기화 경합을 줄이고, 인증 유지 상태를 재인증 필요 상태로 잘못 안내하지 않도록 한다.
- 실행 명령: `dotnet build src/SMSR.App/SMSR.App.csproj -c Release`; Release 실행 파일의 `--self-test`, `--oauth-self-test`, `--codex-config-self-test`
- 검증 결과: Release 빌드 경고·오류 0개. `--codex-config-self-test`, `--oauth-self-test`, `--self-test` 모두 종료 코드 0. OAuth 발급·갱신, DPAPI 상태 재로딩·토큰 회전, MCP 설정 초기화 대기값을 통과했다.
- 남은 위험: 다른 Windows 사용자나 PC에는 DPAPI 및 Codex 보안 저장소를 복사할 수 없어 환경별 최초 OAuth 승인이 필요하다.
- 다음 조치: Release 빌드와 자체 검증 후 설치본 재생성 시 변경을 포함한다.
## 2026-09-01 - 순차 작업 진행률과 의존성 게이트

- 변경 파일: `src/SMSR.App/Mvp/DashboardHierarchy.cs`, `DashboardGraph.cs`, `DashboardPage.cs`, `WorkflowDependencyGate.cs`, `WorkflowTools.cs`, `SmsrMcpInstructions.cs`, `MvpSelfCheck.cs`, `docs/mcp-connection.md`
- 변경 사유: 선행 작업이 완료되지 않았는데 후행 노드가 동시에 `IN_PROGRESS`로 표시되는 문제를 방지한다.
- 실행 명령: `dotnet build src/SMSR.App/SMSR.App.csproj -c Release`; Release 실행 파일의 `--self-test`, `--tracking-self-test`
- 검증 결과: Release 빌드 경고·오류 0개. `--self-test`, `--tracking-self-test` 모두 종료 코드 0. 선행 `SUCCESS(100%)` 전 후행 상태 거부와 과거 잘못된 상태의 `PENDING 0%` 화면 보정을 통과했다.
- 남은 위험: 의존성이 없는 노드는 의도된 병렬 작업으로 간주한다.
- 다음 조치: 빌드와 자체 검증 후 설치본에 포함한다.
## 2026-09-01 - 이벤트 기반 즉시 진행 갱신

- 변경 파일: `src/SMSR.App/Mvp/SmsrMcpInstructions.cs`, `WorkflowTools.cs`, `DashboardLiveUpdates.cs`, `TrackingContractSelfCheck.cs`, `src/SMSR.App/Services/CodexAutoTrackingContext.cs`, `CodexMcpConfigSelfCheck.cs`, `.agents/skills/smsr-tracking/SKILL.md`, `docs/graph-tracking-guide.md`, `docs/mcp-connection.md`
- 변경 사유: 에이전트가 계획과 한 번의 상태만 전송하고 작업 중 진행 변화를 누락하는 문제를 막고, 연속 이벤트를 브라우저가 순서대로 즉시 반영하도록 한다.
- 실행 명령: `dotnet build src/SMSR.App/SMSR.App.csproj -c Release`; Release 실행 파일의 `--self-test`, `--tracking-self-test`, `--codex-config-self-test`
- 검증 결과: Release 빌드 경고·오류 0개. `--self-test`, `--tracking-self-test`, `--codex-config-self-test` 모두 종료 코드 0. 즉시 이벤트 송신 계약, 30초 heartbeat 보완 규칙, 연속 SSE 화면 갱신 코드를 통과했다.
- 남은 위험: SMSR은 수동 수신 서버이므로 에이전트가 보내지 않은 내부 작업 진행률을 임의로 추측하지 않는다.
- 다음 조치: 빌드와 자체 검증 후 새 설치본에 포함한다.

## 2026-09-01 - 활성 그래프 Codex 활동 JSONL 자동 기록

- 변경 파일: `src/SMSR.App/Mvp/Activity*.cs`, `LocalServer.cs`, `LocalServerEndpoints.cs`, `DashboardPage.cs`, `DashboardPanels.cs`, `WorkflowExportService.cs`, `MvpSelfCheck.cs`, `src/SMSR.App/Services/ActivityHookClient.cs`, `CodexActivity*.cs`, `CodexHookRunner.cs`, `CodexTrackingResolver.cs`, `HookJson.cs`, `TrackingSessionStore.cs`, Codex 훅 등록 파일, 추적·설치 문서
- 변경 사유: 그래프가 활성화된 동안 에이전트 lifecycle과 지원되는 로컬 도구 완료를 매번 정규화된 JSONL로 남기고 대시보드에 즉시 표시한다.
- 실행 명령: `dotnet build src/SMSR.App/SMSR.App.csproj -c Release`; Release 실행 파일의 `--self-test`, `--codex-config-self-test`, `--tracking-self-test`
- 검증 결과: 1차 Release 빌드는 새 파일의 명시적 `System.IO`·`System.Net.Http` using 누락으로 실패했고 이를 수정했다. 이후 빌드 경고·오류 0개, config·tracking·OAuth·전체 자체 검증 모두 종료 코드 0을 반환했다. 활성 세션 생성, 하위 에이전트 상속, 비활성 세션 무기록, 원시 도구 입력 제외, 보호된 활동 API, SSE 알림, 대시보드·내보내기 JSONL을 확인했다. Release 실행 파일의 실제 훅 stdin 왕복도 종료 코드 0이며 개발자 컨텍스트 반환과 프롬프트 원문 미노출을 확인했다.
- 남은 위험: 호스팅 웹 검색과 모델 내부 추론은 Codex 훅 범위 밖이므로 기록하지 않는다. 훅 정의가 변경된 이번 업데이트는 Codex `/hooks`에서 한 번 다시 신뢰해야 한다.
- 다음 조치: 실제 설치 실행 파일로 훅 stdin 왕복과 브라우저 연속 갱신을 최종 확인한다.

## 2026-09-01 - 진행 집계와 활동 기록 코드 검토

- 변경 파일: `DashboardHierarchy.cs`, `WorkflowDependencyGate.cs`, `ActivityJsonlStore.cs`, `ActivityFileLock.cs`, `ActivityEndpoints.cs`, `CodexActivityHook.cs`, `CodexActivityClassifier.cs`, `CodexHookRunner.cs`, `TrackingSessionStore.cs`, 활동·MVP 자체 검사
- 변경 사유: 하위 작업 미완료 상위 노드의 조기 성공, 훅 재전송 중복, JSONL 기록과 내보내기의 동시 접근, 종료된 하위 에이전트와 오래된 세션 매핑 잔존 가능성을 제거하고 활동 기록 장애를 Codex 본 작업과 격리한다.
- 실행 명령: Release `dotnet build`; `dotnet test`; Release 실행 파일의 config·tracking·OAuth·전체 self-check; 실제 `--smsr-auto-track-hook` stdin/stdout 왕복; 변경·비밀·절대경로 정적 검색; 새 코드 파일 대상 `dotnet format whitespace --verify-no-changes`
- 검증 결과: Release 빌드 경고 0·오류 0, 자체 검사 4종과 실제 훅 왕복 모두 종료 코드 0. 하위 작업 미완료 상위 `SUCCESS` 거부, 기존 조상 의존 그래프 호환, 활동 ID 중복 제거, 16개 동시 JSONL 기록과 잠금 내보내기, 하위 에이전트 매핑 제거와 30일 만료, 활동 기록 오류 격리, 원시 입력 미저장을 확인했다. 새 코드 파일의 whitespace 검증도 통과했다.
- 남은 위험: 저장소 전체 포맷 검증은 이번 변경 밖의 기존 `AssemblyInfo.cs`, 이벤트 저장소, OAuth 자체 검사 압축 스타일을 보고한다. 호스팅 도구와 모델 내부 추론은 Codex 훅이 제공하지 않아 기록할 수 없다.
- 다음 조치: 커밋 후 `origin/main`에 푸시한다.

## 2026-09-01 - 현재 노드 하이라이트와 스크롤 유지

- 변경 파일: `DashboardCurrentNode.cs`, `DashboardGraph.cs`, `DashboardGraphStyles.cs`, `DashboardLiveUpdates.cs`, `MvpSelfCheck.cs`
- 변경 사유: 모든 진행 중 노드가 발광해 현재 작업을 구분하기 어렵고, SSE 화면 교체마다 흐름·그래프·상세 영역의 스크롤 위치가 초기화됐다.
- 실행 명령: Release 빌드, `--self-test`, 변경 대시보드 파일 대상 whitespace 검증, 브라우저 격리 테스트 시도, 설치 프로그램 빌드·무인 업그레이드, 설치 앱 자체 검사 4종과 서버 상태 확인
- 검증 결과: 최신 유효 노드 이벤트 또는 더 최근의 활성 heartbeat 한 개만 `current`로 선택하고, 드릴다운에서는 현재 노드의 보이는 가장 가까운 조상 하나만 강조한다. 다른 진행 노드는 상태 색상만 유지한다. SSE 교체 전후 `#flow`, `#graph`, `#details`의 가로·세로 위치를 저장·복원한다. Release 빌드 경고 0·오류 0, 전체 자체 검사와 변경 파일 whitespace 검증을 통과했다. Setup SHA-256은 `47FC1275DEB8EB2D01035D65BFC8EE0A11711055108A8F27F7C0FA5B3433E6FA`, 설치 앱 SHA-256은 publish와 동일한 `7E84BABAA07FF2CAEC8712EBE1003269DDCADA8D1FBF1DA12D88AF006566FD8A`다. 설치 앱 자체 검사 4종이 모두 통과했고 설치 경로 프로세스와 127.0.0.1:49783 HTTP 200을 확인했다.
- 남은 위험: 브라우저의 `data:` URL 격리 테스트는 브라우저 보안 정책으로 차단되어 우회하지 않았다. 실제 제품 HTML 생성과 회귀 자체 검사로 동작 계약을 검증했다. 현재 Codex 프로세스는 시작 후 변경된 MCP·훅을 다시 읽지 않으므로 한 번 재시작해야 한다.
- 다음 조치: Codex를 재시작하고 `/hooks`에서 이번에 추가된 `SessionEnd`, `PostToolUse` 정의를 한 번 신뢰한 뒤 실제 대시보드의 장시간 SSE 갱신을 확인한다.

## 2026-09-01 - 인증 요청 없는 Codex stdio 자동 연결

- 변경 파일: `McpBridgeToken.cs`, `McpBridgeConnection.cs`, `McpHttpGateway.cs`, `McpHttpResponse.cs`, `StdioMcpHost.cs`, `StdioWorkflowTools.cs`, `StdioPlanTools.cs`, `StdioAgentTools.cs`, `LocalServer.cs`, `LocalServerEndpoints.cs`, `App.xaml.cs`, Codex 설정·연결 서비스와 자체 검사, 서버·설정 XAML, 설치 UI, README와 연결·설치 문서
- 변경 사유: URL 기반 OAuth는 클라이언트 등록 상태가 어긋나면 MCP 호출 전에는 인증 화면을 열 수 없어 사용자가 에이전트에게 인증 호출을 반복 요청해야 했다. 설치된 SMSR 실행 파일을 Codex가 직접 구동하는 stdio 브리지로 기본 연결을 바꿔 재부팅·다른 프로젝트·다른 PC 설치에서 브라우저 인증 없이 자동 연결한다.
- 실행 명령: Release 빌드, `dotnet test`, config·tracking·OAuth·전체 self-check, 실제 stdio `initialize`·`tools/list`·`list_workflows`, 대상 파일 whitespace 검사, 설치 프로그램 빌드·무인 업그레이드, 설치 앱 자체 검사와 stdio 왕복
- 검증 결과: 최초 빌드는 새 게이트웨이의 `System.Net.Http` using 누락으로 실패해 수정했다. 테스트 서버가 검증 출력 EXE를 잠근 1회 실패는 정확한 PID·경로 확인 후 해당 테스트 프로세스만 종료해 해소했다. 이후 Release 빌드 경고·오류 0개, `dotnet test`와 자체 검사 4종이 모두 종료 코드 0을 반환했다. 소스·설치본 stdio에서 자동 연결 신호, 9개 도구와 기존 워크플로우 조회를 확인했다. 현재 Codex 설정은 설치된 `SMSR.App.exe --mcp-stdio`를 가리키며 OAuth 항목이 없고, publish·설치 EXE SHA-256은 `0F26E99D2E4905A0151C67CA4943A1DE1B4CEE651F5DD788F3F94F0AD549F504`로 일치한다. Setup SHA-256은 `32BD101CC02F5AC68C88D273AA2032D054BCF6BD9655A1CF4EC10CF51AD17FF4`이다.
- 남은 위험: 실행 중인 Codex 프로세스는 시작 후 변경된 MCP 전송 설정을 다시 읽지 않으므로 이번 OAuth→stdio 마이그레이션에서 한 번 완전 재시작해야 한다. 이후에는 브리지가 시작 연결 신호까지 자동 전송하므로 인증·확인 호출이나 브라우저 승인이 필요 없다. HTTP OAuth endpoint는 기존 클라이언트 호환과 자체 진단을 위해 유지한다.
- 다음 조치: Codex를 한 번 완전 재시작한 뒤 현재 작업에서 `list_workflows` 실호출을 확인한다.

## 2026-09-01 - Codex 시작 시 SMSR 본체 자동 복구

- 변경 파일: `src/SMSR.App/Services/DashboardProcessLauncher.cs`, `MainInstanceGuard.cs`, `src/SMSR.App/App.xaml.cs`, `Mvp/StdioMcpHost.cs`, `LocalServerEndpoints.cs`, 자체 검사, README와 연결·설치 문서
- 변경 사유: Codex가 stdio 브리지만 실행하고 대시보드 본체는 실행하지 않아, Windows 자동 시작이 동작하지 않은 세션에서 MCP 도구와 화면을 사용할 수 없던 문제를 해결한다.
- 실행 명령: Release 빌드, config·tracking·OAuth·전체 self-check, 변경 파일 whitespace 검사, 설치 프로그램 빌드·무인 업그레이드, 설치본 stdio `initialize`·`tools/list`, `/api/health`, 프로세스·해시 확인
- 검증 결과: Release 빌드 경고·오류 0개와 자체 검사 4종을 통과했다. 본체와 49783 listener가 없는 상태에서 설치본 stdio를 실행하자 `--background --ensure-server` 본체가 하나 생성됐고, 브리지 종료 후에도 서버가 `ready`로 유지됐다. stdio protocol `2025-11-25` 초기화와 도구 9개를 확인했다. 중복 본체는 하나만 유지하며 시작 메뉴에서 SMSR을 다시 실행하자 같은 PID의 `Show Me Status Report` 창 핸들이 생성됐다. 설치·publish EXE SHA-256은 `5C4AE596FF9DE299DF52A3D0DD85E6E4E8BE39A27CA0890E26F6F9AA715FB32A`, Setup SHA-256은 `FA2D95BB38DAC47C93D1AABA7746FB244B23CDA93737D6E552744AA37E48BEA9`이다.
- 남은 위험: 이미 실행 중인 SMSR에서 사용자가 서버를 수동 중지한 경우에는 그 명시적 선택을 존중하므로 다른 본체를 중복 실행하지 않는다. 복합 PowerShell 검증 명령 2개는 실행 정책이 차단해 같은 방식의 재시도를 중단하고 단순 명령과 임시 테스트 스크립트로 분리했다. 임시 스크립트의 Windows PowerShell 5.1 `ArgumentList` 미지원은 `Arguments`로 변경해 검증했고 스크립트는 제거했다.
- 다음 조치: BeepleLunch는 당시 OAuth 실패로 `save_plan`이 실행되지 않아 SMSR DB에 워크플로우가 없다. 필요하면 기존 프로젝트 요구사항을 새 그래프로 명시적으로 저장한다.

## 2026-09-02 - 읽기 쉬운 워크플로우와 동적 계획 경계

- 변경 파일: workflow ID 생성기, 계획·카탈로그 저장소, WPF 워크플로우 선택, 대시보드 헤더, 계획·상태 게이트, MCP·훅 지침, `smsr-tracking` 스킬, 추적·연결 문서와 자체 검사
- 변경 사유: Codex session UUID가 workflow ID로 저장돼 작업을 식별하기 어렵고, 완료된 100% 노드에 후속 작업을 덧붙이면 완료와 진행 중 상태를 구분하기 어려웠다. 작업 중 계획의 노드 추가·정렬도 명시적인 보존 계약과 검증이 필요했다.
- 실행 명령: Release 빌드, `dotnet test`, config·tracking·OAuth·전체 self-check, 변경 파일 whitespace 검사, 스킬 frontmatter 수동 검사, 설치 프로그램 빌드·무인 업그레이드, 설치본 stdio `initialize`·`tools/list`와 `/api/health`
- 검증 결과: 새 workflow ID는 `yyyyMMdd-HHmmssfff__프로젝트명__대표작업명`으로 생성되고, 존재하지 않는 UUID를 첫 `save_plan`에 전달해도 읽기 쉬운 ID로 교체된다. 기존 UUID 데이터는 ID를 변경하지 않고 WPF 목록과 대시보드에 최상위 작업 제목을 함께 표시한다. 활성 계획 재저장 시 기존 노드 진행률 유지, 새 노드 `PENDING`, 입력 순서 반영, 제거 노드 현재 상태 정리와 SSE 알림을 확인했다. `SUCCESS` 노드 재개·변경·하위 작업 추가와 종료 그래프 변경은 거부된다. 빌드 경고·오류 0개, 자체 검사 4종과 설치본 stdio 도구 9개·서버 `ready`를 확인했다. 설치·publish EXE SHA-256은 `FFA3AC4C7088DABCA7A3916737EE1EC75F159BA976B65FE7D24C27023DA4D81D`, Setup SHA-256은 `6A1FF34134E6B87ABD90091455007A4872D33431397A4B6C62774F389207AFE5`이다.
- 남은 위험: 기존 UUID는 이벤트·JSONL·외부 링크 참조를 깨지 않기 위해 물리적으로 이름을 바꾸지 않는다. `skill-creator`의 `quick_validate.py`는 번들 Python에 PyYAML이 없어 실행되지 않았으며, 동일 검사항목인 frontmatter 구분자·허용 이름·description·TODO 부재를 수동 확인했다.
- 다음 조치: 실제 새 그래프 요청에서 생성 ID와 동적 계획 갱신 표시를 사용자 흐름으로 확인한다.

## 2026-09-02 - 워크플로우 계획 경계 회귀 테스트 보고

- 변경 파일: `src/SMSR.App/Mvp/TrackingContractSelfCheck.cs`, `src/SMSR.App/Mvp/MvpSelfCheck.cs`, `docs/test-report-2026-09-02-workflow-plan.md`, `README.md`, `docs/development-log.md`
- 변경 사유: 읽기 쉬운 workflow ID, 동적 계획 변경, 완료 노드 불변 처리의 실제 테스트 경과와 결과를 재현 가능하게 남기고, 직렬화 문자열·서버 DB 경계에 의존하던 자체 검사 오판을 제거한다.
- 실행 명령: Release `dotnet build`, `dotnet test`, config·tracking·OAuth·전체 self-check, 소스와 설치 EXE stdio `initialize`·`tools/list`, 설치 프로그램 빌드·무인 업그레이드, 설치본 자체 검사 4종, `/api/health`, 해시 확인
- 검증 결과: 최초 tracking 검사 3회와 종합 검사 3회에서 테스트 판정 오류가 이어지자 재실행을 중단하고 원인·시도·새 계획을 테스트 보고서에 기록했다. 실패 원인은 한글 JSON·HTML 원문 비교, 동적 계획 하위 노드 수 기대값, 별도 서버 DB의 초기 목록 기대값이었다. 구조화 판정과 테스트 데이터 경계를 수정한 뒤 Release 빌드 경고 0·오류 0, 자체 검사 4종, stdio protocol `2025-11-25`와 도구 9개, 설치본 자체 검사 4종, 서버 `ready`를 모두 확인했다. Setup SHA-256은 `3D9352C32BB9D3C9662207370F2FEC9943B325818E3AC6E31A2B2A84FF09FD03`, publish·설치 EXE SHA-256은 `A6F43FBC1727D0AEB80929634B707C6BD13A1BADA03328905880192F18C69884`로 일치한다.
- 남은 위험: 별도 단위 테스트 프로젝트가 없고 현재 PC·사용자에서만 설치를 확인했다. 다른 PC의 최초 설치·DPAPI 설정과 다양한 DPI의 시각 검증은 별도 수행이 필요하다.
- 다음 조치: 실제 새 그래프 작업에서 100% 완료 노드 이후 후속 작업이 새 노드 또는 새 그래프로 생성되는 사용자 흐름을 확인한다.

## 2026-09-02 - SMSR v1.1.0 릴리즈 준비

- 변경 파일: `src/SMSR.App/SMSR.App.csproj`, `docs/installer-quickstart.md`, `docs/releases/v1.1.0.md`, `README.md`, `docs/development-log.md`
- 변경 사유: `v1.0.0` 이후 stdio 자동 연결·서버 복구·활동 추적·읽기 쉬운 workflow ID·동적 계획 경계 변경을 하나의 설치 릴리즈로 배포한다.
- 실행 명령: Release 빌드, config·tracking·OAuth·전체 self-check, 설치 프로그램 빌드, 무인 업그레이드, 설치 실행 파일 자체 검사 4종, stdio `initialize`·`tools/list`, `/api/health`, 버전·해시 확인
- 검증 결과: 앱·설치 버전 `1.1.0.0`, 빌드 경고 0·오류 0, 소스와 설치본 자체 검사 4종 종료 코드 0, 설치본 stdio protocol `2025-11-25`와 도구 9개, `127.0.0.1:49783` 서버 `ready`를 확인했다. Setup SHA-256은 `FFB3EFDB6C51C95D2F14D56E67AF5196451C745D77705B4882ADD8F1C7F8D374`, publish·설치 EXE SHA-256은 `4F03D9BEF975F5EA2227BCBE72F0B98BE755CE074BB386688170661A44DF9B6A`로 일치한다.
- 남은 위험: 다른 Windows PC·사용자의 최초 설치와 DPAPI 설정 생성은 대상 환경에서 확인해야 한다.
- 다음 조치: 버전 커밋과 `v1.1.0` 태그를 푸시하고 GitHub 릴리즈에 설치 파일을 첨부한다.

## 2026-09-02 - 완료 그래프의 관련 후속 작업 반영

- 변경 파일: `WorkflowPlanUpdate.cs`, `PlanTools.cs`, `TrackingContractSelfCheck.cs`, `SmsrMcpInstructions.cs`, `CodexAutoTrackingContext.cs`, `CodexMcpConfigSelfCheck.cs`, `.agents/skills/smsr-tracking/SKILL.md`, README와 그래프·MCP·로컬 추적·테스트 문서
- 변경 사유: 그래프의 모든 노드가 100% 완료된 뒤 에이전트가 같은 요청의 보완 작업을 계속해도 서버가 계획 갱신을 거부해, 화면이 완료 상태에 머무는 불일치를 해소한다.
- 실행 명령: `dotnet build SMSR.slnx -c Release`; `dotnet test SMSR.slnx -c Release --no-build --verbosity:minimal`; Release DLL의 `--codex-config-self-test`, `--tracking-self-test`, `--oauth-self-test`, `--self-test`; 변경 C# 파일 대상 `dotnet format whitespace --verify-no-changes`; `git diff --check`
- 검증 결과: 빌드 경고 0·오류 0, 테스트와 자체 검사 4종, 포맷·diff 검사가 모두 종료 코드 0을 반환했다. 완료 노드를 보존하면서 연결된 후속 노드를 추가하면 workflow가 다시 `ACTIVE`가 되고 시작 이벤트 후 대시보드는 `완료 3 / 4`와 100% 미만 진행률을 표시한다. 완료 노드 재개·변경·하위 삽입과 연결 없는 별도 작업은 거부되며 새 그래프 자동 생성은 유지된다.
- 남은 위험: 관련 작업과 별도 작업의 의미 판단은 에이전트가 수행한다. SMSR은 연결 관계와 완료 이력 불변성은 검증하지만 도구 활동만 보고 업무 제목을 임의 생성하지 않는다. 실행 중인 설치본에는 다음 배포 버전을 설치해야 변경 계약이 반영된다.
- 다음 조치: 커밋 후 `origin/main`에 푸시하고 다음 패치 설치본에 포함한다.

## 2026-09-02 - SMSR v1.1.1 패치 릴리즈

- 변경 파일: `src/SMSR.App/SMSR.App.csproj`, `README.md`, `docs/installer-quickstart.md`, `docs/releases/v1.1.1.md`, `docs/development-log.md`, 생성 설치 파일
- 변경 사유: 완료 그래프에 관련 후속 노드를 안전하게 추가하고 실제 에이전트 작업과 100% 화면이 어긋나지 않게 한 변경을 Windows 설치본으로 배포한다.
- 실행 명령: Release 빌드·테스트, `scripts/build-installer.ps1`, 무인 업그레이드, 설치본 config·tracking·OAuth·전체 자체 검사, stdio `initialize`·`tools/list`, 설치 서버 `/api/health`, 버전·SHA-256 확인
- 검증 결과: 앱·설치 버전 `1.1.1.0`, Release 빌드 경고 0·오류 0, 소스와 설치본 자체 검사 4종 종료 코드 0, stdio protocol `2025-11-25`와 도구 9개를 확인했다. 설치 경로 서버는 `127.0.0.1:49783`에서 `ready`이며 publish·설치 EXE SHA-256은 `4EC3328FC704068A989D61496AF5205F4A2EE2C337F60DED58CE6992D57BC10F`, Setup SHA-256은 `6E2CB683831E4278164A2CA1C78E48D4591CCC59C2C12772448D58E672CFBC8B`다.
- 남은 위험: 코드 서명이 없어 다른 PC에서 Windows SmartScreen 경고가 표시될 수 있다. 설치 후 실행 중인 Codex는 MCP·훅 지침을 다시 읽도록 한 번 완전히 재시작해야 한다.
- 다음 조치: 릴리즈 커밋과 `v1.1.1` 태그를 `origin/main`에 푸시하고 GitHub 릴리즈에 설치 파일을 첨부한다.

## 2026-09-03 - 다중 프로젝트 작업 캘린더와 이력 삭제

- 변경 파일: 워크플로우 캘린더·선택·모니터 ViewModel, 작업·이벤트 WPF 패널, 이벤트 저장소 카탈로그·삭제 경로, 활동 JSONL·세션 매핑, MCP 계획 게이트, 자체 검사와 사용 문서
- 변경 사유: UUID 중심 목록을 날짜와 작업명 중심으로 바꾸고, A 프로젝트를 추적한 같은 Codex 세션이 B 프로젝트로 전환될 때 활동 JSONL이 A에 남는 문제를 해소하며, 사용자가 앱에서 저장 이력을 안전하게 정리할 수 있어야 했다.
- 실행 명령: Release `dotnet build`·`dotnet test`; config·tracking·OAuth·전체 self-check; 변경 C# whitespace 검사; 설치본 생성·무인 업그레이드; 설치 EXE 자체 검사; stdio `initialize`·`tools/list`; `/api/health`; Windows UI Automation 트리 검사
- 검증 결과: 모든 프로젝트를 단일 조회해 날짜별로 표시하고 날짜 선택 시 그날 최신 작업으로 전환한다. 같은 날짜의 다른 작업도 선택할 수 있으며 다른 프로젝트 이벤트는 현재 선택을 바꾸지 않는다. 같은 Codex 세션의 A→B 매핑 교체, workflow·project·전체 삭제, 내보내기 보존, 계획 없는 이벤트·heartbeat 거부를 자체 검사로 확인했다. 빌드 경고 0·오류 0, 소스와 설치본 자체 검사 4종, stdio protocol `2025-11-25`와 도구 9개를 통과했다.
- 남은 위험: 캘린더 시각은 SMSR 이벤트 수신 시각이며 파일 시스템 생성 시각은 아니다. 다른 Windows PC·사용자의 실제 DPI와 SmartScreen 동작은 대상 환경에서 확인해야 한다.
- 다음 조치: 기능 커밋 뒤 설치본을 다시 생성해 최종 해시를 기록하고 `v1.2.0`을 배포한다.

## 2026-09-03 - SMSR v1.2.0 기능 릴리즈

- 변경 파일: `src/SMSR.App/SMSR.App.csproj`, `README.md`, 설치·연결·추적 문서, `docs/releases/v1.2.0.md`, 생성 설치 파일
- 변경 사유: 다중 프로젝트 캘린더, 활동 매핑 전환, 기록 삭제와 stdio 서버 자동 복구 수정 사항을 다른 Windows PC에서 설치 가능한 기능 릴리즈로 배포한다.
- 실행 명령: Release 빌드·테스트, 소스와 설치본 자체 검사 4종, `scripts/build-installer.ps1`, 무인 업그레이드, 설치본 stdio `initialize`·`tools/list`, `/api/health`, 실행 파일 버전·SHA-256 확인
- 검증 결과: 앱·설치 버전 `1.2.0.0`, Release 빌드 경고 0·오류 0, 소스와 최종 설치본의 config·tracking·OAuth·전체 자체 검사 종료 코드 0을 확인했다. 설치본 stdio protocol은 `2025-11-25`, 도구는 9개이며 브리지 종료 후 백그라운드 본체 1개와 서버 `ready`가 유지됐다. 설치 EXE SHA-256은 `8C8AA7F32356B39BD8D9D0BF1FD09A9423F04C98C493332559208DCC561BA4A1`, publish·설치 앱 SHA-256은 `D4C5268B16B50C4C260883B585142DF32F77AEBC413D553029A5C4F8B0F7D45C`로 일치한다.
- 남은 위험: 설치 파일은 코드 서명되지 않아 다른 PC에서 SmartScreen 경고가 표시될 수 있다. 현재 실행 중인 Codex는 새 MCP 지침을 읽도록 설치 후 한 번 완전히 다시 시작해야 한다.
- 다음 조치: 릴리즈 메타데이터를 커밋하고 `v1.2.0` 태그와 `main`을 푸시한 뒤 GitHub 릴리즈에 설치 파일을 첨부한다.

## 2026-09-04 - 일일 기록·상태 카드·자동 업데이트 및 stdio 브리지

- 변경 파일: 일일 활동 계약·저장소·MCP 도구, 캘린더·설정·대시보드 WPF/HTML, Codex 자동 추적 지침과 훅, 업데이트 서비스, stdio 브리지, 설치·배포 스크립트, README·연결·설치·릴리스·테스트 문서
- 변경 사유: 간단한 질문과 조회까지 그래프가 생기는 문제를 막고 실제 파일 변경은 날짜별로 찾을 수 있게 하며, 우측 상태 이력을 읽기 쉽게 접고 앱을 사용자 선택에 따라 자동 갱신한다. WinExe를 stdio 서버로 직접 등록해 다른 PC에서 도구가 나타나지 않는 원인도 제거한다.
- 실행 명령: Release `dotnet build`·`dotnet test`; config·tracking·OAuth·전체 self-check; 소스·설치본 `scripts/test-mcp-stdio.ps1`; `scripts/build-installer.ps1`; 무인 업그레이드; `/api/health`; WPF UI Automation; 대시보드 HTML 계약 검사; `dotnet format whitespace --verify-no-changes`; `git diff --check`
- 검증 결과: 임시 SQLite 일일 기록 생성·정정·조회·삭제, 복잡도 정책, 상태 카드와 SSE 상태 보존, 정상·오염 업데이트 체크섬을 통과했다. 설치본은 protocol `2025-11-25`와 도구 10개를 제공하고, 앱 종료 뒤 브리지 호출이 설치 앱과 127.0.0.1 서버를 자동 복구했다. 초기 최종 설치 검사는 백그라운드 앱이 stdio 파이프를 상속해 클라이언트 종료가 지연됐고, 숨김 셸 실행으로 핸들 상속을 차단했다. 설치 설정은 업그레이드 전후 동일하게 보존됐다. 저장소 전체 포맷 검사는 기존 압축 스타일 파일도 보고해 이번에 추가한 C# 파일 범위로 재실행했고, 해당 검사와 전체 diff 검사가 통과했다.
- 남은 위험: 코드 서명이 없어 다른 PC에서 SmartScreen 경고가 표시될 수 있다. 실제 클릭 브라우저 자동화는 현재 런타임 부재로 HTML/SSE 자체 검사와 실행 서버 계약 검사로 대체했다.
- 다음 조치: 최종 커밋 기준 설치본을 다시 만들고 공개 릴리스의 SHA-256 다운로드 검증을 수행한다.

## 2026-09-04 - 월간 작업 그리드와 Gemini 일자별 요약

- 변경 파일: 월간 캘린더·작업 요약·설정 WPF 화면, `WorkflowSelectionViewModel`·`WorkflowWorkspaceViewModel` partial, Gemini 보안 저장·요약 클라이언트, Codex 요약 요청 coordinator와 MCP 도구, stdio 브리지, 자체 검사, README와 MCP·그래프 안내 문서
- 변경 사유: 첫 탭의 중복 현재 상태를 제거하고 날짜별 기록을 한 달 그리드로 찾으며, 최근 이벤트 대신 금일 또는 선택일자의 전체 프로젝트 작업을 LLM으로 읽기 쉽게 요약한다. Gemini를 우선 사용하고 사용할 수 없을 때만 CLI 의존성 없이 Codex에 맡긴다.
- 실행 명령: Release `dotnet build`·`dotnet test`; config·tracking·update·OAuth·전체 self-check; 소스 브리지 `scripts/test-mcp-stdio.ps1 -ExpectedToolCount 12`; 신규 C# 파일 범위 `dotnet format whitespace --verify-no-changes`; `git diff --check`
- 검증 결과: Release 빌드 경고 0·오류 0, 테스트와 자체 검사 5종이 종료 코드 0을 반환했다. 날짜 그리드는 선택 월의 실제 날짜만 최대 31일 포함하며 그래프·일일 기록 수를 표시한다. DPAPI 키 저장·읽기·삭제, `gemini-2.5-flash` URL·인증 헤더·응답 해석을 가짜 HTTP 응답으로 검증했다. Codex 대체 요청은 `get_daily_summary_request`와 `save_daily_summary_result`를 실제 HTTP MCP로 왕복했고 stdio protocol `2025-11-25`에서 총 12개 도구를 확인했다. 최초 종합 검사는 JSON 한글 escape를 원문으로 비교한 테스트 판정이 실패했으며 구조화 JSON 판정으로 수정한 뒤 통과했다.
- 남은 위험: 실제 Gemini 키와 외부 API의 실호출은 키를 저장한 사용자 환경에서 확인해야 한다. Codex Desktop 딥링크는 보안상 프롬프트를 자동 전송하지 않아 열린 새 작업에서 사용자가 전송을 한 번 눌러야 하며, 업데이트된 두 MCP 도구를 읽도록 Codex를 한 번 다시 시작해야 한다. 대기 중 SMSR 서버가 종료되면 메모리의 Codex 요약 요청은 만료된다. 저장소 전체 포맷 검사는 기존 압축 스타일의 수정 파일도 보고하므로 신규 파일 범위로 검증했다.
- 다음 조치: 설정에서 Gemini 키를 저장해 연결 확인 후 실제 일자 요약을 생성한다. 배포가 필요하면 버전을 결정하고 설치본을 생성해 설치 후 WPF 시각 확인과 Codex 재시작 검증을 수행한다.

## 2026-09-04 - SMSR v1.4.0 기능 릴리스

- 변경 파일: `src/SMSR.App/SMSR.App.csproj`, README, 설치 안내, `docs/releases/v1.4.0.md`, `docs/test-report-2026-09-04-v1.4.0.md`, 생성 설치 파일
- 변경 사유: 월간 작업 그리드와 Gemini 우선·Codex 대체 일자별 요약을 다른 Windows PC에서 설치·자동 업데이트할 수 있는 정식 기능 릴리스로 배포한다.
- 실행 명령: Release 빌드·테스트, config·tracking·update·OAuth·전체 self-check, `scripts/build-installer.ps1`, 무인 업그레이드, 설치본 자체 검사 5종, 설치본 stdio `initialize`·`tools/list`, `/api/health`, WPF UI Automation, 버전·SHA-256·설정 보존 확인
- 검증 결과: 앱·설치 버전 `1.4.0.0`, Release 빌드 경고 0·오류 0, 소스와 최종 설치본 자체 검사 5종 종료 코드 0을 확인했다. 설치본은 서버가 없는 상태에서 본체를 자동 복구하고 protocol `2025-11-25`와 도구 12개를 제공했다. 월간 그리드·두 요약 버튼·Gemini 설정 컨트롤을 실제 설치 WPF Automation 트리에서 확인했다. 기존 `settings.json`과 SQLite 해시는 업그레이드 전후 같고 publish·설치 앱 SHA-256은 `CADB77020C903CBBF66AAAC4862617DD3DD9689709B9FB187A68BF8AD08B5E7C`로 일치한다. Setup SHA-256은 `871C5FF0A870DD192F62A18413D8CDD80404DAAF5D0ED5752E7F3F5E31D89E6D`이다.
- 남은 위험: 실제 Gemini 호출은 사용자의 키가 있어야 확인할 수 있고 설치 파일은 코드 서명되지 않았다. Codex가 새 두 도구를 읽으려면 설치 후 한 번 완전히 다시 시작해야 한다. 서버가 중지된 기존 설치본이 단일 인스턴스를 점유한 상태의 소스 브리지 검사는 시작 대기에서 두 번 타임아웃돼 같은 재시도를 중단하고, 기존 프로세스를 정상 업그레이드한 뒤 최종 설치본 자동 복구 검사로 전환해 통과했다. 첫 WPF Automation 검사는 탭 헤더의 접근성 이름이 일반값이라 컨트롤을 찾지 못했으며 헤더 텍스트의 부모 탭을 선택하도록 검사 방식을 고쳐 통과했다.
- 다음 조치: 변경 커밋과 `v1.4.0` 태그를 `origin/main`에 푸시하고 GitHub 릴리스에 설치 EXE와 SHA-256 파일을 첨부한다.

## 2026-09-15 - Gemini API 확인과 요약 모델 선택

- 변경 파일: `GeminiSummaryClient.cs`, `AppSettingsService.cs`, Gemini 설정·작업 요약 ViewModel과 WPF 패널, `AiSummarySelfCheck.cs`, `README.md`, `docs/development-log.md`
- 변경 사유: Gemini 연결 확인과 요약 호출이 `gemini-2.5-flash`에 고정되어 API 키가 정상이어도 다른 모델을 선택하거나 모델별 실패 원인을 확인할 수 없었다.
- 실행 명령: 저장된 DPAPI 키로 Gemini `models.list`와 대체 모델 `generateContent` 실호출, `dotnet build SMSR.slnx -c Release`, Release 앱 `--self-test`, `git diff --check`
- 검증 결과: 실제 키의 모델 목록 API가 정상 응답하고 `generateContent` 지원 모델 41개를 반환했다. 대체 모델 실호출은 세 번에서 중단했다. `gemini-2.5-pro`와 `gemini-2.5-flash-lite`는 신규 사용자 404, `gemini-3.1-pro-preview`는 무료 할당량 0의 429를 반환해 키 오류가 아닌 모델별 사용 정책임을 확인했고, 목록만으로 할당량을 추측하는 대신 선택 모델을 연결 확인하는 흐름으로 전환했다. 모델 조회·선택·저장·선택 모델 URL과 오류 메시지 계약을 자체 검사했고 Release 빌드 경고 0·오류 0, 전체 자체검사 종료 코드 0을 확인했다.
- 남은 위험: 모델 목록 응답은 실제 호출 할당량을 보장하지 않으므로 선택 뒤 연결 확인이 필요하다. 이미지·TTS·전사 모델은 텍스트 요약 선택지에서 제외하며, 실행 중 설치본에는 다음 배포 버전 설치가 필요하다.
- 다음 조치: 앱 설정에서 연결 확인으로 모델 목록을 갱신하고 사용할 모델을 선택한 뒤 다시 연결 확인한다. 배포가 필요하면 새 버전의 설치본을 생성한다.

## 2026-09-15 - Gemini 3.8 Flash 기본 모델 적용

- 변경 파일: `GeminiSummaryClient.cs`, `README.md`, `docs/development-log.md`
- 변경 사유: 사용자가 요약 기본 모델로 `gemini-3.8-flash`를 지정했으며 Gemini 3 계열은 기존 sampling 설정 대신 thinking level 설정이 필요하다.
- 실행 명령: 저장된 DPAPI 키로 `gemini-3.8-flash` 실호출, Release 빌드, 전체 자체검사, diff 검사
- 검증 결과: 기본 모델을 `gemini-3.8-flash`로 바꾸고, 매 요청에서 사용자가 선택한 모델을 판별해 Gemini 3·`latest` 계열에는 `thinkingLevel=low`, 이전 모델에는 기존 `temperature`를 적용하도록 분기했다. 두 요청 형식을 자체검사로 고정했고 저장된 실제 API 키로 3.8 Flash가 `OK`를 반환했으며 Release 빌드 경고 0·오류 0, 전체 자체검사 종료 코드 0을 확인했다.
- 남은 위험: 현재 실행 중인 `1.4.0` 설치본에는 다음 배포 버전을 설치해야 변경이 반영된다.
- 다음 조치: `record_daily_activity`가 같은 활동 ID로 세 차례 모두 60초 시간 초과되어 재시도를 중단했다. SMSR MCP 응답 복구 후 같은 ID의 활동 카드를 정정하고, 필요하면 새 설치본을 배포한다.

## 2026-09-15 - 첫 화면에서 선택일 작업 요약

- 변경 파일: `MainWindow.xaml`, `WorkflowPanel.xaml`, 삭제한 `WorkflowHistoryPanel`, `README.md`, `docs/mcp-connection.md`, `docs/development-log.md`
- 변경 사유: 별도 작업 요약 탭에는 날짜 선택이 없어 첫 화면 캘린더와 오가야 했다.
- 실행 명령: Release 빌드, 전체 자체검사, diff 검사
- 검증 결과: 기존 날짜 선택과 요약 명령을 재사용해 작업 현황 화면에서 오늘 또는 캘린더 선택일을 요약하고 결과도 바로 확인하도록 통합했다.
- 남은 위험: 여러 날짜 범위 통합 요약은 지원하지 않는다.
- 다음 조치: 새 설치본 배포 후 실제 WPF 화면에서 캘린더 선택과 요약 결과 스크롤을 확인한다.

## 2026-09-15 - 작업 현황 목록 단순화

- 변경 파일: `WorkflowPanel.xaml`, `WorkflowChoice.cs`, `MvpSelfCheck.cs`, README와 그래프·MCP 안내 문서
- 변경 사유: 작업 현황에 캘린더와 별도로 복잡 작업 그래프·일일 기록 목록이 중복되고 워크플로우 선택 항목에도 날짜·상태가 함께 표시됐다.
- 실행 명령: Release 빌드, 전체 자체검사, diff 검사
- 검증 결과: 상세 그래프와 일일 작업 목록을 화면에서 제거하고 워크플로우 선택 항목은 작업명만 표시하도록 단순화했다.
- 남은 위험: 상세 그래프는 대시보드에서 확인해야 한다.
- 다음 조치: 새 설치본에서 긴 작업명의 말줄임과 요약 영역 배치를 확인한다.

## 2026-09-15 - 기간 요약·스크롤·Gemini 오류·격리 검사 수정

- 변경 파일: 작업 캘린더·요약 ViewModel과 WPF 패널, Gemini 클라이언트·자체검사, stdio 호스트·검사 스크립트, 설치 UI, README와 MCP 안내 문서
- 변경 사유: 캘린더 선택일은 하루만 요약했고 긴 작업 현황 화면의 휠 이동이 느렸다. Gemini 실제 실패가 `미연결`로 덮였으며 릴리스 검사가 실행 중 앱을 강제 종료하거나 테스트용 백그라운드 앱을 남길 수 있었다.
- 실행 명령: Release 빌드, 전체 자체검사, 격리 stdio 도구 목록 검사와 전후 프로세스 비교, 설치본 빌드와 전후 프로세스 비교, diff 검사
- 검증 결과: 시작일에서 그리드 선택 종료일까지 최대 1년을 포함해 조회·요약하고, 휠 delta를 바깥 ScrollViewer에 직접 적용했다. Gemini 비텍스트 응답 부분을 건너뛰고 실제 오류를 Codex 대체 상태에 보존한다. 설치 전 전역 `taskkill`을 제거하고 stdio 검사는 대시보드 시작을 건너뛰며 protocol `2025-11-25`·도구 12개와 신규 SMSR 프로세스 0개를 확인했다. 설치본 빌드 중에도 SMSR 프로세스를 새로 만들거나 종료하지 않았다.
- 남은 위험: 실제 설치본의 WPF 체감 스크롤과 Inno Setup Restart Manager의 정상 업그레이드 종료 동작은 설치 환경에서 확인해야 한다.
- 다음 조치: 실제 배포 전 버전을 올리고 새 설치본에서 날짜 범위·실제 Gemini 요약·실행 중 앱 업그레이드를 확인한다.

## 2026-09-15 - 달력 두 번 클릭 기간 선택

- 변경 파일: `WorkflowPanel.xaml`, `WorkflowPanel.xaml.cs`, `WorkflowSelectionViewModel.Calendar.cs`, `WorkflowSelectionViewModel.CalendarNavigation.cs`, `MvpSelfCheck.cs`, 관련 안내 문서
- 변경 사유: 별도 시작일 입력은 날짜 그리드를 이용한 기간 선택 흐름과 맞지 않았다.
- 실행 명령: Release 빌드, 전체 자체검사, 설치본 재생성과 전후 프로세스 비교, diff 검사
- 검증 결과: 첫 클릭은 `단일`, 두 번째 클릭은 두 날짜 사이 셀 전체를 강조한 `기간`, 세 번째 클릭은 새 단일 선택으로 동작한다. 역순 클릭도 날짜를 정렬하며 Release 빌드 경고 0·오류 0, 자체검사 종료 코드 0을 확인했다. 최신 설치본 생성 중 기존 SMSR PID `33024`가 유지됐고 신규 SMSR 프로세스는 0개였다.
- 남은 위험: 실제 WPF 화면에서 여러 주에 걸친 선택 강조와 휠 스크롤 체감을 확인해야 한다.
- 다음 조치: 배포 전 버전을 올리고 새 설치본에서 달력 상호작용을 확인한다.

## 2026-09-15 - 오늘 배지와 기간 선택 강조

- 변경 파일: `CalendarDayItem.cs`, 캘린더 ViewModel·WPF 패널, `MvpSelfCheck.cs`, `docs/development-log.md`
- 변경 사유: 오늘 표시와 선택 테두리가 겹쳐 오늘 표기가 사라진 것처럼 보였고, 기간의 시작·중간·종료 차이가 약했다.
- 실행 명령: Release 빌드, 전체 자체검사, 설치본 재생성, diff 검사
- 검증 결과: 오늘은 선택 상태와 독립된 `오늘` 배지로 표시한다. 단일·기간 시작·기간 종료는 3px 테두리, 기간 중간은 연한 배경과 1px 테두리로 구분하며 월 이동 중에도 선택 범위를 유지한다. Release 빌드 경고 0·오류 0, 자체검사 종료 코드 0, 설치본 빌드 성공과 신규 SMSR 프로세스 0개를 확인했다.
- 남은 위험: 실제 WPF 화면에서 밝은·어두운 테마의 색 대비를 확인해야 한다.
- 다음 조치: 새 설치본에서 오늘 배지와 여러 주·월에 걸친 기간 강조를 확인한다.

## 2026-09-15 - 선택일 목록과 Gemini 키 표시

- 변경 파일: `WorkflowPanel.xaml`, `SettingsAiPanel.xaml`, `SettingsAiPanel.xaml.cs`, Gemini 설정·캘린더 선택 ViewModel, `EventStoreWorkflowCalendar.cs`, `MvpSelfCheck.cs`, 관련 안내 문서
- 변경 사유: 닫힌 워크플로우 선택 상자에 객체 문자열이 보였고, 저장된 Gemini 키를 마스킹 상태로 다시 불러오거나 눈 버튼으로 확인할 수 없었다. 날짜 선택도 상단 프로젝트·워크플로우 목록을 필터링하지 않았다.
- 실행 명령: Release 빌드, 전체 자체검사, 설치본 빌드와 전후 프로세스 비교, diff 검사
- 검증 결과: `DisplayMemberPath`로 작업명만 표시하고, DPAPI 키를 마스킹 상태로 불러와 눈 버튼으로만 평문 전환한다. 워크플로우 캘린더 쿼리는 마지막 날짜 한 건이 아니라 활동 일자별 행을 반환하며, 선택일에 그래프가 있는 프로젝트와 해당 프로젝트의 워크플로우만 상단 목록에 채운다. DPAPI 키 UI 복원·서로 다른 날짜의 같은 워크플로우·선택일 필터 자체검사와 Release 빌드가 통과했다. 설치본 생성 중 기존 SMSR PID `28316`이 유지됐고 신규 프로세스는 0개였다.
- 실패 및 재계획: 자체검사가 세 차례 `기존 UUID 워크플로우 표시명`에서 실패했다. 첫 시도는 새 날짜 필터가 다른 날짜 항목을 제외했고, 두 번째는 테스트 뒤 프로젝트 복원이 부족했으며, 세 번째 진단은 실제 결과가 `demo/wf-1/2026-09-15`로 올바르고 기존 검사가 다른 날짜의 UUID 항목까지 요구함을 확인했다. 재시도를 중단하고 검사 기준을 현재 선택일·프로젝트의 표시 항목으로 변경했다.
- 남은 위험: 실제 WPF의 마스킹·눈 버튼과 긴 작업명 말줄임을 확인해야 한다.
- 다음 조치: 새 설치본에서 첨부 화면과 같은 데이터로 선택일 목록을 확인한다.

## 2026-09-15 - 선택 상자 제목과 기간 목록 수정

- 변경 파일: `WorkflowPanel.xaml`, 캘린더 선택 ViewModel, `MvpSelfCheck.cs`, 관련 안내 문서
- 변경 사유: 펼친 목록에서는 작업명이 보이지만 닫힌 워크플로우 선택 상자에는 선택 객체가 명시되지 않았고, 기간 선택 후에도 목록 필터가 종료일 한 날짜만 사용했다.
- 실행 명령: Release 빌드, 전체 자체검사, 설치본 빌드와 전후 프로세스 비교, diff 검사
- 검증 결과: ComboBox의 `SelectedItem`을 `SelectedWorkflow`에 연결해 닫힌 상태에서도 제목을 표시한다. 프로젝트와 워크플로우 목록은 선택 시작일~종료일 전체에서 찾고, 같은 워크플로우의 여러 날짜 행은 한 항목으로 합친다. 기간 목록·선택 제목 자체검사와 Release 빌드가 통과했다. 최신 설치본은 63,426,484 bytes, SHA256 `C319E82CBB33271CA27E73290FF4585B39666F05F0922AADFDE8CE4AB2423785`이며 신규 SMSR 프로세스는 0개였다.
- 남은 위험: 실제 WPF에서 매우 긴 제목의 말줄임 표시를 확인해야 한다.
- 다음 조치: 새 설치본에서 단일·기간 선택별 목록을 확인한다.

## 2026-09-15 - 닫힌 워크플로우 제목 표시

- 변경 파일: `WorkflowChoice.cs`, `MvpSelfCheck.cs`, `docs/development-log.md`
- 변경 사유: WPF 테마의 닫힌 ComboBox 선택 영역이 `DisplayMemberPath` 대신 레코드 기본 `ToString()`을 렌더링했다.
- 실행 명령: Release 빌드, 전체 자체검사, 설치본 빌드와 전후 프로세스 비교, diff 검사
- 검증 결과: `WorkflowChoice.ToString()`이 제목을 반환하도록 해 펼친 목록과 닫힌 선택 영역 모두 작업 제목만 표시한다. Release 빌드·전체 자체검사가 통과했고, 최신 설치본은 63,412,371 bytes, SHA256 `D8CF600CAD8470F62824609CE05E3921248A49C2800897ABF4DA91E0BE662C06`이다. 설치본 생성 중 기존 SMSR PID `30820`이 유지됐고 신규 프로세스는 0개였다.
- 남은 위험: 실제 설치본 화면 확인이 필요하다.
- 다음 조치: 새 설치본에서 닫힌 워크플로우 선택 영역을 확인한다.

## 2026-09-15 - v1.4.1 릴리스 준비

- 변경 파일: `SMSR.App.csproj`, `docs/releases/v1.4.1.md`, `docs/test-report-2026-09-15-v1.4.1.md`, 설치 안내와 README
- 변경 사유: 현재 날짜·Gemini·릴리스 검사 개선사항을 신규 정식 버전으로 배포한다.
- 실행 명령: 실행 예정
- 검증 결과: 버전을 `1.4.1`로 올리고 릴리스 문서와 설치 파일명을 갱신했다.
- 남은 위험: 전체 검증, 패키징, GitHub 게시가 남아 있다. GitHub CLI는 로그인되어 있지 않다.
- 다음 조치: Release 빌드·자체검사·stdio 검사·설치본 빌드 후 게시한다.

## 2026-09-15 - 그래프 종료와 노드별 작업 상세

- 변경 파일: `DashboardPage.cs`, `DashboardPanels.cs`, 추적 지침·도구 설명, 자체검사, 릴리스 문서
- 변경 사유: 노드를 선택해도 우측 활동·상태 기록이 전체 노드를 표시했고, 작업 종료 뒤 진행 상태 노드가 남을 수 있었다.
- 실행 명령: Release 빌드, 전체 자체검사
- 검증 결과: 선택 노드 ID로 활동·상태 목록을 함께 필터링하고 다른 노드 표식이 출력되지 않는 계약 검사를 추가했다. 최종 응답 전 상태를 재조회해 남은 노드를 `SUCCESS`, `FAILED`, `BLOCKED`로 종결하도록 MCP·Codex 훅 지침을 강화했다. Release 빌드 경고 0·오류 0, 자체검사 종료 코드 0이다.
- 남은 위험: 실행 도중 강제 종료된 클라이언트는 최종 이벤트를 보낼 수 없다.
- 다음 조치: stdio·설치본 검증 후 v1.4.1을 게시한다.

## 2026-09-15 - 그래프 중단 상태

- 변경 파일: 이벤트·일일 기록 검증, 그래프 계층·스타일·상태 카드, 워크플로우 목록 SQL, 추적 지침, 자체검사, 관련 문서
- 변경 사유: 작업이 완료되지 않은 채 다음 요청으로 넘어가는 경우를 성공·실패·차단과 구분할 최종 상태가 없었다.
- 실행 명령: Release 빌드, 전체 자체검사
- 검증 결과: `CANCELLED`를 허용하고 목록·달력에서 종결 상태로 계산하며 그래프와 상태 카드에 `중단`으로 표시한다. 활동 훅도 중단 이벤트에서 추적 세션을 닫는다. Release 빌드 경고 0·오류 0, 자체검사 종료 코드 0이다.
- 남은 위험: 클라이언트가 강제 종료되어 이벤트 자체를 보낼 수 없는 경우는 상태가 남을 수 있다.
- 다음 조치: 최종 stdio·설치본 검증 후 v1.4.1을 게시한다.

## 2026-09-15 - v1.4.1 최종 패키징

- 변경 파일: `docs/releases/v1.4.1.md`, `docs/test-report-2026-09-15-v1.4.1.md`, `docs/development-log.md`, `artifacts/installer` 생성물
- 변경 사유: 날짜·Gemini·그래프 상세·중단 상태 개선을 신규 정식 버전으로 배포한다.
- 실행 명령: Release 빌드·test·전체 자체검사, 격리 stdio 검사, `scripts/build-installer.ps1`, publish 앱 자체검사·stdio 검사, 버전·SHA-256 확인
- 검증 결과: 앱·설치 파일 `1.4.1.0`, Release 빌드 경고 0·오류 0, 자체검사 종료 코드 0, protocol `2025-11-25`와 도구 12개를 확인했다. 설치 파일은 63,427,689 bytes, SHA-256 `B0924891E81B2EBE5E293A4ABFB88E4CF5CD39C0C07ABF0E4FF41058DB503CC8`이다. 패키징·stdio 검사 중 기존 SMSR PID `39932`가 유지됐고 신규 프로세스는 0개였다.
- 남은 위험: 설치 파일은 코드 서명되지 않았고 실제 설치 실행·WPF 수동 확인은 생략했다. GitHub CLI는 로그인되어 있지 않다.
- 다음 조치: 커밋·태그를 원격에 전송하고 GitHub Release에 설치 파일과 체크섬을 게시한다.

## 2026-09-15 - v1.4.1 GitHub 게시

- 변경 파일: `docs/development-log.md`
- 변경 사유: 최종 원격 게시 결과와 자산 검증을 기록한다.
- 실행 명령: `git push origin main`, `git push origin v1.4.1`, GitHub Release 생성·조회
- 검증 결과: 릴리스 커밋 `fc28fd4`와 태그 `v1.4.1`을 전송하고 정식 최신 릴리스를 게시했다. 원격 자산은 설치 EXE 63,427,689 bytes와 SHA-256 파일 96 bytes이며 설치 EXE digest는 `B0924891E81B2EBE5E293A4ABFB88E4CF5CD39C0C07ABF0E4FF41058DB503CC8`로 일치한다.
- 남은 위험: 코드 서명이 없고 실제 설치 실행·WPF 수동 확인은 남아 있다.
- 다음 조치: 설치 환경에서 업그레이드 후 Codex를 완전히 다시 시작한다.

## 2026-09-15 - 노드별 이력 카드와 에이전트 표시

- 변경 파일: `EventStoreStateQueries.cs`, `LocalServerEndpoints.cs`, `DashboardHistoryCards.cs`, `DashboardPanels.cs`, `DashboardStyles.cs`, `MvpSelfCheck.cs`, 버전·릴리스 문서
- 변경 사유: 선택 노드 상세가 해당 노드의 최신 상태 한 장만 표시했고, 에이전트 카드가 내부 ID·역할·상태를 그대로 노출해 읽기 어려웠다.
- 실행 명령: Release 빌드, 전체 자체검사
- 검증 결과: 선택 노드는 DB에서 직접 최근 이력 최대 50건을 조회해 상태 변경별 카드로 표시하고 다른 노드 이력을 제외한다. 전체 화면은 노드당 최신 카드 한 장을 유지한다. 에이전트 카드에는 한국어 이름·역할·상태와 실제 작업 제목을 우선 표시한다. Release 빌드 경고 0·오류 0, 자체검사 종료 코드 0이다.
- 남은 위험: 실제 설치본 화면과 매우 긴 작업 제목은 수동 확인이 필요하다.
- 다음 조치: v1.4.2 설치본을 검증하고 게시한다.

## 2026-09-15 - v1.4.2 최종 패키징

- 변경 파일: `docs/test-report-2026-09-15-v1.4.2.md`, `docs/development-log.md`, `artifacts/installer` 생성물
- 변경 사유: 노드별 이력과 에이전트 표시 개선을 설치 가능한 정식 버전으로 배포한다.
- 실행 명령: Release 빌드·test·전체 자체검사, 격리 stdio 검사, `scripts/build-installer.ps1`, publish 앱 자체검사·stdio 검사, 버전·SHA-256 확인
- 검증 결과: 앱·설치 파일 `1.4.2.0`, Release 빌드 경고 0·오류 0, 자체검사 종료 코드 0, protocol `2025-11-25`와 도구 12개를 확인했다. 설치 파일은 63,435,431 bytes, SHA-256 `DBFEC3A7C2F23BD8B75BAFE46DAF5AB96E44CA236805EA90626F196DCCA65814`다. 기존 SMSR PID `18904`가 유지됐고 신규 프로세스는 0개였다.
- 남은 위험: 설치 파일은 코드 서명되지 않았고 실제 설치 실행·WPF 수동 확인은 생략했다.
- 다음 조치: 커밋·태그·GitHub Release를 게시한다.

## 2026-09-15 - v1.4.2 GitHub 게시

- 변경 파일: `docs/development-log.md`
- 변경 사유: 최종 게시 결과와 원격 자산 검증을 기록한다.
- 실행 명령: `git push origin main`, `git push origin v1.4.2`, GitHub Release 생성·조회
- 검증 결과: 릴리스 커밋 `fd4bc68`과 태그 `v1.4.2`를 전송하고 정식 최신 릴리스를 게시했다. 원격 설치 EXE는 63,435,431 bytes이며 digest `DBFEC3A7C2F23BD8B75BAFE46DAF5AB96E44CA236805EA90626F196DCCA65814`가 로컬과 일치한다.
- 남은 위험: 코드 서명이 없고 실제 설치 화면의 수동 확인은 남아 있다.
- 다음 조치: v1.4.2로 업그레이드한 뒤 Codex를 완전히 다시 시작한다.

## 2026-09-15 - 프로젝트 기간 AI 요약·질의 팝업

- 변경 파일: `WorkflowPanel.xaml`, `WorkflowPanel.xaml.cs`, `WorkflowWorkspaceViewModel*.cs`, `DailyWorkSummaryPrompt.cs`, `AiSummarySelfCheck.cs`, 버전·릴리스 문서
- 변경 사유: 캘린더 본문의 요약 UI를 단순화하고 선택한 프로젝트·기간 기록에 대해 작업 이유와 필요성을 질문할 수 있게 한다.
- 실행 명령: `dotnet build SMSR.slnx -c Release --no-restore`, `dotnet test SMSR.slnx -c Release --no-build`, Release 앱 `--self-test`, 격리 stdio 검사
- 검증 결과: Release 빌드 경고 0·오류 0, 자체검사 종료 코드 0, protocol `2025-11-25`와 도구 12개를 확인했다. 질문 길이·기간 검증과 기록 외 추측 방지 프롬프트 자체검사를 추가했다.
- 남은 위험: 실제 설치본 팝업의 화면 크기별 수동 확인이 필요하다.
- 다음 조치: v1.4.3 설치본을 생성하고 격리 검증 후 게시한다.

## 2026-09-15 - v1.4.3 최종 패키징

- 변경 파일: `docs/releases/v1.4.3.md`, `docs/test-report-2026-09-15-v1.4.3.md`, `docs/development-log.md`, `artifacts/installer` 생성물
- 변경 사유: 프로젝트 기간 AI 요약·질의 팝업을 설치 가능한 정식 버전으로 배포한다.
- 실행 명령: Release 빌드·test·전체 자체검사, 격리 stdio 검사, `scripts/build-installer.ps1`, publish 앱 자체검사·stdio 검사, 버전·SHA-256 확인
- 검증 결과: 앱·설치 파일 `1.4.3.0`, Release 빌드 경고 0·오류 0, Release·publish 자체검사 종료 코드 0, protocol `2025-11-25`와 도구 12개를 확인했다. 설치 파일은 63,427,814 bytes, SHA-256 `AD3EE82D6D2752B9F7438166B35DAC805AB8CB484A67AC9D99790A7B194C483F`다. 기존 SMSR PID `18904`가 유지됐고 신규 프로세스는 0개였다.
- 남은 위험: 설치 파일은 코드 서명되지 않았고 실제 설치 실행·팝업 수동 확인은 생략했다.
- 다음 조치: 커밋·태그·GitHub Release를 게시한다.

## 2026-09-15 - v1.4.3 GitHub 게시

- 변경 파일: `docs/development-log.md`
- 변경 사유: 최종 게시 결과와 원격 자산 검증을 기록한다.
- 실행 명령: `git push origin main`, `git push origin v1.4.3`, GitHub Release 생성·조회
- 검증 결과: 릴리스 커밋 `9dea037`과 태그 `v1.4.3`을 전송하고 정식 릴리스를 게시했다. 원격 설치 EXE는 63,427,814 bytes이며 digest `AD3EE82D6D2752B9F7438166B35DAC805AB8CB484A67AC9D99790A7B194C483F`가 로컬과 일치한다.
- 남은 위험: 코드 서명이 없고 실제 설치 화면의 수동 확인은 남아 있다.
- 다음 조치: v1.4.3으로 업그레이드한 뒤 팝업 요약·질의를 확인한다.

## 2026-09-15 - 그래프 사용자 요청 요약과 한글 질의 입력

- 변경 파일: `SmsrMcpInstructions.cs`, `CodexAutoTrackingContext.cs`, `WorkflowTools.cs`, `StdioWorkflowTools.cs`, `WorkflowPanel.xaml`, `WorkflowWorkspaceViewModel.cs`, 자체검사와 관련 문서
- 변경 사유: 지정된 그래프에 최초·관련 후속 사용자 요청의 맥락을 남기고, AI 질의 입력 중 한글 조합이 끊기는 현상을 해결한다.
- 실행 명령: Release 빌드, 전체 자체검사
- 검증 결과: 요청 원문 대신 1~2문장 요약만 노드 시작 이력에 기록하도록 MCP·Codex 훅 지침과 도구 설명을 통일했다. TextBox의 매 글자 소스 갱신과 질문 내용 기반 명령 재평가를 제거하고 실행 시 입력 검증은 유지했다. Release 빌드 경고 0·오류 0, 자체검사 종료 코드 0이다.
- 남은 위험: 이미 완료된 그래프에는 과거 요청 요약이 소급 생성되지 않는다.
- 다음 조치: v1.4.4 설치본을 생성하고 격리 검증 후 게시한다.

## 2026-09-15 - v1.4.4 최종 패키징

- 변경 파일: `docs/releases/v1.4.4.md`, `docs/test-report-2026-09-15-v1.4.4.md`, `docs/development-log.md`, `artifacts/installer` 생성물
- 변경 사유: 그래프 요청 이력과 한글 질의 입력 개선을 설치 가능한 정식 버전으로 배포한다.
- 실행 명령: Release 빌드·test·전체 자체검사, 격리 stdio 검사, `scripts/build-installer.ps1`, publish 앱 자체검사·stdio 검사, 버전·SHA-256 확인
- 검증 결과: 앱·설치 파일 `1.4.4.0`, Release 빌드 경고 0·오류 0, Release·publish 자체검사 종료 코드 0, protocol `2025-11-25`와 도구 12개를 확인했다. 설치 파일은 63,435,617 bytes, SHA-256 `1F9898467FE1FE44A4AFFB63B263767DF177F1CEF4E03CBDA83296053EE05F57`다. 검사 전후 SMSR 프로세스는 없었고 새 프로세스가 남지 않았다.
- 남은 위험: 설치 파일은 코드 서명되지 않았고 실제 한글 IME 수동 입력 확인은 생략했다.
- 다음 조치: 커밋·태그·GitHub Release를 게시한다.

## 2026-09-15 - v1.4.4 GitHub 게시

- 변경 파일: `docs/development-log.md`
- 변경 사유: 최종 게시 결과와 원격 자산 검증을 기록한다.
- 실행 명령: `git push origin main`, `git push origin v1.4.4`, GitHub Release 생성·조회
- 검증 결과: 릴리스 커밋 `dab8b43`과 태그 `v1.4.4`를 전송하고 정식 릴리스를 게시했다. 원격 설치 EXE는 63,435,617 bytes이며 digest `1F9898467FE1FE44A4AFFB63B263767DF177F1CEF4E03CBDA83296053EE05F57`가 로컬과 일치한다.
- 남은 위험: 코드 서명이 없고 실제 설치 화면에서 한글 IME 입력을 수동 확인해야 한다.
- 다음 조치: v1.4.4로 업그레이드하고 SMSR·Codex를 완전히 다시 시작한다.

## 2026-09-15 - Gemini 대체 모델 확인과 한글 IME 재수정

- 변경 파일: `GeminiSummaryClient.cs`, `WorkflowWorkspaceViewModel.Summaries.cs`, `WorkflowPanel.xaml`, `WorkflowPanel.xaml.cs`, `AiSummarySelfCheck.cs`, 버전·릴리스 문서
- 변경 사유: Gemini 503 후 곧바로 Codex로 전환되어 다른 모델 선택 기회가 없었고, 투명 WPF Popup 내부 질의 TextBox에서 한글 조합이 계속 끊겼다.
- 실행 명령: Release 빌드, 전체 자체검사, 격리 stdio 검사
- 검증 결과: 404·429·503에서 사용 가능한 대체 Flash 모델을 찾아 현재 요청만 재시도할지 확인한다. 별도 투명 Popup HWND를 제거하고 같은 창의 모달 오버레이와 LostFocus 바인딩으로 교체했다. Release 빌드 경고 0·오류 0, 자체검사 종료 코드 0, protocol `2025-11-25`와 도구 12개를 확인했다.
- 남은 위험: 실제 Windows 한글 IME 키보드 입력과 Gemini 실 API의 503 대체 확인창은 설치 환경에서 수동 확인이 필요하다.
- 다음 조치: v1.4.5 설치본을 생성하고 격리 검증 후 게시한다.

## 2026-09-15 - v1.4.5 최종 패키징

- 변경 파일: `docs/releases/v1.4.5.md`, `docs/test-report-2026-09-15-v1.4.5.md`, `docs/development-log.md`, `artifacts/installer` 생성물
- 변경 사유: Gemini 대체 모델 확인과 한글 IME 재수정을 설치 가능한 정식 버전으로 배포한다.
- 실행 명령: Release 빌드·test·전체 자체검사, 격리 stdio 검사, `scripts/build-installer.ps1`, publish 앱 자체검사·stdio 검사, 버전·SHA-256 확인
- 검증 결과: 앱·설치 파일 `1.4.5.0`, Release 빌드 경고 0·오류 0, Release·publish 자체검사 종료 코드 0, protocol `2025-11-25`와 도구 12개를 확인했다. 설치 파일은 63,417,904 bytes, SHA-256 `759C44A277FA5DB97BABB1261DFDE11B06B731425E3C728861E0ED40E97C1720`다. 검사 전후 `SMSR.Bridge:41432`, `SMSR.App:42248`가 유지됐고 새 프로세스가 남지 않았다.
- 남은 위험: 설치 파일은 코드 서명되지 않았고 실제 한글 IME와 Gemini 503 대체 확인창의 수동 확인은 남아 있다.
- 다음 조치: 커밋·태그·GitHub Release를 게시한다.

## 2026-09-15 - v1.4.5 GitHub 게시

- 변경 파일: `docs/development-log.md`
- 변경 사유: 최종 게시 결과와 원격 자산 검증을 기록한다.
- 실행 명령: `git push origin main`, `git push origin v1.4.5`, GitHub Release 생성·조회
- 검증 결과: 릴리스 커밋 `43e3311`과 태그 `v1.4.5`를 전송하고 정식 릴리스를 게시했다. 원격 설치 EXE는 63,417,904 bytes이며 digest `759C44A277FA5DB97BABB1261DFDE11B06B731425E3C728861E0ED40E97C1720`가 로컬과 일치한다.
- 남은 위험: 코드 서명이 없고 실제 설치 화면에서 한글 IME와 Gemini 503 대체 확인창을 수동 확인해야 한다.
- 다음 조치: v1.4.5로 업그레이드하고 SMSR·Codex를 완전히 다시 시작한다.

## 2026-09-15 - Gemini 대체 모델 최신 버전 선택

- 변경 파일: `GeminiSummaryClient.cs`, `AiSummarySelfCheck.cs`, 버전·릴리스 문서
- 변경 사유: 일시 장애가 난 현재 모델 대신 계정에서 지원 종료된 `gemini-2.5-flash`를 제안해 재시도가 404로 실패했다.
- 실행 명령: Release 빌드, 전체 자체검사, 격리 stdio 검사
- 검증 결과: 모델 목록에서 현재 모델과 Preview를 제외한 Flash 버전을 내림차순으로 선택하고, 같은 버전이면 일반 Flash를 Lite보다 우선하도록 수정했다.
- 남은 위험: 실제 Gemini 계정별 모델 호출 가능 여부는 서버 응답에 따라 달라진다.
- 다음 조치: v1.4.6 설치본을 생성하고 격리 검증 후 게시한다.

## 2026-09-15 - v1.4.6 최종 패키징

- 변경 파일: `docs/releases/v1.4.6.md`, `docs/test-report-2026-09-15-v1.4.6.md`, `docs/development-log.md`, `artifacts/installer` 생성물
- 변경 사유: Gemini 대체 모델 선택 수정을 설치 가능한 정식 버전으로 배포한다.
- 실행 명령: Release 빌드·test·전체 자체검사, 격리 stdio 검사, `scripts/build-installer.ps1`, publish 앱 자체검사·stdio 검사, 버전·SHA-256 확인
- 검증 결과: 앱·설치 파일 `1.4.6.0`, Release 빌드 경고 0·오류 0, Release·publish 자체검사 종료 코드 0, protocol `2025-11-25`와 도구 12개를 확인했다. 설치 파일은 63,434,183 bytes, SHA-256 `117265D8826BD5977F4A4737AFCF597DB4CD9FE25B93B43BA16E06A76774612C`다. 검사 전후 기존 PID `3904`, `4356`가 유지됐고 새 프로세스가 남지 않았다.
- 남은 위험: 설치 파일은 코드 서명되지 않았고 실제 계정의 Gemini 호출 결과는 서버 가용성에 좌우된다.
- 다음 조치: 커밋·태그·GitHub Release를 게시한다.

## 2026-09-15 - v1.4.6 GitHub 게시

- 변경 파일: `docs/development-log.md`
- 변경 사유: 최종 게시 결과와 원격 자산 검증을 기록한다.
- 실행 명령: `git push origin main`, `git push origin v1.4.6`, GitHub Release 생성·조회
- 검증 결과: 릴리스 커밋 `a650cc4`와 태그 `v1.4.6`을 전송하고 정식 릴리스를 게시했다. 원격 설치 EXE는 63,434,183 bytes이며 digest `117265D8826BD5977F4A4737AFCF597DB4CD9FE25B93B43BA16E06A76774612C`가 로컬과 일치한다.
- 남은 위험: 코드 서명이 없고 실제 Gemini 계정의 모델 가용성은 호출 시점에 따라 달라진다.
- 다음 조치: v1.4.6으로 업그레이드하고 SMSR·Codex를 완전히 다시 시작한다.

## 2026-09-15 - 도구 실행 시작 실시간 표시

- 변경 파일: `CodexAutoTrackingHook.cs`, `CodexActivityClassifier.cs`, `DashboardPage.cs`, `DashboardPanels.cs`, `DashboardLiveUpdates.cs`, `DashboardStyles.cs`, 자체검사와 릴리스 문서
- 변경 사유: 기존 훅은 `PostToolUse`만 기록해 오래 실행되는 명령이 끝난 뒤에야 대시보드에 나타났다.
- 실행 명령: Release 빌드, 전체 자체검사, 격리 stdio 검사
- 검증 결과: `PreToolUse`를 `TOOL_STARTED`로 기록하고 기존 추적 세션의 활성 노드에 연결한다. 대시보드는 작업 제목·한국어 활동명·진행 경과 시간·SSE 연결 상태를 표시한다. Release 빌드 경고 0·오류 0, 자체검사 종료 코드 0, protocol `2025-11-25`와 도구 12개를 확인했다.
- 남은 위험: 이미 실행 중인 Codex에는 새 훅 정의가 반영되지 않으므로 설치 후 재시작이 필요하다.
- 다음 조치: v1.4.7 설치본을 생성하고 격리 검증 후 게시한다.

## 2026-09-15 - v1.4.7 최종 패키징

- 변경 파일: `docs/releases/v1.4.7.md`, `docs/test-report-2026-09-15-v1.4.7.md`, `docs/development-log.md`, `artifacts/installer` 생성물
- 변경 사유: 실시간 도구 시작 표시를 설치 가능한 정식 버전으로 배포한다.
- 실행 명령: Release 빌드·test·전체 자체검사, 격리 stdio 검사, `scripts/build-installer.ps1`, publish 앱 자체검사·stdio 검사, 버전·SHA-256 확인
- 검증 결과: 앱·설치 파일 `1.4.7.0`, Release 빌드 경고 0·오류 0, Release·publish 자체검사 종료 코드 0, protocol `2025-11-25`와 도구 12개를 확인했다. 설치 파일은 63,416,657 bytes, SHA-256 `E3C62BCB317DB2529D2098B7B94A62CF2560F1328F8BD34207CAE8EF22BFAA72`다. 검사 전후 기존 PID 6개가 유지됐고 새 프로세스가 남지 않았다.
- 남은 위험: 설치 파일은 코드 서명되지 않았고 기존 Codex 작업은 재시작 전까지 새 훅을 사용하지 않는다.
- 다음 조치: 커밋·태그·GitHub Release를 게시한다.

## 2026-09-15 - v1.4.7 GitHub 게시

- 변경 파일: `docs/development-log.md`
- 변경 사유: 최종 게시 결과와 원격 자산 검증을 기록한다.
- 실행 명령: `git push origin main`, `git push origin v1.4.7`, GitHub Release 생성·조회
- 검증 결과: 릴리스 커밋 `4b37744`와 태그 `v1.4.7`을 전송하고 정식 릴리스를 게시했다. 원격 설치 EXE는 63,416,657 bytes이며 digest `E3C62BCB317DB2529D2098B7B94A62CF2560F1328F8BD34207CAE8EF22BFAA72`가 로컬과 일치한다.
- 남은 위험: 코드 서명이 없고 기존 Codex 작업은 재시작 전까지 새 훅을 사용하지 않는다.
- 다음 조치: v1.4.7로 업그레이드하고 SMSR·Codex를 완전히 다시 시작한다.

## 2026-09-16 - 대시보드 가독성과 중첩 워크플로 규칙 보강

- 변경 파일: `DashboardPanels.cs`, `DashboardStyles.cs`, 추적 지침·도구 설명, 자체검사, 그래프 안내 문서
- 변경 사유: 작업 상세의 기술 ID·다음 작업·분리된 메타 카드가 핵심 내용을 가렸고, 계획이 작업 중 세부 흐름으로 확장될 때 드릴다운 계층을 생성하는 기준이 추적 지침에 없었다.
- 실행 명령: Release 빌드, tracking·config·전체 자체검사, `git diff --check`
- 검증 결과: 작업 상세는 제목·상태, 담당·역할, 현재 결과 또는 작업, 접힌 완료 기준, 갱신 시각 순으로 축소했다. 미완료 노드가 독립 추적 가능한 하위 흐름으로 확장될 때 `parentNodeId`를 사용하도록 Codex 훅·MCP 안내를 통일했고 기존 클릭 드릴다운 계약을 자체검사로 확인했다. Release 빌드 경고 0·오류 0, 자체검사 종료 코드 0이다.
- 남은 위험: 자동 중첩 판단은 작업 계획 에이전트의 단계 분해에 의존하며, 완료된 노드에는 데이터 보존을 위해 새 하위 노드를 붙일 수 없다.
- 다음 조치: 사용자 승인 전에는 설치본 생성·커밋·푸시·릴리스를 진행하지 않는다.

## 2026-09-16 - v1.4.8 중첩 계획 전개와 패키징

- 변경 파일: `PlanContracts.cs`, `PlanHierarchy.cs`, `PlanTools.cs`, 계획 지침·자체검사, 대시보드 패널·스타일, 릴리스·테스트 문서, 설치본 생성물
- 변경 사유: 포괄 작업 하나에 독립 상태가 섞여 진행·중단을 알 수 없던 문제를 하위 계획과 드릴다운 그래프로 분리하고, 승인된 가독성 개선을 설치본에 포함한다.
- 실행 명령: Release 빌드·test·tracking/config/전체 자체검사, source·publish stdio 검사, `scripts/build-installer.ps1`, 버전·SHA-256·프로세스 보존 확인
- 검증 결과: `children` 입력을 3개 부모·자식 노드로 전개하고 자식 `parentNodeId`를 확인했다. Release 빌드 경고 0·오류 0, source·publish 자체검사 종료 코드 0, protocol `2025-11-25`와 도구 12개를 확인했다. 설치 파일 `1.4.8.0`은 63,437,826 bytes, SHA-256 `E30119836CA39B8928533F49662C07383AB7D905BB67E616559730A19E378B18`이며 기존 SMSR PID 4개가 유지되고 새 프로세스가 남지 않았다.
- 남은 위험: 하위 단계의 의미 있는 제목·완료 조건은 작업 에이전트가 실제 범위에서 생성하며, 설치 파일은 코드 서명되지 않았다.
- 다음 조치: v1.4.8을 커밋·태그·GitHub Release로 게시하고 원격 자산을 확인한다.

## 2026-09-16 - v1.4.8 GitHub 게시

- 변경 파일: `docs/development-log.md`
- 변경 사유: 승인된 v1.4.8 설치본의 원격 게시 결과를 기록한다.
- 실행 명령: `git push origin main`, `git push origin v1.4.8`, GitHub Release 생성·조회
- 검증 결과: 커밋 `579cb08`과 태그 `v1.4.8`을 전송하고 정식 릴리스를 게시했다. 설치 EXE 63,437,826 bytes와 SHA-256 `E30119836CA39B8928533F49662C07383AB7D905BB67E616559730A19E378B18`의 원격 digest가 로컬과 일치한다.
- 남은 위험: 설치 파일은 코드 서명되지 않았다.
- 다음 조치: v1.4.8 설치본으로 업그레이드하고 SMSR·Codex를 다시 시작한다.

## 2026-09-16 - 기능 확장 개발계획서 작성

- 변경 파일: `docs/smsr-feature-development-plan.md`, `docs/smsr-feature-development-plan.html`, `README.md`
- 변경 사유: 상태 신뢰성·정체 감지·계획 분해·요약 질의·사용자 등록 펫을 하나의 단계형 개발계획으로 정리한다.
- 실행 명령: 문서 구조·HTML 반응형 스타일·링크·`git diff --check` 확인
- 검증 결과: Markdown과 단일 HTML 문서에 범위·역할·단계 게이트·자원·리스크·QA·방법론·에이전트 분할을 모두 포함했다. 날짜 기반 일정과 벡터화는 계획에서 제외했다.
- 남은 위험: 실제 구현 우선순위와 각 단계의 완료 시점은 계획 승인 후 확정한다.
- 다음 조치: 사용자가 계획 범위와 P0 단계의 완료 기준을 승인하면 상태·정체 감지부터 구현한다.

## 2026-09-16 - 요약 범위·상태 요약·단일 작업 펫 1차 구현

- 변경 파일: AI 요약·질의 ViewModel/XAML, 대시보드 상태 요약, 펫 설정·자산 검증·오버레이·트레이, 자체검사, README와 기능 확장 계획서
- 변경 사유: 요약·질의 팝업에서 특정 프로젝트와 전체 프로젝트를 선택하고, 현재 그래프의 정체·차단·실패를 빠르게 확인하며, 사용자가 등록한 펫 한 마리로 선택 그래프 상태를 볼 수 있게 한다.
- 실행 명령: `dotnet build SMSR.slnx -c Release --no-restore`, Release 앱 `--self-test`, `git diff --check`
- 검증 결과: 특정/전체 프로젝트 범위와 오늘/선택 기간 명령을 분리했다. 대시보드에 정체 가능·확인 필요·실패 수와 작업명 기반 차단 링크를 추가했다. 10MB 이하 PNG·JPG·BMP 시그니처 검사, 단일 펫 등록, 선택 그래프 상태·진행률 표시, 트레이 표시·숨김을 구현했다. Release 빌드 경고 0·오류 0과 자체검사 종료 코드 0을 확인했다.
- 남은 위험: 펫 1차 버전은 정적 이미지에 상태별 WPF 움직임을 적용하며 스프라이트 애니메이션·크기·투명도·움직임 감소 설정은 후속 단계다. 실제 사용자 이미지와 다중 모니터 배치는 수동 확인이 필요하다.
- 다음 조치: 팝업·펫을 실제 화면에서 확인한 뒤 운영 검색·인수인계 또는 펫 접근성 설정 중 우선 항목을 진행한다.

## 2026-09-16 - 완료 그래프와 연결·진행 상태 문구 분리

- 변경 파일: `DashboardPage.cs`, `DashboardLiveUpdates.cs`, `DashboardPanels.cs`, `DashboardHistoryCards.cs`, `DashboardGraph.cs`, `PetPresentation.cs`, `MvpSelfCheck.cs`
- 변경 사유: 완료된 그래프에도 `실시간 연결됨`과 에이전트 `작업 중`이 표시돼 현재도 AI가 작업하는 것처럼 보였다.
- 실행 명령: `dotnet build SMSR.slnx -c Release --no-restore`, Release 앱 `--self-test`, `--tracking-self-test`, `git diff --check`
- 검증 결과: 모든 계획 노드가 완료된 그래프는 `그래프 완료`로 고정하고 SSE 연결 문구가 덮어쓰지 않게 했다. 활성 그래프의 SSE는 `자동 갱신 연결됨`, heartbeat의 ACTIVE는 `연결됨`, 저장된 IN_PROGRESS는 `진행 상태`, 실제 미완료 도구 실행만 `실행 중`으로 구분했다.
- 남은 위험: 오래된 클라이언트가 종료 이벤트를 보내지 않아 실제 IN_PROGRESS 상태가 남은 경우에는 `정체 가능` 수치와 상태 기록을 함께 확인해야 한다.
- 다음 조치: 실제 완료·차단·대기 그래프에서 상단 문구와 에이전트 카드를 수동 확인한다.

## 2026-09-16 - 목표·그래프 IN/OUT 토큰 표시

- 변경 파일: `ActivityContracts.cs`, `ActivityJsonlStore.cs`, `ActivityEndpoints.cs`, `ActivitySelfCheck.cs`, `DashboardPage.cs`, `DashboardStyles.cs`, `LocalServerEndpoints.cs`, `WorkflowExportService.cs`, `CodexActivityHook.cs`, `CodexTokenUsageReader.cs`, `README.md`
- 변경 사유: 현재 Codex 목표 작업의 전체 사용량과 선택 그래프가 연결된 뒤 사용한 토큰을 구분해 확인할 수 없었다.
- 실행 명령: Release 빌드, 전체·tracking 자체검사, `git diff --check`
- 검증 결과: Codex 로컬 세션의 누적 `input_tokens`·`output_tokens`를 읽고 세션별 최신값을 합산한다. 목표 작업은 전체 누적값, 그래프는 연결 시점 기준값을 뺀 값을 표시하며 하위 에이전트 사용량도 같은 목표·그래프에 합산한다. 사용량이 없으면 추정하지 않고 `수집 대기`로 표시한다.
- 남은 위험: Codex 로컬 세션 JSONL 형식은 공개 훅 계약이 아니므로 향후 형식이 바뀌면 `수집 대기`로 안전하게 폴백한다. 기존 기록은 토큰 스냅샷이 없어 소급 계산하지 않는다.
- 다음 조치: 실제 설치 환경의 새 그래프에서 턴 종료 후 목표·그래프 IN/OUT 갱신을 확인한다.

## 2026-09-16 - v1.5.0 문서·설치본 확정

- 변경 파일: `README.md`, 기능 확장 계획서 Markdown·HTML, `SMSR.App.csproj`, 설치 안내, v1.5.0 릴리스 노트·통합 테스트 보고서, 설치본 생성물
- 변경 사유: 요약 범위, 상태 의미, 단일 펫, 목표·그래프 토큰 기능을 하나의 신규 릴리스로 문서화하고 후속 작업과 배포 산출물을 확정한다.
- 실행 명령: Release 빌드·test, 소스·publish config/tracking/OAuth/전체 자체검사, `scripts/build-installer.ps1`, publish stdio 검사, 버전·SHA-256·프로세스 확인
- 검증 결과: 소스·publish 자체검사 8개와 MCP protocol `2025-11-25`·도구 12개를 확인했다. 설치 파일 `1.5.0.0`은 63,448,762 bytes, SHA-256 `3CD18ACBAE0679BC8EEFC2D8B0322D9D74AC5B96CB425E48047952D5D02CB067`이며 검사 전후 기존·신규 SMSR 프로세스가 없었다.
- 남은 위험: 토큰 입력은 Codex 로컬 세션 형식에 의존하고 기존 그래프는 소급 계산하지 않는다. 한국어 IME와 다중 모니터 펫 배치는 실제 설치 환경 수동 검사가 남았다. 설치 파일은 코드 서명되지 않았다.
- 다음 조치: 변경을 커밋하고 v1.5.0 태그·GitHub Release와 설치본·체크섬을 게시한 뒤 원격 digest를 확인한다.

## 2026-09-16 - v1.5.0 GitHub 게시

- 변경 파일: `docs/development-log.md`
- 변경 사유: v1.5.0 커밋·태그·정식 GitHub Release와 원격 설치 자산 검증 결과를 기록한다.
- 실행 명령: `git push origin main`, `git push origin v1.5.0`, GitHub Release 생성·조회
- 검증 결과: 기능 커밋 `47040418e4153370da5b65e3ba438f2353296f2e`과 태그 `v1.5.0`을 전송하고 정식 릴리스를 게시했다. 원격 설치 EXE는 63,448,762 bytes이며 digest `sha256:3cd18acbae0679bc8eefc2d8b0322d9d74ac5b96cb425e48047952d5d02cb067`이 로컬과 일치한다.
- 남은 위험: 설치 파일은 코드 서명되지 않았고, 토큰 표시는 Codex 로컬 세션 형식 변경 시 `수집 대기`로 폴백한다.
- 다음 조치: v1.5.0으로 업그레이드한 뒤 한국어 IME, 다중 모니터 펫 배치, 신규 그래프 토큰 갱신을 수동 확인한다.

## 2026-09-16 - 진행률 구간 펫과 노드 작업 요청

- 변경 파일: 펫 설정·자산 저장·미디어 선택·오버레이, 대시보드 노드 상세, 자체검사, `README.md`
- 변경 사유: 선택 그래프의 진행률 구간마다 사용자가 등록한 이미지·애니메이션·영상을 바꾸고, 지연되거나 진행 중인 노드의 후속 지시를 직접 타이핑하지 않게 한다.
- 실행 명령: `dotnet build SMSR.slnx -c Release`, Release 앱 `--tracking-self-test`, `--self-test`, `git diff --check`
- 검증 결과: 0~100% 연속 구간 검증, PNG·JPG·APNG·GIF·MP4 시그니처 검사, 정적·프레임 이미지와 반복 MP4 표시, 투명 펫 창을 구현했다. Release 빌드는 경고 0·오류 0이며 전체 자체검사는 종료 코드 0이다.
- 남은 위험: APNG 애니메이션 디코딩은 Windows WIC 지원 범위에 따르며, MP4 알파 투명도는 Windows 기본 재생기에서 지원되지 않는다.
- 다음 조치: 실제 APNG·GIF·MP4와 고해상도 DPI 환경에서 펫 전환·반복 재생을 수동 확인한다.

## 2026-09-16 - 작업 중인 노드 MCP 지시 전달

- 변경 파일: 대시보드 노드 상세·실시간 스크립트, 로컬 지시 API·대기열, `record_event`·`record_heartbeat`, MCP 지침·자체검사·문서
- 변경 사유: 사용자가 작업 중인 노드에만 이어 진행·가속·재설계를 요청하고 별도 Codex 입력·전송 없이 현재 작업이 받게 한다.
- 실행 명령: Release 빌드, 전체·tracking 자체검사, `git diff --check`
- 검증 결과: 활성 heartbeat와 진행 상태를 서버에서 다시 확인한 뒤 허용된 세 지시만 한 건 대기시키고, 다음 상태 기록 또는 heartbeat 응답의 `operatorInstruction`으로 한 번 전달한다. 종료·정체·차단 노드에는 버튼이 나타나지 않는다.
- 남은 위험: 실행 중인 긴 외부 명령 자체를 중간에 끊지는 않으며, 해당 Codex가 다음 MCP 상태 기록 또는 heartbeat를 호출할 때 지시를 받는다.
- 다음 조치: 실제 진행 노드에서 세 버튼의 전달 지연과 지시 반영 결과를 확인한다.

## 2026-09-16 - v1.6.0 로컬 업그레이드 검증

- 변경 파일: `SMSR.App.csproj`, `docs/installer-quickstart.md`, `docs/development-log.md`, v1.6.0 설치본 생성물
- 변경 사유: 진행률 구간 펫과 작업 중 노드 지시 기능을 실제 설치 환경에서 판독할 수 있도록 로컬 테스트 설치본을 만들고 기존 설치를 업그레이드한다.
- 실행 명령: `scripts/build-installer.ps1`, v1.6.0 무인 설치, 설치본 `--tracking-self-test`, `--self-test`, HTTP health 확인
- 검증 결과: 설치 파일 `SMSR-Setup-1.6.0.0-win-x64.exe`는 63,453,442 bytes, SHA-256 `0DE55D35FD7746F1EBB2B5E270C2FC0AB1445AAE3C80127BFF18B8342AB70BA3`이다. 설치 종료 코드와 두 자체검사는 모두 0이며, 설치된 실행 파일 버전은 1.6.0.0, `/api/health`는 `ready`를 반환했다. 설정과 SQLite DB의 설치 전후 SHA-256도 동일하다.
- 남은 위험: 설치 파일은 코드 서명되지 않았고, APNG·GIF·MP4 전환과 작업 중 노드 버튼은 실제 UI에서 수동 확인이 필요하다.
- 다음 조치: 열린 v1.6.0 화면에서 펫 미디어 구간과 활성 노드 지시 전달을 확인한다.

## 2026-09-16 - 날짜별 AI 범위와 펫 구간 UI 개선

- 변경 파일: AI 요약 범위 ViewModel, `WorkflowPanel`, 펫 설정 ViewModel·XAML·구간 선택기, 자체검사, `README.md`
- 변경 사유: AI 요약·질의 프로젝트 목록에 선택 기간 밖 프로젝트가 보였고, 펫 구간 숫자를 직접 입력하는 UI는 겹침을 만들 수 있었다.
- 실행 명령: Release 빌드, 전체·tracking 자체검사, `git diff --check`
- 검증 결과: 팝업을 열 때 선택 기간의 그래프와 일일 기록에 존재하는 프로젝트만 조회한다. `Esc`로 팝업을 닫는다. 펫 미디어 추가·삭제 시 0~100%를 개수에 따라 자동 균등 분할하고, 직접 숫자 입력과 별도 저장 버튼을 제거한 동일 폭 영역 UI로 바꿨다. 2개와 3개 미디어의 비중첩 분배를 자체검사한다.
- 남은 위험: 매우 많은 미디어를 등록하면 각 영역의 파일명이 좁아지므로 실제 사용량을 보고 가로 스크롤 또는 축약 표시를 보강할 수 있다.
- 다음 조치: 실제 팝업 날짜 변경과 펫 미디어 1~4개 추가·삭제 화면을 수동 확인한다.

## 2026-09-16 - v1.6.1 로컬 업데이트

- 변경 파일: `SMSR.App.csproj`, `docs/installer-quickstart.md`, `docs/development-log.md`, v1.6.1 설치본 생성물
- 변경 사유: 날짜별 AI 프로젝트 범위, 펫 자동 구간 UI와 `Esc` 닫기 수정본을 현재 PC에서 확인할 수 있게 설치한다.
- 실행 명령: `scripts/build-installer.ps1`, v1.6.1 무인 설치, 설치본 전체·tracking 자체검사, HTTP health 확인
- 검증 결과: 설치 파일 `SMSR-Setup-1.6.1.0-win-x64.exe`는 63,458,760 bytes, SHA-256 `F22A72DBDFAA172E6C22E9E9BBC79AAE513DF8E910DEEB552D9AAA33A2C600FC`이다. 설치 종료 코드와 자체검사는 모두 0이고 설치 버전은 1.6.1.0, `/api/health`는 `ready`다. 설정과 SQLite DB 해시는 설치 전후 동일하며 SMSR.App은 한 개만 실행 중이다.
- 남은 위험: 설치 파일은 코드 서명되지 않았으며 실제 미디어 조합과 팝업 조작은 화면 수동 확인이 필요하다.
- 다음 조치: 열린 v1.6.1에서 선택 날짜별 프로젝트 목록, `Esc`, 펫 미디어 추가·삭제 자동 분배를 확인한다.

## 2026-09-16 - 요약 범위 표시명 수정과 v1.6.2 업데이트

- 변경 파일: AI 요약 범위 항목, 자체검사, `SMSR.App.csproj`, 설치 안내·개발 로그, v1.6.2 설치본 생성물
- 변경 사유: WPF 콤보박스 선택 영역이 프로젝트명 대신 `SummaryProjectScopeOption` 레코드 문자열을 표시했다.
- 실행 명령: Release 빌드, 전체·tracking 자체검사, `scripts/build-installer.ps1`, v1.6.2 무인 설치, HTTP health 확인
- 검증 결과: 범위 항목 `ToString()`이 항상 `Label`을 반환하고 회귀검사로 고정됐다. 설치 파일은 63,460,348 bytes, SHA-256 `F2602737AD203662C9E1F5D772BFB3CDF5BF73F57AF32BDB9B9224C79B5906D7`이다. 첫 설치는 실행 중인 `SMSR.Bridge.exe` 파일 잠금으로 종료 코드 5였고 해당 설치 경로의 프로세스임을 확인해 종료한 뒤 재설치해 종료 코드 0을 확인했다. 설치 버전 1.6.2.0, 자체검사 2종 종료 코드 0, `/api/health` `ready`, 설정·DB 해시 보존을 확인했다.
- 남은 위험: 설치 파일은 코드 서명되지 않았다.
- 다음 조치: 열린 AI 요약·질의 팝업에서 선택값과 펼친 목록 모두 프로젝트명만 표시되는지 확인한다.

## 2026-09-16 - v1.6.2 GitHub 게시

- 변경 파일: `docs/development-log.md`
- 변경 사유: 펫 진행률 미디어, 활성 노드 지시, 날짜별 AI 범위와 표시명 수정의 커밋·태그·정식 릴리스 결과를 기록한다.
- 실행 명령: `git push origin main`, `git push origin v1.6.2`, GitHub Release 생성·조회
- 검증 결과: 기능 커밋 `ba26c0a0d733bda34497bf4f49541d6379df96d3`과 태그 `v1.6.2`를 전송하고 정식 릴리스를 게시했다. 원격 설치 EXE는 63,460,348 bytes이며 digest `sha256:f2602737ad203662c9e1f5d772bfb3cdf5bf73f57af32bdb9b9224c79b5906d7`이 로컬과 일치한다.
- 남은 위험: 설치 파일은 코드 서명되지 않았다.
- 다음 조치: 실제 사용 중 발견되는 표시·미디어 재생 문제를 다음 패치 릴리스에서 보완한다.

## 2026-09-16 - 펫 구간·크기 조정과 완료 후 대기 상태

- 변경 파일: 펫 설정 ViewModel·XAML, 미디어 구간 선택기, 펫 컨트롤러·오버레이, 설정 저장, 자체검사, `README.md`
- 변경 사유: 자동 분할된 각 미디어 진행률을 사용자가 안전하게 조정하고, 펫 크기와 완료 후 대기 상태를 제어한다.
- 실행 명령: `dotnet build SMSR.slnx -c Release --no-restore --verbosity:minimal`, Release 앱 `--self-test`, `--tracking-self-test`, `git diff --check`
- 검증 결과: 하나의 진행률 막대에서 경계선을 드래그해도 인접 구간이 0~100%를 빈틈 없이 유지된다. 60~180% 크기는 미디어에만 적용되고, 완료 시 우클릭 `완료 확인` 후 별도 대기 미디어와 IDLE 움직임이 작업 재개 전까지 유지된다. 하단은 진행률 숫자만 표시한다. Release 빌드 경고 0·오류 0, 자체검사 2종 종료 코드 0이다.
- 남은 위험: 실제 고DPI·다중 모니터에서 미디어 크기와 우클릭 메뉴 배치는 화면 수동 확인이 필요하다.
- 다음 조치: 실제 펫 미디어 2~4개의 경계선 드래그, 미디어 크기 변경, 우클릭 완료 확인·작업 재개 전환을 수동 확인한다.

## 2026-09-16 - v1.6.3 로컬 업데이트

- 변경 파일: `SMSR.App.csproj`, `docs/installer-quickstart.md`, `docs/development-log.md`, v1.6.3 설치본 생성물
- 변경 사유: 펫 구간·크기·완료 후 대기 상태 수정본을 현재 PC에서 직접 확인할 수 있게 설치한다.
- 실행 명령: `scripts/build-installer.ps1`, v1.6.3 무인 설치, 설치본 `--self-test`, `--tracking-self-test`, HTTP health 확인
- 검증 결과: 설치 파일 `SMSR-Setup-1.6.3.0-win-x64.exe`는 63,459,327 bytes, SHA-256 `4345F958EDB373396A0CCEC61261D3A0AAD685F3F401CE6698DA440AD65B5D35`이다. 첫 설치는 실행 중 SMSR 파일 잠금으로 종료 코드 5였고, 해당 SMSR 프로세스 1개만 종료한 뒤 재설치해 종료 코드 0을 확인했다. 설정·DB·Gemini 키 파일 3개의 해시가 유지됐고, 설치 버전 1.6.3.0, 자체검사 2종 종료 코드 0, `/api/health` `ready`, SMSR.App 프로세스 1개를 확인했다.
- 남은 위험: 설치 파일은 코드 서명되지 않았고 펫 표시는 사용자 화면 수동 확인이 남았다.
- 다음 조치: 열린 v1.6.3 설정에서 진행률 경계, 펫 크기, `완료 확인` 후 IDLE 전환을 확인한다.

## 2026-09-16 - 펫 UI 보정과 릴리스 버전 유지 재설치

- 변경 파일: 펫 설정·오버레이·컨트롤러, 설정 저장·자체검사, `README.md`, `SMSR.App.csproj`, 설치 안내·개발 로그
- 변경 사유: 크기 설정이 전체 창을 줄여 미디어·설명·완료 버튼이 잘렸고, 경계별 개별 슬라이더와 불명확한 IDLE 표시를 보정한다. 정식 릴리스 전까지 설치 버전은 1.6.2를 유지한다.
- 실행 명령: Release 빌드, 전체·tracking 자체검사, `scripts/build-installer.ps1`, 1.6.2.0 무인 재설치, HTTP health 확인
- 검증 결과: 이미지·영상만 크기가 바뀌고 진행 중에는 하단에 진행률만 남으며, 완료 확인 후 IDLE에서는 하단 영역도 숨겨진다. 100%에서만 우클릭 완료 메뉴가 활성화되며 대기 미디어 설정이 저장된다. 설치 파일은 63,461,311 bytes, SHA-256 `091DD56EBDE7256DFA2C61EB68C8B52CCBAFD27ACE9064A0190BBAE7AB22A275`이다. 설정·DB·Gemini 키 해시를 보존했고, 설치 버전 1.6.2.0, 자체검사 2종 종료 코드 0, `/api/health` `ready`, SMSR.App 1개를 확인했다.
- 남은 위험: 경계선 드래그·대기 미디어 전환은 실제 사용자 화면 수동 확인이 남았다.
- 다음 조치: 열린 앱에서 경계선, 크기, 우클릭 완료 확인, IDLE 미디어를 확인한다.

## 2026-09-16 - v1.6.3 펫 제어 릴리스 확정

- 변경 파일: 펫 설정·미디어 선택·오버레이·앱 복원 연결, 설정 저장·자체검사, README·설치 안내·v1.6.3 릴리스 노트
- 변경 사유: 사용자가 펫 미디어 구간·크기·완료 후 대기 상태를 제어하고 펫 더블클릭으로 SMSR 창을 바로 복원한다.
- 실행 명령: Release 빌드·`dotnet test`, 소스·publish·설치본 자체검사, MCP stdio 검사, `scripts/build-installer.ps1`, v1.6.3 무인 설치, HTTP health·프로세스·데이터 해시 확인
- 검증 결과: 빌드 경고 0·오류 0, publish·설치본 자체검사 4종 종료 코드 0, MCP protocol `2025-11-25`·도구 12개를 확인했다. 설치 파일은 63,464,687 bytes, SHA-256 `6B056F561D74AE5A58DB396042DDA853E7965E2AE5B5BEEF7B1BD34F05A1AA92`이다. 설정·DB·Gemini 키 파일을 보존했고, 설치 버전 1.6.3.0, `/api/health` `ready`, SMSR.App 1개를 확인했다.
- 남은 위험: 설치 파일은 코드 서명되지 않았고 펫 더블클릭·경계선 드래그는 실제 화면 수동 확인이 남았다.
- 다음 조치: 기능 커밋·태그를 원격에 푸시하고 v1.6.3 GitHub Release에 설치 파일·체크섬을 게시한다.

## 2026-09-16 - v1.6.3 GitHub 게시

- 변경 파일: `docs/development-log.md`
- 변경 사유: 펫 구간·크기·IDLE·SMSR 창 복원 기능의 커밋·태그·정식 릴리스 게시 결과를 기록한다.
- 실행 명령: `git push origin main`, `git push origin v1.6.3`, `gh release create`, GitHub Release 자산·digest 조회
- 검증 결과: 기능 커밋 `011f926ae6cf7851de32371f20e934c308e602cd`과 `v1.6.3` 태그를 전송하고 정식 릴리스를 게시했다. 원격 설치 EXE는 63,464,687 bytes이며 digest `sha256:6b056f561d74ae5a58db396042dda853e7965E2AE5B5BEEF7B1BD34F05A1AA92`로 로컬 SHA-256과 일치한다.
- 남은 위험: 설치 파일은 코드 서명되지 않았다.
- 다음 조치: 실제 펫 미디어 조합에서 경계선 드래그, IDLE 전환, 더블클릭 창 복원을 수동 확인한다.

## 2026-09-16 - 선택 없음 펫 대기 상태 보정

- 변경 파일: `PetPresentation.cs`, `PetController.cs`, `MvpSelfCheck.cs`, `docs/development-log.md`
- 변경 사유: 프로젝트 또는 워크플로우 선택을 해제한 직후 이전 완료 그래프가 컬렉션에 남아 펫 진행률이 100%로 표시되거나, 이를 0%로 바꿔도 불필요한 진행률 표시가 남았다.
- 실행 명령: Release 빌드, 소스·설치본 자체검사 4종, `scripts/build-installer.ps1`, v1.6.3 무인 재설치, HTTP health 확인
- 검증 결과: 유효한 선택이 없으면 남아 있는 노드 상태와 관계없이 기존 `IDLE` 상태로 전환해 진행률을 숨기고 대기 미디어를 사용하며, 프로젝트 선택 변경도 즉시 갱신한다. 회귀검사를 추가했고 빌드 경고 0·오류 0, 설치본 자체검사 2종 종료 코드 0이다. 설치 파일은 63,445,123 bytes, SHA-256 `2B9BEBEDAC4C9C6942CCC1F9DA3CCAD22F31398A797F443F89A10A55D69581AF`이다. 설정·DB·Gemini 키 파일을 보존했고 설치 버전 1.6.3.0, `/api/health` `ready`, SMSR.App 1개를 확인했다.
- 남은 위험: 선택 해제·재선택에 따른 화면 전환은 실제 사용자 화면 수동 확인이 남았다.
- 다음 조치: 열린 앱에서 프로젝트와 워크플로우 선택을 해제했을 때 진행률이 숨겨지고 대기 미디어로 전환되는지 확인한다.

## 2026-09-16 - v1.6.4 펫 대기·창 종속 보정

- 변경 파일: 펫 컨트롤러·표시 상태·자체검사, `SMSR.App.csproj`, `README.md`, 설치 안내·릴리스 노트·개발 로그
- 변경 사유: 선택된 프로젝트·워크플로우가 없을 때 진행률 숫자가 남았고, 펫이 SMSR과 별도의 최상위 창으로 생성돼 `Alt+Tab` 전환 항목으로 취급될 수 있었다.
- 실행 명령: Release 빌드·`dotnet test`, 소스·설치본 자체검사, `scripts/build-installer.ps1`, v1.6.4 무인 설치, HTTP health·프로세스·데이터 해시 확인
- 검증 결과: 선택 없음은 기존 `IDLE` 상태와 대기 미디어를 사용해 진행률을 숨긴다. 펫 창은 `ShowInTaskbar=False`를 유지하면서 SMSR 메인 창을 `Owner`로 지정해 독립 전환 창에서 제외된다. 빌드 경고 0·오류 0, 설치본 자체검사 4종 종료 코드 0이다. 설치 파일은 63,463,906 bytes, SHA-256 `C96850FA2AF23315499D43A7022CDECB8B641C2E8BC06F0BDF2EF43CAB06A38A`이다. 설정·DB·Gemini 키 파일을 보존했고 설치 버전 1.6.4.0, `/api/health` `ready`, SMSR.App 1개를 확인했다.
- 남은 위험: 현재 컴퓨터 제어 표면에서 네이티브 앱 목록을 제공하지 않아 실제 `Alt+Tab` 화면 캡처는 수행하지 못했으며 WPF 소유 창 동작으로 검증했다. 설치 파일은 코드 서명되지 않았다.
- 다음 조치: 기능 커밋·태그를 원격에 푸시하고 v1.6.4 GitHub Release에 설치 파일·체크섬을 게시한다.

## 2026-09-16 - v1.6.4 GitHub 게시

- 변경 파일: `docs/development-log.md`
- 변경 사유: 선택 없음 펫 IDLE 처리와 SMSR 종속 창 보정의 커밋·태그·정식 릴리스 결과를 기록한다.
- 실행 명령: `git push origin main`, `git push origin v1.6.4`, `gh release create`, GitHub Release 자산·digest 조회
- 검증 결과: 기능 커밋 `7403cbb30e1f99d9e895c14ef89c9927d6344b6d`과 `v1.6.4` 태그를 전송하고 정식 릴리스를 게시했다. 원격 설치 EXE는 63,463,906 bytes이며 digest `sha256:c96850fa2af23315499d43a7022cdecb8b641c2e8bc06f0bdf2ef43cab06a38a`로 로컬 SHA-256과 일치한다.
- 남은 위험: 설치 파일은 코드 서명되지 않았다.
- 다음 조치: 실제 `Alt+Tab` 전환 화면과 선택 없음 IDLE 표시를 사용자 화면에서 확인한다.

## 2026-09-16 - v1.6.4 펫 최소화 회귀 수정

- 변경 파일: `PetController.cs`, `PetWindow.xaml.cs`, `README.md`, v1.6.4 릴리스 노트·개발 로그
- 변경 사유: 펫을 SMSR 메인 창의 소유 창으로 지정한 방식은 `Alt+Tab`에서는 제외되지만 메인 창 최소화 시 펫도 함께 숨겼다.
- 실행 명령: Release 빌드, 자체검사 4종, `scripts/build-installer.ps1`, v1.6.4 무인 재설치, Win32 창 스타일·최소화 동작 검사, HTTP health 확인
- 검증 결과: 메인 창 소유 관계를 제거하고 펫 자체에 `WS_EX_TOOLWINDOW`를 적용했다. 실행 중 펫 창은 `ToolWindow=True`이며 메인 창을 실제 최소화한 동안에도 `Visible=True`를 유지했다. 설치 파일은 63,472,110 bytes, SHA-256 `FB365D0F1DC0AF515F748E0D967CFEC014CFBA3D41738C6AE8073FD5CCF50EB2`이다. 설치본 자체검사 4종 종료 코드 0, 설정·DB·Gemini 키 파일 보존, 설치 버전 1.6.4.0을 확인했다.
- 남은 위험: 설치 파일은 코드 서명되지 않았다.
- 다음 조치: 수정 커밋으로 v1.6.4 태그를 갱신하고 기존 GitHub Release 설치 파일·체크섬을 교체한다.

## 2026-09-16 - v1.6.4 수정본 교체 게시

- 변경 파일: `docs/development-log.md`
- 변경 사유: 새 버전을 만들지 않고 사용자가 지정한 기존 v1.6.4 릴리스에 펫 최소화 회귀 수정본을 반영한다.
- 실행 명령: `git push origin main`, v1.6.4 태그 강제 갱신, `gh release upload --clobber`, `gh release edit`, 원격 자산 digest 조회
- 검증 결과: v1.6.4 태그를 수정 커밋 `17a146c76408338149683002caa76a1363b58980`으로 갱신했다. 기존 릴리스의 설치 EXE와 체크섬을 교체했으며 원격 EXE는 63,472,110 bytes, digest `sha256:fb365d0f1dc0af515f748e0d967cfec014cfba3d41738c6ae8073fd5ccf50eb2`로 로컬과 일치한다.
- 남은 위험: 이미 이전 v1.6.4 설치 파일을 받은 사용자는 같은 버전 번호의 수정 설치본을 다시 받아야 한다.
- 다음 조치: 없음.

## 2026-09-28 - 그래프 작업 이력과 하위 상태 표시 보정

- 변경 파일: `WorkflowContext` 저장소·MCP 도구·대시보드·내보내기·자동 추적 지침·자체검사
- 변경 사유: 드릴다운 그래프가 임시 부모 연결의 `PENDING` 상태를 자식에 적용해 완료 자식을 0%로 보였고, 작업 배경·진행 방식·최종 결과를 그래프와 함께 보존할 수 없었다.
- 실행 명령: `dotnet build SMSR.slnx -c Release`, tracking·전체 self-check, `scripts/test-mcp-stdio.ps1 -ExpectedToolCount 14`
- 검증 결과: 자식은 원래 노드 상태로 계산해 `SUCCESS`·100%를 유지한다. `reason`·`approach`·`result`는 SQLite에 저장되고 MCP·stdio·HTTP API·대시보드·내보내기에서 조회된다. Release 빌드 경고 0·오류 0, tracking 및 전체 self-check와 stdio 도구 14개 검사가 통과했다.
- 남은 위험: 기존 그래프에는 새 작업 이력 값이 없으므로 대시보드가 누락 표기로 표시한다.
- 다음 조치: 새 그래프를 만들 때 `save_plan`의 `reason`·`approach`를 채우고 완료 전 `save_workflow_context`의 `result`를 기록한다.

## 2026-09-17 - Codex 토큰 수집 대기 고정 수정

- 변경 파일: `CodexTokenUsageReader.cs`, `TokenUsageRecorder.cs`, MCP 이벤트·heartbeat 처리, 로컬 서버 등록, 활동 자체검사, `README.md`, 개발 로그
- 변경 사유: 최신 Codex 세션은 `token_usage_record.thread_token_usage`를 기록하지만 기존 파서는 이전 `token_count`만 읽었다. 또한 현재 도구 중첩 경로에서는 command 훅이 MCP 내부 호출을 보지 못해 활동 파일 자체가 생성되지 않았다.
- 실행 명령: 실제 Codex JSONL 필드 구조 검사, Release 빌드, 전체·tracking 자체검사, `scripts/build-installer.ps1`, v1.6.4 무인 재설치, 설치본 자체검사 4종, 실제 MCP 이벤트·대시보드 HTML 검사
- 검증 결과: 최신·이전 토큰 이벤트를 모두 읽고 잘못 일치한 JSONL 항목은 건너뛴다. `record_event`와 heartbeat가 `agentId`에 해당하는 Codex 세션 누적값과 그래프 시작 기준 차이를 활동 저장소에 직접 기록한다. 직접 스냅샷 회귀검사에서 목표 IN/OUT 1600/400, 그래프 IN/OUT 200/50을 확인했다. 설치본에서 목표 IN 166.5M/OUT 620.0K, 그래프 IN 603.4K/OUT 2.5K가 표시되고 두 `수집 대기` 문구가 사라졌다. 빌드 경고 0·오류 0, 설치본 자체검사 4종 종료 코드 0이다. 설치 파일은 63,465,881 bytes, SHA-256 `6996146CC98384CF3134EEBD90757C482ACEA643CC5F78A900EB67504BED84B2`이며 설정·DB·Gemini 키 파일을 보존했다.
- 남은 위험: `agentId`가 Codex 세션 ID가 아닌 `root` 같은 별칭인 기존 그래프는 새 이벤트부터도 세션 JSONL을 찾을 수 없어 계속 수집 대기로 남을 수 있다.
- 다음 조치: 기존 `root` 별칭 그래프는 새 Codex 세션 ID 이벤트가 들어오는 시점부터 수집한다.

## 2026-09-17 - 목표 작업 토큰 누적값 보정

- 변경 파일: 토큰 추적 세션 계약, 직접 MCP 수집기, Codex 활동 훅, 활동 자체검사, `README.md`, 개발 로그
- 변경 사유: 목표 작업 토큰에 목표 시작 이후 사용량이 아니라 긴 Codex 세션의 절대 누적값이 저장돼 수억 IN 토큰으로 과대 표시됐다.
- 실행 명령: Release 빌드, 전체·tracking 자체검사, `scripts/build-installer.ps1`, v1.6.4 무인 재설치, 설치본 자체검사 4종, 실제 MCP heartbeat·대시보드 HTML 검사
- 검증 결과: 목표 시작 시 IN/OUT 기준값을 저장하고 이후 누적값과의 차이만 기록한다. 최신·구형 토큰 레코드가 함께 있으면 최신 `token_usage_record`를 우선해 지연된 구형 값으로 숫자가 역행하지 않는다. 직접 수집 회귀검사에서 세션 누적 IN/OUT 1600/400 대신 목표·그래프 증분 200/50을 확인했다. 설치 환경에서는 목표 IN/OUT 2.9M/9.2K, 그래프 IN/OUT 10.9M/62.2K로 증분 표시됐고 최종 검증 중 전체 진행률은 88%였다. Release 빌드는 경고 0·오류 0, 설치본 자체검사 4종 종료 코드 0이다. 설치 파일은 63,444,427 bytes, SHA-256 `D21D7DD439024B612F2978C2EAB76D382172B033D1FAACD97C9FCFFD015F41FC`이며 설정·DB·Gemini 키 파일을 보존했다.
- 남은 위험: 기준값이 없던 기존 추적 세션은 수정본 첫 이벤트를 새 기준으로 사용하므로 그 이전 목표 사용량은 소급하지 않는다.
- 다음 조치: 없음.

## 2026-09-17 - 그래프 조기 100% 방지 규칙 보강

- 변경 파일: `.agents/skills/smsr-tracking/SKILL.md`, 개발 로그
- 변경 사유: 설치·최종 검증이 남은 상태에서 마지막 검증 노드를 먼저 완료해 그래프가 일시적으로 100%로 표시됐다.
- 실행 명령: 후속 구현·설치 노드를 기존 그래프에 추가하고 상태 전이를 기록
- 검증 결과: 발견된 추가 수정과 재설치 검증을 의존 노드로 표시했으며, 실행 중인 명령이나 필수 검증이 남으면 마지막 노드를 완료하지 않도록 추적 규칙을 명시했다.
- 남은 위험: 그래프는 에이전트가 보고한 계획과 상태를 표시하므로 에이전트가 후속 작업을 기록하지 않으면 자동 추론할 수 없다.
- 다음 조치: 재설치·실화면 검증 완료 후에만 최종 노드를 종료한다.

## 2026-09-17 - 검증 노드 색상과 그래프 전용 토큰 의미 보정

- 변경 파일: 그래프 SVG·스타일·대시보드 토큰 표기, 토큰 추적 세션·수집기, 계획·이벤트·heartbeat 도구, 활동 훅·자체검사, `README.md`, 개발 로그
- 변경 사유: 테스트·검증 노드를 일반 작업과 시각적으로 구분할 수 없었고, 그래프 토큰이 SMSR 추적 오버헤드가 아니라 그래프 연결 이후 전체 작업 토큰으로 계산됐다.
- 실행 명령: Release 빌드, 전체·tracking 자체검사
- 검증 결과: `validator`·`test` 역할 노드는 노란색으로 표시하고 실패·차단·재시도는 빨간색을 우선한다. 목표 토큰은 시작부터 종료까지의 Codex 실측 증분을 유지하며, `그래프(추정)`은 `save_plan`·`record_event`·heartbeat 요청과 응답의 UTF-8 크기 기반 추정 토큰만 누적한다. 빌드 경고 0·오류 0, 자체검사 2종 종료 코드 0이다.
- 남은 위험: Codex는 도구 호출별 실제 토큰을 제공하지 않아 그래프 전용 값은 추정치다. 로컬 JSONL 기록 자체는 모델 토큰을 소비하지 않는다.

## 2026-09-17 - 토큰 분류 호버 표시

- 변경 파일: Codex 토큰 판독기, 토큰 추적 계약·수집기·저장소, 대시보드, 활동 자체검사, `README.md`, 개발 로그
- 변경 사유: 합계 IN만으로는 신규 입력과 캐시 입력의 비중을 구분하기 어려웠다.
- 실행 명령: `dotnet build src/SMSR.App/SMSR.App.csproj -c Release`, `SMSR.App.exe --self-test`
- 검증 결과: 목표 시작 기준값에서 캐시 입력 증분도 함께 계산하고, 목표 토큰 호버에 신규 입력·캐시 입력·출력을 줄 단위로 표시한다. 그래프 추정 토큰은 입력·출력을 줄 단위로 표시한다. Release 빌드 경고 0·오류 0, 자체검사 종료 코드 0이다.
- 남은 위험: 이전 기록에는 캐시 입력 스냅샷이 없어 새 스냅샷이 기록되기 전까지 세부 분류를 표시하지 않는다.
- 다음 조치: 실제 대시보드에서 브라우저 기본 툴팁의 줄바꿈과 수치를 확인한다.

## 2026-09-17 - v1.6.5 토큰 카드·대시보드·펫 확인 개선

- 변경 파일: 토큰 수집·저장·대시보드 HTML/CSS·자동 갱신, 에이전트 카드 CSS, 펫 표시 상태·더블클릭 처리, 자체검사, `README.md`, `docs/images/sample-workflow-graph.svg`, 설치 안내, v1.6.5 릴리스 노트, 개발 로그
- 변경 사유: 토큰 세부값이 브라우저 기본 툴팁으로 표시돼 가독성이 낮았고, 진행률과 완료·정체·확인·실패 숫자가 중복됐다. 완료 펫은 우클릭으로만 확인할 수 있었으며 에이전트 라벨이 좁은 카드에서 줄바꿈됐다. 대시보드 갱신 요청이 지연되면 이후 갱신도 잠긴 채 대기할 수 있었다.
- 실행 명령: Release 빌드·`dotnet test`, 자체검사 5종, `scripts/build-installer.ps1`, v1.6.5 무인 설치, 설치본 자체검사 5종, HTTP health·프로세스·보존 데이터 해시·브라우저 렌더링 확인
- 검증 결과: 목표 토큰은 신규 입력·캐시 입력·출력, 그래프 추정 토큰은 입력·출력을 테마형 카드로 표시하며 키보드 포커스도 지원한다. 상단은 연결 상태·두 토큰·전체 진행률을 한 줄로 유지한다. 에이전트 이름은 카드 안에서 말줄임하고 상태 배지를 고정한다. 완료 펫 더블클릭은 완료 확인 후 SMSR 창을 연다. 자동 갱신은 요청을 8초 뒤 중단하고 `finally`에서 잠금을 해제한다. 노드 선택 시 진행 중 갱신과 이벤트 스트림을 먼저 중단해 사용자 이동을 우선하며 `새 정보 반영 중…`·`선택 작업 여는 중…` 상태를 연결 칩에 표시한다. 빌드 경고 0·오류 0, 소스와 설치본 자체검사 5종 종료 코드 0이다. 설치 버전 1.6.5.0, 서버 `ready`, SMSR.App 1개이며 설정·Gemini 키와 기존 그래프가 유지됐다. 실제 브라우저에서 토큰 카드, 상단 한 줄, 에이전트 배지 경계와 선택 노드 URL·상세의 일치를 확인했다. 문서용 SVG 샘플 그래프는 XML 파싱과 README 상대 경로를 확인했다. 설치 파일은 63,453,035 bytes, SHA-256 `7CD9A22BCC163392D88E57E45492A5BF449B4C86F1BEF2CA58FCE1D61B5EBEF0`이다.
- 남은 위험: 설치 파일은 코드 서명되지 않았다. 펫 더블클릭의 실제 사용자 입력 체감은 설치 화면에서 최종 수용 확인이 남았다.
- 다음 조치: 기능 커밋·푸시 후 v1.6.5 태그와 GitHub Release에 설치 파일·체크섬을 게시한다.
- 다음 조치: 필요하면 다음 설치본에 포함해 실제 화면의 노란색 노드와 추정 표기를 확인한다.

## 2026-09-17 - v1.6.5 GitHub 게시

- 변경 파일: `docs/development-log.md`
- 변경 사유: 토큰 분류, 대시보드 자동 갱신 복구, 사용자 선택 우선, 펫 완료 확인과 README 샘플 그래프의 커밋·태그·정식 릴리스 결과를 기록한다.
- 실행 명령: `git push origin main`, `git push origin v1.6.5`, `gh release create`, GitHub Release 자산·digest 조회
- 검증 결과: 기능 커밋 `6924b46`과 `v1.6.5` 태그를 전송하고 정식 릴리스를 게시했다. 원격 설치 EXE는 63,453,035 bytes이며 digest `sha256:7cd9a22bcc163392d88e57e45492a5bf449b4c86f1bef2ca58fce1d61b5ebef0`로 로컬 SHA-256과 일치한다.
- 남은 위험: 설치 파일은 코드 서명되지 않았다.
- 다음 조치: 없음.

## 2026-09-28 - 작업 이력·코드/문서 그래프 기능 흡수 계획

- 변경 파일: `docs/smsr-graph-capability-plan-2026-09-28.md`, `docs/smsr-graph-capability-plan-2026-09-28.html`, `docs/development-log.md`
- 변경 사유: Graphify·GitNexus의 유용한 기능을 SMSR 내부에 흡수하되, 작업 이력과 코드·문서 관계를 모두 SQLite 기반으로 추적하는 단계별 실행 계획이 필요했다.
- 실행 명령: 현행 저장소·계획·검증 코드 및 기존 계획서 확인, `ConvertFrom-Markdown` HTML 변환, Markdown/HTML 구조·항목 검사
- 검증 결과: 9개 필수 계획 항목, 기존 기능 재사용·신규 데이터 계약·12주 마일스톤·예산 가정·위험/QA·후속 에이전트 분담을 작성했다. HTML은 단일 문서와 6개 표로 변환했다.
- 남은 위험: 실제 인력·예산·색인 대상 규모가 미확정이며, 계획의 성능·정확도 목표는 S1 표본에서 재조정해야 한다. 문서 작성만 수행했고 제품 기능·설치본은 변경·검증하지 않았다.
- 다음 조치: 범위·인력 가정을 승인받은 뒤 S1의 대표 데이터와 수용 표본을 확정한다.

## 2026-09-28 - Codex 주도 기준 그래프 개발 일정 재산정

- 변경 파일: `docs/smsr-graph-capability-plan-2026-09-28.md`, `docs/smsr-graph-capability-plan-2026-09-28.html`, `docs/development-log.md`
- 변경 사유: 이전 2명·12주 일정과 인건비는 Codex가 구현을 주도하는 전제에 맞지 않았다.
- 실행 명령: 기존 계획 일정·인력·비용·위험 항목 확인, Markdown 수정, `ConvertFrom-Markdown` HTML 재생성, 두 문서 구조 검사
- 검증 결과: P0/P1 기준 4주·위험 버퍼 포함 4–6주로 변경하고, 주간 승인 게이트와 선택 기능의 추가 5–10영업일을 명시했다. 사람 M/M·원화 인건비 추정을 폐기하고 실제 사용량 기반 비용 산정식으로 교체했다.
- 남은 위험: 실제 Codex 사용량, 사용자 응답 시간, 구기록 이전 난이도, 설치본 결함은 시작 전 확정되지 않았다.
- 다음 조치: 1주차에 대표 데이터·사용량·사용자 승인 가능 시간을 측정해 일정과 예산을 보정한다.

## 2026-09-28 - 작업 이력 그래프 P0 구현

- 변경 파일: `EventStore` 스키마·계획 버전/타임라인/증거·이전/삭제 처리, 계획·워크플로우 MCP/stdio 도구, HTTP 엔드포인트, 대시보드·내보내기, 자체검사, `docs/graph-tracking-guide.md`, 계획서 출처, 개발 로그
- 변경 사유: 최신 계획으로 덮인 과거 구성을 확인할 수 없었고, 작업 순서·산출물 근거·구기록의 부모/자식 상태 불일치가 한 화면에 연결되지 않았다.
- 실행 명령: `dotnet build SMSR.slnx -c Release --no-restore`, `SMSR.App.exe --tracking-self-test`, `SMSR.App.exe --self-test`, `scripts/test-mcp-stdio.ps1`, `dotnet test SMSR.slnx -c Release --no-restore`, 신규 C# 파일의 `dotnet format whitespace --verify-no-changes`, JavaScript 구문 검사, `git diff --check`
- 검증 결과: 버전별 계획 스냅샷·변경 이유와 이벤트 산출물 링크를 SQLite에 저장한다. 기존 DB는 P0 이전에 SQLite 백업하고 기존 산출물은 1회 이관하며, 과거 계획은 추정하지 않고 `버전 이력 없음`으로 표시한다. 대시보드·HTTP·MCP·내보내기에서 시간순 실행과 불일치 진단을 확인한다. Release 빌드 경고/오류 0, tracking/전체 자체검사 종료 코드 0, stdio 도구 16개, JavaScript 구문 검사 통과.
- 남은 위험: 화면 타임라인은 최근 200개 이벤트·100개 증거만 보여주고 전체 이벤트는 `events.jsonl`에서 확인한다. 실제 사용자 DB·설치본 마이그레이션과 시각적 수용 검사는 아직 수행하지 않았다. Graphify 저장소는 P0의 직접 의존성이 아니다.
- 다음 조치: 사용자가 승인하면 격리된 실제 DB 사본과 설치본에서 백업·화면·복원을 확인하고, P1 코드·문서 관계 색인에 Graphify 구현을 참고한다.

## 2026-09-28 - Graphify 추가 기능 개발계획서 반영

- 변경 파일: `docs/smsr-graph-capability-plan-2026-09-28.md`, `docs/smsr-graph-capability-plan-2026-09-28.html`, `docs/development-log.md`
- 변경 사유: Graphify 실제 코드·테스트 조사 결과를 P1/P2 구현 범위에 반영하고, SMSR의 WPF 구현 기술을 모든 색인 대상의 필수 범위로 오해하지 않도록 수정했다.
- 실행 명령: 계획서·현행 소스·Graphify 고정 리비전 확인, `ConvertFrom-Markdown` HTML 재생성, Markdown/HTML 구조·내용 일치 검사, 일정·우선순위 항목 검색
- 검증 결과: 언어 공통 파일·문서 관계, 문서 인용의 모호성 처리, 파일 소유권 증분 갱신, 무결성 진단, 근거 있는 경로/영향 조회를 P1에 반영했다. 언어별 분석기(.NET/WPF 포함), 그래프 차이·구조화된 피드백·비밀 제외 MCP 관계를 P2로 분리했다. 자동 훅·원문 질문 로그·별도 서버/그래프 DB는 제외했다. 5주 기본·5–7주 버퍼 일정과 QA·위험·담당 기준을 갱신했다. HTML 골격·Markdown 변환 본문 일치·9개 필수 장·핵심 우선순위 검사 통과. 제품 코드나 설치본은 변경하지 않았다.
- 남은 위험: Graphify 코드 전용 시범 색인은 추출 가능성만 보여주며 관계 정확도를 보증하지 않는다. 대상 언어·민감 경로·성능 표본과 P2 채택 여부는 사용자 게이트에서 결정해야 한다.
- 다음 조치: P0 실제 DB/설치본 수용을 끝내고, P1 언어 공통 색인의 정답집·허용 경로를 확정한다.

## 2026-09-28 - P1 코드·문서 관계 그래프 구현

- 변경 파일: `Graph*.cs`, `EventStoreGraph*.cs`, `StdioGraphTools.cs`, `EventStore`/로컬 서버/대시보드 연결, 백업·자체검사, MCP stdio 검사, `README.md`, 그래프 안내·계획서, 개발 로그
- 변경 사유: 작업 이력의 산출물과 언어 공통 파일·문서 관계를 연결하고, 출처가 있는 경로·역방향 영향·미해소 참조를 SQLite와 화면에서 조회할 필요가 있었다.
- 실행 명령: `dotnet build SMSR.slnx -c Release --no-restore -v:q`, `dotnet test SMSR.slnx -c Release --no-restore -v:q`, `SMSR.App.exe --graph-self-test`, `SMSR.App.exe --tracking-self-test`, `SMSR.App.exe --self-test`, `scripts/test-mcp-stdio.ps1`, `git diff --check`
- 검증 결과: 빌드 경고/오류 0, 자체검사 3종 종료 코드 0, stdio MCP 도구 23개, `dotnet test` 종료 코드 0, diff 공백 오류 0. Git 파일 목록·해시 기반 증분 색인, Markdown 링크/파일 수준 인용, 삭제·미해소 참조·대량 축소 롤백, 근거 경로·역방향 영향, HTML 인코딩·민감 제목 마스킹, 작업 산출물 연결을 임시 저장소의 HTTP/MCP 자체검사로 확인했다. P0/P1 백업 연결의 풀링을 해제해 임시 DB 잠금을 없애고 기존 MCP 이력 검사를 JSON 값 기준으로 수정했다.
- 남은 위험: 실제 사용자 DB 사본의 이전·복원, 대표 저장소 규모·p95 성능, 설치본의 실제 화면·키보드·한국어 입력 검사는 아직 수행하지 않았다. 파일 경로·문서 제목은 저장되므로 민감값이 들어간 저장소는 색인 전에 제외 범위를 검토해야 한다. 언어별 심벌·호출 관계는 P2다.
- 다음 조치: 사용자 데이터 사본과 대표 저장소에서 성능·복원·화면 수용을 진행한 뒤 배포 여부를 결정한다.

## 2026-09-28 - P1 색인 최신성·모호 참조·관계 진단 보강

- 변경 파일: `GraphIndexService.cs`, `GraphLinkResolver.cs`, `GraphMarkdown.cs`, `GraphContracts.cs`, `GraphPage*.cs`, `GraphEndpoints.cs`, `GraphTools.cs`, `StdioGraphTools.cs`, `EventStoreGraph*.cs`, `McpHttpGateway.cs`, `GraphIndexSelfCheck.cs`, `scripts/test-mcp-stdio.ps1`, `docs/graph-tracking-guide.md`, 개발 로그
- 변경 사유: 이전 P1 구현은 색인 후 파일 변경 여부를 판정하지 못했고, 외부 링크를 로컬 미해소 참조로 오인하며 파일명 후보·중복·자가 참조 진단을 표시하지 않았다.
- 실행 명령: Release 솔루션 빌드, 그래프·추적·전체 자체검사, stdio MCP 도구 목록 확인, `dotnet test`, 화면 JavaScript 구문 검사, `git diff --check`
- 검증 결과: 명시적 읽기 전용 해시 비교에서 추가·변경·삭제를 반환한다. 외부 URL은 제외하고 미해소 로컬 링크에 같은 이름 후보를 최대 5개까지 표시하되 관계로 확정하지 않는다. `FILE_ONLY` 심벌 인용을 화면에서 설명하고 중복·자기 파일 참조를 출처 위치와 함께 진단한다. 기존 P1 DB의 후보 컬럼 이전 전 백업, HTML 인코딩, HTTP/MCP 경로를 자체검사에 포함했다. Release 빌드 경고/오류 0, 자체검사 3종 종료 코드 0, stdio MCP 도구 24개, `dotnet test`·JavaScript 구문·diff 공백 검사 통과.
- 남은 위험: 대표 저장소의 색인·조회 p95, 실제 사용자 DB 복원과 설치본 화면·키보드·한국어 입력 검사는 아직 수행하지 않았다. 현재 소스 확인은 정확한 전체 해시 검사이므로 큰 저장소에서 시간이 걸릴 수 있다. 다중 파일 순환 관계의 전용 진단과 근거 파일 직접 열기는 아직 구현하지 않았다. 언어별 심벌 구현 해소는 선택형 P2 범위다.
- 다음 조치: 대표 저장소와 설치본을 대상으로 성능·복원·사용성 수용 기준을 확인한다.

## 2026-09-28 - P1 순환 진단과 색인 근거 원문 미리보기

- 변경 파일: `GraphCycleDetector.cs`, `GraphQueryCycles.cs`, `EventStoreGraphCycles.cs`, `GraphSourceService.cs`, `GraphSourcePage.cs`, `GraphEndpoints.cs`, `GraphPage*.cs`, `GraphTools.cs`, `StdioGraphTools.cs`, `LocalServer.cs`, `GraphIndexSelfCheck.cs`, `McpHttpGateway.cs`, `scripts/test-mcp-stdio.ps1`, `docs/graph-tracking-guide.md`, 개발 로그
- 변경 사유: 다중 파일 순환 연결을 확인할 수 없고 관계 근거 위치에서 실제 파일을 직접 대조할 수 없었다.
- 실행 명령: Release 솔루션 빌드, `SMSR.App.exe --graph-self-test`, `SMSR.App.exe --tracking-self-test`, `SMSR.App.exe --self-test`, `scripts/test-mcp-stdio.ps1`, `dotnet test SMSR.slnx -c Release --no-restore -v:q`, `git diff --check`
- 검증 결과: 저장된 방향 그래프의 순환 연결 그룹을 한정된 범위에서 HTTP·MCP·화면으로 조회한다. 색인 파일만 현재 해시·경로·크기·바이너리·연결 파일 검사 후 기록된 줄 주변을 HTML 인코딩한 읽기 전용 화면에 표시한다. 단방향·양방향 관계, 변경 파일·비색인 파일 거부, HTML 삽입 방지, HTTP/MCP 연결을 자체검사에 포함했다. Release 빌드 경고/오류 0, 자체검사 3종 종료 코드 0, stdio 도구 25개, `dotnet test`·JavaScript 구문·diff 공백 검사 통과.
- 남은 위험: 대표 저장소에서 순환 조회 p95와 실제 사용자 DB 복원·설치본 화면/키보드/한국어 입력은 아직 수용 검증하지 않았다. 순환 그룹은 개별 경로가 아닌 강한 연결 요소이며 대형 색인은 상한 초과로 판정을 생략한다.
- 다음 조치: 대표 저장소와 설치본에서 규모·사용성·복원 수용을 진행한다.

## 2026-09-28 - P2 관계 분석·안전한 고급 기능 확장

- 변경 파일: `Graph*.cs`, `EventStoreGraph*.cs`, `EventStoreDeletion.cs`, `GraphTools.cs`, `StdioGraphTools.cs`, `GraphEndpoints.cs`, `App.xaml.cs`, `GitAutoIndex*.cs`, `SettingsViewModel.GitHooks.cs`, 설정 화면, 자체검사, `scripts/test-mcp-stdio.ps1`, 그래프 안내·계획서 Markdown/HTML, 개발 로그
- 변경 사유: 리비전별 차이·관계 피드백·정적 API/MCP 선언·검증 기록 공백·선택 범위 내보내기·대형 그래프 요약·명시적 저장소 간 참조와 훅 설치 기능이 없었다. 전용 그래프 DB는 실측 근거가 필요했다.
- 실행 명령: `dotnet build SMSR.slnx -c Release --no-restore -v:q`, `dotnet test SMSR.slnx -c Release --no-restore -v:q`, `SMSR.App.exe --graph-self-test`, `SMSR.App.exe --tracking-self-test`, `SMSR.App.exe --self-test`, `SMSR.App.exe --codex-config-self-test`, `SMSR.App.exe --graph-benchmark`, `scripts/test-mcp-stdio.ps1 -ApplicationPath src/SMSR.App/bin/Release/net8.0-windows/SMSR.App.exe`, `git diff --check`
- 검증 결과: Release 빌드 경고/오류 0, 그래프·추적·전체·Codex 설정 자체검사 종료 코드 0, stdio MCP 도구 33개, `dotnet test` 종료 코드 0. 저장소 간 참조는 Markdown 링크와 `.csproj`/`.slnx`의 명시적 파일 경로가 다른 색인 저장소 파일과 정확히 일치할 때만 저장하고, 파일/프로젝트 삭제 후 정리를 확인했다. Git 전역 훅은 임시 설정에서 설치·중복 방지·경로 갱신·해제·기존 설정 보호를 검증했으며 실제 사용자 Git 설정은 변경하지 않았다. 합성 5천 파일·5만 노드·20만 간선의 SQLite 경로/영향 40회 p95는 73.6/82.5ms로 초기 2초 목표를 충족했다.
- 남은 위험: 기존 리비전의 과거 그래프는 소급 복원할 수 없고 전체 스냅샷은 리비전 수만큼 DB를 키울 수 있다. 저장소 간 갱신은 모든 색인 저장소를 재검사하므로 대형 다중 저장소의 색인 시간과 실패 복구를 실제 환경에서 확인해야 한다. 합성 조회 벤치마크는 실제 색인·UI 지연과 장기 DB 증가량을 대체하지 않는다. 범용 Cypher·벡터 검색·전 언어 심벌 정확도·taint/PDG는 범위/엔진/정답집 미확정으로 미구현이다. 원문 질문 로그는 사용자 선택으로 제외했다. 실제 사용자 DB 복원·설치본 화면·접근성 검사는 미실시다.
- 다음 조치: 실제 저장소 표본에서 색인·조회·DB 증가량을 측정하고, 언어/분석 엔진과 검색 방식의 범위를 결정한 뒤 고급 분석을 별도 구현한다.

## 2026-09-28 - 써로웍스 MCP 설정 확인 및 스크립트 갱신

- 변경 파일: `C:\Users\surromind\Documents\ChatGPT\surroworks\work_support_mcp.py`; 기존 `C:\Users\surromind\.codex\config.toml`은 요청값과 일치하여 변경하지 않음
- 변경 사유: 지정된 인증 경로에서 제공한 최신 MCP 서버 스크립트로 교체하고 Codex의 기존 설정 경로·명령·환경을 확인할 필요가 있었다.
- 실행 명령: 지정된 `https://works.surromind.ai/api/mcp/client-script`에 인증된 GET, Windows 번들 Python으로 TOML 파싱·Python 구문 검사
- 검증 결과: 설정의 서버명·명령·스크립트 경로·API 기준 주소가 요청값과 일치하며 스크립트를 갱신했다. 설정 백업은 `C:\Users\surromind\.codex\config.toml.bak-surroworks-mcp-20260928_191135`, 기존 스크립트 백업은 같은 스크립트 경로의 `.bak-surroworks-mcp-20260928_191135`이다. 다운로드 63,075바이트, TOML/구문 검사 통과. 비밀값은 로그에 기록하지 않았다.
- 남은 위험: 설정 파일의 환경 변수 방식은 토큰을 평문으로 포함한다. 스크립트는 실행하지 않았으며 실제 MCP 연결 검사는 새 Codex 대화에서 수행해야 한다.
- 다음 조치: 새 대화에서 `써로웍스 MCP 사용법을 확인해줘. 허용된 도구만 사용해줘.`로 제공 도구 범위 내 동작을 확인한다.

## 2026-09-28 - SMSR 추적 연결 실패 후 재시도 중단

- 변경 파일: `docs/development-log.md`
- 변경 사유: 로컬 SMSR 서버가 중단돼 그래프 상태와 일일 활동 저장이 반복 실패했다.
- 실행 명령: SMSR `record_event` 2회, `record_daily_activity` 1회, `http://127.0.0.1:49783/health` 및 `SMSR.App` 프로세스 확인
- 검증 결과: 기록 도구 3회가 각각 60초 제한으로 종료됐다. 로컬 HTTP 연결이 거부됐고 실행 중인 `SMSR.App` 프로세스가 없었다. 제품 빌드·자체검사 결과에는 영향이 없다.
- 남은 위험: 현재 작업 그래프의 저장소 간 연결 노드와 최종 상태, 일일 활동 기록이 서버에 반영되지 않았다. 도구 외 SQLite 직접 기록은 하지 않았다.
- 다음 조치: 사용자가 SMSR 대시보드 서버를 다시 실행하면 `get_state`로 실제 상태를 확인하고 미기록 노드를 결과에 맞게 종결한 뒤 동일 활동 ID로 일일 기록을 한 번 저장한다. 서버가 복구되기 전에는 같은 호출을 재시도하지 않는다.

## 2026-09-28 - SMSR 실행 복구와 로컬 고급 검색·분석 연결

- 변경 파일: `GraphRuntime/*`, `GraphAdvanced*`, `GraphWorker.cs`, `GraphVectors.cs`, `GraphAnalysis.cs`, `EventStoreGraphAdvanced.cs`, 그래프/stdio 등록·화면·스키마·프로젝트 삭제, Git 실행 인코딩, `scripts/install-graph-runtime.ps1`, `scripts/verify-graph-backup.py`, 계획서 MD/HTML·안내 문서.
- 변경 사유: 사용자가 서버를 켜고 남은 구현·검증을 진행하도록 요청했다. SQLite와 기존 서비스 패턴을 재사용하고 질문·소스 원문을 저장하지 않는 로컬 도구로 연결했다.
- 실행 명령: `dotnet build/publish -c Release --no-restore`, `test_runtime.py --vectors`, `--graph-advanced-self-test`, `--graph-self-test`, `--tracking-self-test`, `--codex-config-self-test`, `scripts/test-mcp-stdio.ps1`, `dotnet test -c Release --no-restore`, `verify-graph-backup.py`, 실제 HTTP·브라우저 조회.
- 검증 결과: 빌드 경고/오류 0, 고급/기존 그래프·추적·Codex 설정 자체검사 통과, stdio 37도구 확인. Cypher 쓰기·파일 함수·다중 문장 차단, 방향 관계·200행 제한, 한영 오프라인 의미 순위, 8개 언어 구문/taint 양성·상수/함수간 격리 음성 검사 통과. 16개 문법 로딩 확인. 실제 첫 색인 337파일/887노드/592관계 1.115초, Cypher 투영 포함 430ms, 최초 벡터 생성 포함 25.992초(단일 측정). 운영 DB 온라인 백업→새 파일 복원, 전체 테이블 행 수 일치·integrity/foreign_key 검사 통과. 백업은 `%LOCALAPPDATA%/SMSR/verification-backups/20260928-105426-348871`에 보존했다.
- 실패·수정: 실제 한글 저장소를 영문 임시 폴더 시험이 놓쳤다. Git 출력 UTF-8을 명시하고 한글 경로 회귀시험을 추가했다. 리비전 Cypher 투영이 HTTP·worker 재현·직접 엔진 진단에서 3회 실패하여 단순 재시도를 중지했다. CSV 자동 인용/escape 감지의 순서 의존성을 원인으로 확인하고 구분자·quote·escape·직렬 읽기를 고정했다. 한글/쉼표/따옴표/역슬래시/개행 표본 및 실제 리비전 887노드 재검증이 통과했다.
- 남은 위험: 전 언어 의미 정확도·타입/별칭·함수간 정밀 PDG는 미구현이다. taint는 휴리스틱 검토 후보이며 오탐·누락이 있다. 의미 검색은 경로·제목·종류만 대상으로 본문 전체를 검색하지 않는다. Kuzu 조회는 제한된 읽기 방언이며 임의 확장/API는 차단한다. 별도 Python/ONNX 모델 설치와 패키지 유지보수 부담, 대형 실제 워크로드·장기 스냅샷 DB 증가량 및 스크린리더 전체 검수는 남는다.
- 최종 검증: 최신 publish 실행본 `artifacts/live-graph-final/SMSR.App.exe`를 시작했다. 고급/그래프/추적/Codex 설정/전체 자체검사 종료 코드 모두 0. 브라우저에서 Cypher 집계 5행, 한국어 의미 검색 20건, C# 선언/흐름 표·지원 범위 표시를 확인했다. 증분 리비전 2는 337파일/889노드/594관계이며 변경분 재임베딩 포함 검색 4.294초였다. 새 `graph_derived` 포함 운영 DB 복원본은 `verification-backups/20260928-110351-500595`에 보존했다. 전역 `core.hooksPath`는 미설정 상태다.
- 다음 조치: 전역 Git 훅은 설치하지 않으며 SQLite 원본을 유지한다. 전 언어 정밀 의미 분석·함수간 PDG·본문 의미 검색은 구문/메타데이터 기능과 별도 미구현 항목으로 남겨 완료로 오인하지 않도록 한다. 현재 실행본은 자체검사와 실제 화면 검증을 거쳤으나 독립 보안 감사·전 언어 정답집·장기 성능 검증을 대체하지 않는다.

## 2026-09-28 - P1 완료 감사와 파서·조회 경계 보강

- 변경 파일: `GraphMarkdownSyntax.cs`, `GraphMarkdown.cs`, `GraphMarkdownSelfCheck.cs`, `GraphFileScanner.cs`, `GraphLinkResolver.cs`, `GraphEvidenceService.cs`, `GraphSourceService.cs`, `GraphCrossRepoService.cs`, `GraphIndexService.cs`, `GraphContracts.cs`, `GraphQuery{Service,Path,Impact,Scope}.cs`, `EventStoreGraph{Schema,Format,Files,Nodes,Edges,Writes}.cs`, `GraphFormatMigration.cs`, `EventStore.cs`, `EventStoreDeletion.cs`, `Graph{Boundary,Unicode,Revision,Performance,Responsiveness}SelfCheck.cs`, `GraphIndexSelfCheck.cs`, `App.xaml.cs`, `SMSR.App.csproj`, P1 보고서·계획서 Markdown/HTML·가이드·개발 로그.
- 변경 사유: 기존 Markdown 정규식이 참조 링크·괄호·코드/주석 문맥을 놓쳤고, 파일명 대소문자·정규화와 조회 도중 재색인이 잘못된 관계를 만들 수 있었다. P1 요구별 완료 근거와 실제 실행본 검증을 보강했다.
- 구현 결정: Markdig 1.3.2 AST 재사용, 실제 원본 파일명 보존과 정규화 충돌 거부, 파서 형식 버전별 재색인, 리비전 스냅샷 조회·낙관적 쓰기 검사·건강도 읽기 트랜잭션, 탐색 한도와 절단 표시 보정. 별도 그래프 DB나 중복 근거 저장소는 추가하지 않았다.
- 실행 명령: Release build/publish; `--graph-self-test`, `--graph-advanced-self-test`, `--tracking-self-test`, `--codex-config-self-test`, `--self-test`, `--graph-benchmark`, `--graph-responsiveness-self-test`를 `Start-Process -Wait -PassThru`로 종료 코드 확인; `scripts/test-mcp-stdio.ps1`; `scripts/verify-graph-backup.py`; `git diff --check`; 실제 HTTP 재색인·브라우저 검색/근거/방향 경로/최신성 조회.
- 검증 결과: 최신 publish 빌드 경고/오류 0, 7개 자체검사 종료 코드 0, stdio 37도구. 고정 Markdown 표본 20건(양성 11/음성 9), 대소문자 rename·Unicode 원본·충돌 시 기존 색인 보존, 파서 버전 재생성, 오래된 스캔 거부·과거 리비전 격리, 경로 한도 1/2와 영향 절단 통과. 5천 파일/5만 노드/20만 관계 40회 p95 74.7747/83.5877ms. 1천 파일/2.1만 노드/4만 관계 색인 2,510.6ms, 동시 응답 77회 최대 120.9ms. 실제 중간 리비전 5는 345파일/898노드/595관계·고아/자가/중복 0. 실제 근거 열기·한글 검색·Enter 탐색·방향 1단계 경로·최신성 일치 표시를 확인했다.
- 데이터 보호: 운영 DB 온라인 백업→별도 파일 복원에서 전체 테이블 행 수 일치·무결성/외래키 통과. `verification-backups/20260928-120047-377209` 사본 보존. 작업 중 직접 실행한 이전 publish 프로세스만 경로 확인 후 교체했다. 전역 Git 훅 미적용·원문 질문 로그 제외 유지.
- 남은 위험: 전체 스크린리더/실제 IME 조합·별도 PC 설치·사용자 출시 승인은 미검수이며 한글 문자열 입력과 동일시하지 않는다. 정답집은 고정 표본에 한정된다. 장기 스냅샷 DB 증가·민감한 경로/제목에 대한 운영 정책과 P2 정밀 분석 미구현은 별도다.
- 다음 조치: `docs/p1-acceptance-2026-09-28.md`의 수동 출시 확인을 수행한다. P1 기능 완료와 P2 미구현·사용자 승인 대기를 혼동하지 않는다.

## 2026-09-28 - P2 본문 검색·관계 보강 및 16개 문법 점검

- 변경 파일: `GraphBody{Chunks,Search,SelfCheck,EndpointSelfCheck}.cs`, `GraphEmbeddingCache.cs`, `GraphVectors.cs`, 고급 화면/렌더링/HTTP/MCP/stdio 등록, `GraphRuntime/{embedding,full_embedding,test_full_embedding,symbols,language_cases,test_language_coverage,test_runtime}.py`, 피드백 서비스/화면/도구, `GraphCodeMask.cs`, 경로 추출/시험, 커뮤니티/지도/순환 리비전 조회, `GraphQueryScope.cs`, 저장소 간 버전/재시도 서비스·시험, `GraphIndexSelfCheck.cs`, `GraphAdvancedSelfCheck.cs`, `EventStoreGraphFormat.cs`, 계획서 MD/HTML·가이드·P2 진행 보고서.
- 변경 사유: P2의 본문 의미 검색이 없었고 수정됨 피드백, 다중 저장소 갱신 경쟁/실패 복구, 조회 리비전 일관성, 주석/문자열 경로 오탐 및 16개 문법 호출 표본에 공백이 있었다.
- 구현 내용: 기존 로컬 모델/worker/SQLite 파생 캐시를 재사용한다. 본문 1만 조각 한도·파일/폴더 범위·원문 해시 검증·비밀 패턴 파일 제외·질문/본문 미저장·근거 줄을 제공한다. 전체 토큰 창을 임베딩해 꼬리 잘림을 막는다. API/MCP 지도는 정적 구문 후보로만 취급한다. 저장소 간 전체 리비전 검증과 기존 연결 보존, 변경 없는 재색인 재시도를 추가한다.
- 실행 명령: Release build/publish; `--graph-self-test`, `--graph-advanced-self-test`, `--tracking-self-test`, `--codex-config-self-test`, `--self-test`의 프로세스 종료 코드 확인; `test_runtime.py`, `test_full_embedding.py`, `test_language_coverage.py`; `scripts/test-mcp-stdio.ps1`; `scripts/verify-graph-backup.py`; 실제 본문 HTTP·브라우저 조회.
- 검증 결과: 빌드 경고/오류 0, 자체검사 통과, stdio 38도구. 한영 본문 순위·범위·민감 파일 제외·원문 미저장·변경 거부·벡터 교체·프로젝트 삭제 정리·401/400·MCP 검색 통과. 16개 문법의 선언/호출/호출 소유 함수 표본 통과. Cypher 가변 경로·역방향·집계/필터·OPTIONAL MATCH·금지 질의 및 200행 상한 통과. 연결 갱신 충돌→이전 관계 보존→원인 제거 후 로컬 변경 없는 재색인 복구 통과. 운영 DB 별도 복원 `verification-backups/20260928-132533-678882`에서 전체 테이블 행 수·integrity·외래키 검사 통과.
- 수정한 시험 실패: 수정됨 판정 시험을 추가하면서 기존 HTTP/MCP 기대 문자열 `ERROR`가 남아 한 번 실패했다. 세 판정 시험을 유지하고 새 요청의 기대값을 `CORRECTED`로 수정해 재시험 통과했다. PowerShell에서 파일 경로 자체에 전달한 glob이 반복 실패했으므로 이후 텍스트 검색은 디렉터리와 `rg -g` 필터로 분리한다.
- 남은 위험: P2 전체는 미완료다. 사용자 지정 16개 대상의 타입/별칭/오버로드/함수간 의미·정밀 PDG/taint가 남는다. 구문 선언·호출 시험을 의미 정확도 시험으로 대체하지 않는다. 본문 1만 조각 실사용 성능·전체 접근성 수용·외부 언어 서버/SDK 설치 범위는 아직 검증/결정 전이다. 비밀 패턴 필터는 완전한 DLP가 아니다.
- 다음 조치: SMSR 전용 폴더의 추가 분석 서버/SDK 설치 범위를 사용자에게 확인한 뒤 16개 대상 분석기와 언어별 의미/함수간 정답집을 연결한다. 전역 PATH·Git 훅을 임의 변경하지 않으며 P2 목표를 완료로 변경하지 않는다.

## 2026-09-28 - P2 16개 문법 위치와 LSP 연결 기반

- 변경 파일: `GraphRuntime/{symbols,symbol_positions,test_language_coverage,test_lsp_server}.py`, `Mvp/GraphLsp{Wire,Session,Requests,Protocol,Job,Locations,Definitions,SelfCheck,BoundarySelfCheck,FailureSelfCheck}.cs`, `GraphAdvancedSelfCheck.cs`, `App.xaml.cs`, P2 진행 보고서·계획서 Markdown/HTML.
- 변경 사유: 16개 언어 전체 검증 요구를 유지하면서, 설치 범위 답변 전 진행 가능한 공통 통신·위치·원문 검증을 구현했다. 새 언어 서버/SDK나 패키지는 설치하지 않았다.
- 구현 내용: Tree-sitter 이름 토큰의 UTF-16 시작/끝 위치를 기존 구문 분석·저장 경로에 추가했다. stdio LSP 초기화/정의 조회/문서 수명주기, 크기·시간·메시지 수 상한, 편집/명령 거부, 서버 원문 오류 미노출, 색인 경계·해시·리비전·좌표 검증을 추가했다. 내부 정의 후보를 확정 호출/PDG 간선으로 승격하지 않는다.
- 실행 명령: `dotnet build src/SMSR.App/SMSR.App.csproj -c Release --no-restore`; `dotnet publish src/SMSR.App/SMSR.App.csproj -c Release --no-restore -o artifacts/live-p2-lsp-foundation`; `--graph-lsp-self-test`, `--graph-advanced-self-test`, `--graph-self-test`, `--tracking-self-test`, `--codex-config-self-test`, `--self-test`; Python `test_language_coverage.py`, `test_runtime.py`; `scripts/test-mcp-stdio.ps1`; `git diff --check`; 실제 HTTP 색인·분석·저장 조회.
- 검증 결과: 빌드 경고/오류 0, 위 자체검사와 16개 이름 위치·한국어/emoji/CRLF/복수 호출 검증 통과, stdio 38도구. 실제 리비전 9는 373파일/944노드/617관계이며 새 분석 보고서 4선언/8호출 위치 및 DB 저장 결과 일치·stale=false 확인. 기존 서버의 실행 경로를 확인한 뒤 새 실행본으로 교체했다.
- 실패와 수정: 첫 LSP 시험의 파일 노드 누락으로 스냅샷 원문 조회가 거절되어 올바른 색인 fixture로 수정했다. 이어 취소 시 Python 실행기 자손 잔류를 관찰했다. 종료 직렬화만으로 해결되지 않았으며 Windows Job Object를 도입한 뒤 기한 종료·명시 취소·자식/손자 종료 검사가 통과했다. 실패 시험의 확인된 Python 프로세스만 정리했다. 정리 오류로 원래 실패를 가리지 않도록 임시 GUID 폴더 정리를 보완했다.
- 남은 위험: 실제 서버별 의미 해소·타입/별칭/동적 호출·함수 간 PDG/taint·최종 P2 통합 검증은 미완료다. LSP는 아직 HTTP/MCP/UI에 활성화하지 않았다. Job 등록 전 자식 생성 경쟁, 서버 자체 빌드 훅/네트워크/확장은 실제 분석기 활성화 전에 검증해야 한다. 모의 서버 성공은 실제 언어 의미 정확성이나 보안 샌드박스 증거가 아니다.
- 다음 조치: 추가 분석 서버/SDK의 SMSR 전용 설치 범위 답변 후 검증된 서버 프로필과 실제 16개 언어 정답집을 연결한다. 전역 PATH·Git 훅 미변경과 원문 질문 로그 제외를 유지한다.

## 2026-09-28 - P2 C# 실제 컴파일러 의미 분석의 파일 묶음 범위

- 변경 파일: `src/SMSR.CSharpAnalysis/*`, `SMSR.slnx`, `SMSR.App.csproj`, `GraphWorker.cs`, `GraphProcessOutput.cs`, `GraphCSharp{Request,Analysis,Page,SelfCheck,StorageSelfCheck}.cs`, 고급 화면/HTTP/MCP/stdio 등록, `GraphAdvancedSelfCheck.cs`, `scripts/test-mcp-stdio.ps1`, `samples/graph-csharp/*`, README·P2 보고서·계획서 MD/HTML.
- 변경 사유: 추가 도구 설치 답변 없이도 기존 .NET SDK의 Roslyn을 재사용해 실제 타입/오버로드/별칭 해석을 진전시킬 수 있음을 확인했다. 16개 대상 전체 목표는 유지하고 입력 파일 묶음 분석을 실제 csproj 전체 의미 분석과 구분한다.
- 구현 내용: C# 12 기본·명시적 버전/조건부 심벌·호스트 프레임워크 참조의 다중 파일 컴파일 단위, 호출/생성자 대상·반환 타입·인자 대응·alias·근거 위치를 추출한다. 컴파일 오류/모호/가상/delegate/dynamic은 별도 판정이다. 파일 목록·해시·리비전·원문 미저장 계약을 기존 SQLite 파생 결과에 연결했다. 실제 대상 코드/MSBuild/analyzer/generator는 실행하지 않는다. 공통 worker에 출력 16 MiB 읽기 상한·stderr 미보존·Job 종료를 재사용했다.
- 실행 명령: 로컬 SDK packs만 원천으로 C# 보조 프로젝트 restore; Release build/publish; `SMSR.CSharpAnalysis.exe --self-test`; `--graph-lsp-self-test`, `--graph-self-test`, `--graph-advanced-self-test`, `--tracking-self-test`, `--codex-config-self-test`, `--self-test`; stdio 40도구 검사; 실제 색인·브라우저 분석·저장 결과 확인; `git diff --check`.
- 검증 결과: 빌드 경고/오류 0, 컴파일러 정답집 및 모든 자체검사 종료 코드 0. HTTP 출처 보호/경로 중복·탈출/DB 원문 미포함/의존 파일 변경·삭제/MCP 저장·조회 통과. 실제 리비전 11에서 예제의 `Format(int)`와 `Format(string)`을 별도 소스 메서드로 해소하고 진단 0·근거 줄·DB 재조회 화면 확인. 신규 실행본은 `artifacts/live-p2-csharp/SMSR.App.exe`다.
- 실패와 수정: 호스트 런타임 폴더의 모든 DLL을 참조하면 네이티브 DLL에 CS0009가 발생했다. 프레임워크 위치 안의 신뢰된 관리 어셈블리 목록만 참조하도록 수정해 실제 컴파일 검사를 통과했다. 첫 publish에서 출력 상대 경로 메타데이터 평가가 충돌해 NETSDK1152가 발생했다. 명시적 파일 item과 한정된 메타데이터로 수정해 언어별 리소스·라이선스 고지까지 정상 배포했다. 문서 패치 한 건의 존재하지 않는 문맥은 확인 후 바로잡았다.
- 남은 위험: 전체 프로젝트 참조/옵션/생성 코드, 모든 암묵적 호출 종류, 함수 간 PDG/taint/sanitizer 및 나머지 언어의 의미 분석은 미완료다. .NET 10 런타임이 없는 배포 대상의 설치/포함 방식, 외부 배포 라이선스 수용, 대형 컴파일 단위 성능·보안 검토도 남는다. 기존 구문 휴리스틱을 정확한 의미 분석으로 대체 완료한 것으로 표시하지 않는다.
- 추가 검증: 운영 DB 별도 백업·복원본 `verification-backups/20260928-141206-201635`에서 전체 테이블 행 수 일치·무결성 정상·외래키 오류 0 확인. 운영 DB는 교체하지 않았다. 그래프 사용 가이드의 C# 분석과 기록 제외 정보 제목 계층을 분리했다.
- 다음 조치: 실제 C# 프로젝트 맥락과 함수 간 분석을 추가 검증하고, 사용자 설치 범위 답변 후 나머지 언어 분석기를 연결한다. 원문 질문 제외·전역 Git 훅 미적용·명시적 저장소 참조 정책을 유지한다.

## 2026-09-28 - P2 C# 컴파일러 제어·변수 흐름 근거

- 변경 파일: `SMSR.CSharpAnalysis/{FlowContracts,FlowFacts,FlowBlocks,FlowOperations,FlowVariables,FlowSelfCheck,FlowBoundarySelfCheck,SymbolFacts,Analyzer,Contracts,Program}.cs`, `GraphCSharp{Analysis,Page,FlowPage,SelfCheck,StorageSelfCheck,VersionSelfCheck}.cs`, `GraphAdvancedPage.cs`, `samples/graph-csharp/Entry.cs`·README, P2 보고서·계획서 MD/HTML·추적 가이드.
- 변경 사유: 실제 호출 대상 분석 이후에도 제어 흐름은 구문 휴리스틱뿐이었다. 설치된 Roslyn으로 계산한 블록·분기·예외 영역·변수 요약을 후속 함수 간 분석의 근거로 제공한다. 새로운 SDK/분석 서버는 설치하지 않았다.
- 구현 내용: 명시적 멤버와 내부 지역/익명 함수 CFG, 도달 가능성, 조건·후속 블록·finally 경유, 연산/매개변수 ID, 본문 영역 변수 읽기/쓰기·유입/유출·항상 할당/캡처. 합산 4만 항목 한도·원문 미저장·기존 시간/출력 제한을 유지한다. 보고서 버전 2로 구버전 결과의 재분석을 요구한다. 좁은 화면에서는 내부 ID 대신 이름·근거 줄과 한국어 판정을 표시하며 전체 ID는 원자료에 보존한다.
- 실행 명령: `dotnet build`/`dotnet publish -c Release --no-restore`; `SMSR.CSharpAnalysis.exe --self-test`; 게시 앱 `--graph-lsp-self-test`, `--graph-self-test`, `--graph-advanced-self-test`, `--tracking-self-test`, `--codex-config-self-test`, `--self-test`; `scripts/test-mcp-stdio.ps1`; `scripts/verify-graph-backup.py`; `git diff --check`; 실제 HTTP 색인·브라우저 분석·저장 재조회.
- 검증 결과: 빌드 오류/경고 0, 컴파일러 및 모든 게시 자체검사 종료 0, stdio 40도구. 최종 UI 게시본에서도 고급 자체검사 0. 리비전 12(400파일/983노드/632관계) 예제에서 5개 함수와 `Demo.Flow`의 8블록·반복 역간선·반환·변수 요약을 실제 화면에서 확인했다. 브라우저 콘솔 오류 없음. 운영 DB의 별도 복원 `verification-backups/20260928-142859-175580`에서 행 수 일치·무결성 정상·외래키 오류 0, 운영 DB 미교체.
- 실패와 수정: 람다 식별자 예상 시험 실패를 조사해 Roslyn 문서 ID가 상위 함수가 다른 지역/익명 함수들을 구분하지 못함을 발견했다. 공통 심벌 ID를 해당 종류만 물리 위치 기반으로 수정하고 선언·호출·CFG 일치 및 동일 시그니처 6개 함수의 유일성 회귀를 통과했다. 예제 문서 패치의 잘못된 문맥 1건을 확인·수정했다. 추적 서버는 지정된 불투명 workflow ID를 읽기 쉬운 ID로 변환했으므로 반환 ID를 사용했다.
- 남은 위험: 변수 요약은 중첩 함수 구문을 포함하며 def-use/실행/taint 확정이 아니다. 런타임 예외 간선 전체, 최상위 문장·필드 초기화·암묵적 멤버, 프로젝트별 참조/생성 코드, 함수 간 PDG·sanitizer, 나머지 언어의 실제 의미 분석 및 최종 P2 수용은 미완료다. 파일 위치 ID는 수정 시 달라지므로 리비전/해시를 함께 사용한다.
- 경계 검증: 8,500개 할당이 4만 연산을 넘을 것이라는 시험 가정이 틀려 처음에는 거부 검사가 실패했다. 실제 34,001개 연산임을 측정하고 11,000개 할당으로 수정했다. 공개 프로세스가 일반 오류로 종료하고 내부 정답집은 정확히 흐름 한도 예외를 확인한다.
- 후속 멤버 검증: 표현식 속성/인덱서 선언에서 직접 CFG를 만들면 `NO_CFG`가 나옴을 확인했다. `FlowRoots.cs`에서 getter 소속 루트 연산을 선택하고 `FlowMembersSelfCheck.cs`에 생성자·소멸자·속성·인덱서·연산자·자동 접근자 구분을 추가했다. 최종 분석 버전은 3으로 올려 초기 버전 2도 오래됨으로 처리하며 MCP 버전 회귀에 포함했다.
- 다음 조치: 기존 컴파일러 CFG를 기준으로 함수 간 인자/반환 연결과 실제 데이터·제어 의존을 구현·검증한다. 다른 언어 설치 범위 답변과 무관하게 가능한 C# 작업은 계속할 수 있다. 원문 질문 제외·전역 Git 훅 미적용·SQLite 원본 유지 정책을 바꾸지 않는다.
- 최종 확인: 버전 3 게시본 `artifacts/live-p2-csharp-flow-ui/SMSR.App.exe`, 리비전 15(402파일/986노드/633관계), 5함수/예제 8블록·DB `stale=false`. 컴파일러와 최종 게시 고급 자체검사 0, stdio 40도구. 별도 복원 `verification-backups/20260928-143754-389858`의 전체 행 수·무결성·외래키 검사 통과. 최종 화면 캡처 `smsr-p2-csharp-flow.png` 보존.

## 2026-09-28 - P2 C# 함수 간 인자·반환 연결 근거

- 변경 파일: `src/SMSR.CSharpAnalysis/{BoundaryContracts,BoundaryFacts,ArgumentFacts,CallConnections,ConnectionSelfCheck,ConnectionBoundarySelfCheck,ConnectionDispatchSelfCheck,ConnectionLimitSelfCheck,Contracts,CallFacts,FlowContracts,FlowFacts,Analyzer,Program}.cs`, `GraphCSharp{Analysis,Page,ConnectionsPage,SelfCheck,ConnectionsSelfCheck,StorageSelfCheck,VersionSelfCheck}.cs`, `GraphAdvancedPage.cs`, 고급 MCP/stdio 설명, `samples/graph-csharp/*`, P2 보고서·계획서 MD/HTML·추적 가이드.
- 변경 사유: 기존 컴파일러 호출 대상과 CFG를 재사용해 호출 인자가 어느 매개변수에 대응하고 어떤 반환 지점이 후보인지 화면과 DB에 남긴다. 새 분석 서버·SDK·의존성은 설치하지 않았다.
- 구현 내용: 직접 호출의 단일 본문에 한해 인자/매개변수·수신자·일반 값 반환 후보를 연결한다. 명명/기본/params/확장/제네릭/재귀/partial 호출을 검증하고 가상·delegate·동적·제약 타입·조건부 호출은 확정 연결에서 제외한다. ref 인자와 async/iterator/ref 반환·생성자는 별도 상태로 구분한다. 연결 확장 2만 항목 상한, 원문/리터럴 미저장, 기존 시간·출력 제한을 유지한다. 분석 버전 4와 구버전 0/2/3의 재분석 판정, 한국어 상세 화면을 연결했다.
- 실행 명령: `dotnet build -c Release --no-restore`, `dotnet publish -c Release --no-restore -o artifacts/live-p2-csharp-connections`; 게시 보조 분석기 `--self-test`; 게시 앱 `--graph-lsp-self-test`, `--graph-self-test`, `--graph-advanced-self-test`, `--tracking-self-test`, `--codex-config-self-test`, `--self-test`; `scripts/test-mcp-stdio.ps1`; `scripts/verify-graph-backup.py`; `git diff --check`; 실제 HTTP 분석·조회와 브라우저 상세 확인.
- 검증 결과: 빌드 경고/오류 0, 컴파일러 및 게시 자체검사 모두 종료 0, stdio 40도구. 실제 리비전 16(412파일/997노드/634관계)에서 분석 버전 4·진단 0·7함수·5호출 중 3개 직접 본문 연결·인자 4개·반환 후보 4개 및 DB `stale=false`를 확인했다. HTTP/MCP/DB의 교차 파일 근거와 구버전 무효화 검사가 통과했다. 브라우저 오류 0, 화면 `smsr-p2-csharp-connections.png` 보존. 새 실행본은 127.0.0.1:49783에만 바인딩했다.
- 실패와 수정: partial 메서드 시험이 non-void 접근 지정자를 빠뜨려 CS8796으로 실패했다. 양쪽 선언에 `private`를 명시한 유효 코드로 수정한 뒤 구현 본문의 매개변수 위치 대응 검사를 통과했다.
- 데이터 보호: 운영 DB는 교체하지 않았다. 별도 백업/복원본 `verification-backups/20260928-145558-842472`에서 모든 테이블 행 수 일치·무결성 정상·외래키 오류 0을 확인했다. 원문 질문 로그 제외, 전역 Git 훅 미적용, SQLite 원본 유지 정책은 그대로다.
- 남은 위험: 연결은 값 전파·실행 순서·PDG·taint 확정이 아니다. 경로 조건, 힙/ref 별칭, 캡처, 비동기/iterator, 암묵 변환 효과, 프로젝트 참조·생성 코드와 나머지 언어 의미 분석은 미완료다. 반환 후보는 실행 가능 경로가 검증되지 않은 MAY 관계다. 전체 16개 언어 목표는 완료 처리하지 않는다.
- 다음 조치: 기존 CFG의 실제 def-use/제어 의존 및 함수 간 값 전파를 검증하고, 다른 언어 분석기는 설치 범위 결정과 실제 정답집 검증을 거쳐 연결한다.

## 2026-09-29 - P2 C# 할당·읽기 도달 정의

- 변경 파일: `src/SMSR.CSharpAnalysis/{DefinitionContracts,DefinitionOperations,DefinitionFacts,ReachingDefinitions,DefinitionSelfCheck,DefinitionBoundarySelfCheck,FlowContracts,FlowFacts,Program}.cs`, `src/SMSR.App/Mvp/GraphCSharp{Analysis,SelfCheck,VersionSelfCheck,ConnectionsSelfCheck,DefinitionsPage,FlowPage}.cs`, `GraphAdvancedPage.cs`, P2 보고서·계획서 MD/HTML·추적 가이드.
- 변경 사유: 인자/반환 위치 연결과 변수 영역 요약만으로는 어떤 할당이 특정 읽기에 도달하는지 알 수 없다. 설치된 Roslyn의 CFG와 기존 저장/화면 구조를 재사용해 함수 내부 정상 경로의 도달 정의를 추가했다.
- 구현 내용: 읽기·쓰기의 평가 순서 추출, 진입 매개변수 정의, 덮어쓰기 제거, 분기 합류와 반복 고정점, 도달 불가 제외. 기본형/문자열 변수 범위 밖과 참조·캡처·예외·알 수 없는 연산은 사유와 함께 분석 불가로 표시한다. 함수별 2만 간선·입력 묶음별 25만 작업 및 기존 한도 초과 시 전체 분석을 거부한다. 결과 버전 5, 과거 0/2/3/4 무효화, 상세 화면의 할당 후보→읽기 위치를 연결했다.
- 실행 명령: `dotnet build src/SMSR.CSharpAnalysis/SMSR.CSharpAnalysis.csproj -c Release --no-restore`; `dotnet build src/SMSR.App/SMSR.App.csproj -c Release --no-restore`; `dotnet publish src/SMSR.App/SMSR.App.csproj -c Release --no-restore -o artifacts/live-p2-csharp-definitions`; 보조 분석기 `--self-test`; 앱 `--graph-lsp-self-test`, `--graph-self-test`, `--graph-advanced-self-test`, `--tracking-self-test`, `--codex-config-self-test`, `--self-test`; stdio 검사; SQLite 별도 복원 검사; `git diff --check`; 실제 색인·UI 분석·HTTP 저장 조회.
- 검증 결과: 빌드 경고/오류 0, 소스/게시 컴파일러 및 게시 앱 자체검사 모두 종료 0, stdio 40도구. 덮어쓰기·분기·반복·복합/중첩 순서·미도달·경계 판정 통과. 게시 후 추가한 150분기×150읽기 확장 상한 검사도 소스 빌드에서 정확한 2만 간선 한도 예외로 통과했다. 실제 리비전 17(419파일/1005노드/635관계), 분석 버전 5·진단 0·7함수, `Demo.Flow`의 도달 정의 11개와 `stale=false`를 화면/DB에서 확인했다. 브라우저 콘솔 오류 0.
- 데이터 보호: 확인한 이전 소유 서버 실행본만 새 게시본으로 교체했고 127.0.0.1:49783을 유지한다. 별도 백업/복원 `verification-backups/20260928-150541-608422`의 전체 테이블 행 수·무결성·외래키 검사 통과. 운영 DB 미교체, 신규 SDK 미설치, 원문 질문 제외와 전역 Git 훅 미적용 유지.
- 실패와 수정: 첫 게시 보조 분석기 검사는 폴더 이름을 잘못 지정해 실행되지 않았다. 실제 `CSharpAnalysis` 경로를 확인하고 종료 코드가 검증되는 전체 검사를 다시 실행했다. 문서 패치의 잘린 문맥과 HTML 중복 행은 실제 내용을 확인해 수정했다.
- 남은 위험: 정상 CFG 경로의 보수적 할당 후보이며 경로 조건, 필드/힙, 참조/캡처, 비동기/iterator, 변수 간 표현식 값 전달, 제어 의존, 함수 간 PDG/taint를 완료한 것이 아니다. 16개 대상 전체 정밀 의미 분석과 프로젝트 맥락·성능·최종 수용은 계속 남아 있다.
- 다음 조치: 연산 결과의 값 의존 및 제어 의존을 연결하고, 기존 함수 경계 근거와 통합해 함수 간 분석을 검증한다. 다른 언어 분석기 설치 범위 결정과 무관하게 가능한 구현은 계속 진행한다.

## 2026-09-29 - P2 C# 제어 의존 및 상수 제외 간선

- 변경 파일: `src/SMSR.CSharpAnalysis/{ControlContracts,ControlGraph,ControlPostdominators,ControlFacts,ControlSelfCheck,ControlOracleSelfCheck,ControlLimitSelfCheck,FlowContracts,FlowFacts,FlowBlocks,ReachingDefinitions,DefinitionSelfCheck,Program}.cs`, `src/SMSR.App/Mvp/GraphCSharp{Analysis,SelfCheck,ConnectionsSelfCheck,VersionSelfCheck,ControlPage,FlowPage}.cs`, `GraphAdvancedPage.cs`, P2 보고서·계획서 MD/HTML·사용 가이드.
- 변경 사유: 할당→읽기 근거와 별도로 조건이 실행 여부를 좌우하는 블록을 식별해야 한다. 기존 컴파일러 CFG와 저장/화면 계약을 재사용해 후지배 기반 제어 의존을 추가했다. 새 의존성·언어 도구는 설치하지 않았다.
- 구현 내용: 정상 반환/명시 throw의 공통 가상 종료, 후지배 고정점과 직후지배자, 조건 선택별 영향 블록, 반복 자기 의존. 컴파일 오류·종료 불가·예외 영역·비동기는 사유를 표시한다. 초기 집합 할당을 포함한 400만 계산 작업/묶음·2만 제어 간선/함수 및 기존 안전 한도를 적용한다. 저장 버전 6, 구버전 0/2/3/4/5 갱신 필요 판정, 실제 조건 fixture의 API/MCP/DB 검사와 UI를 연결했다.
- 실패와 수정: throw 시험이 2개 간선을 예상했지만 정상 반환 종료 블록도 조건부로 실행되므로 실제 3개가 옳음을 확인해 정답을 수정했다. 무한 루프 시험에서는 블록 도달 가능성만으로 불가능한 상수 분기 간선을 제거할 수 없음을 발견했다. 공통 `FlowBranch.ConstantExcluded`를 추가해 컴파일러 확정 간선을 데이터/제어 의존 양쪽에서 제외하고 가짜 루프 탈출 정의 전파 회귀를 통과했다. HTML 상태 표의 중복 행은 제거하고 개수를 확인했다.
- 실행 명령: `dotnet build src/SMSR.CSharpAnalysis/SMSR.CSharpAnalysis.csproj -c Release --no-restore`; `dotnet build src/SMSR.App/SMSR.App.csproj -c Release --no-restore`; `dotnet publish src/SMSR.App/SMSR.App.csproj -c Release --no-restore -o artifacts/live-p2-csharp-control`; 보조 분석기 `--self-test`; 앱 `--graph-lsp-self-test`, `--graph-self-test`, `--graph-advanced-self-test`, `--tracking-self-test`, `--codex-config-self-test`, `--self-test`; stdio 검사; SQLite 별도 복원 검사; `git diff --check`; 실제 색인·UI 분석·HTTP 저장 조회.
- 검증 결과: 빌드 경고/오류 0, 컴파일러와 게시 앱 자체검사 모두 종료 0, stdio 40도구. 1,000개 소형 그래프를 독립적인 노드 제거/종료 경로 탐색과 대조해 후지배·제어 간선을 검증했다. 분기·조기 반환·반복·명시적 종료·무한 루프·상수 제외·계산/간선 상한 회귀 통과. 실제 리비전 18(427파일/1015노드/637관계), 버전 6에서 `Demo.Flow`의 제어 의존 4개·직후지배 8개·도달 정의 11개와 DB `stale=false`를 확인했다. 브라우저 오류 0.
- 데이터 보호: 확인된 이전 서버 실행본만 새 게시본으로 교체했다. 127.0.0.1 바인딩·원문 질문 제외·전역 Git 훅 미적용 유지. 별도 복원 `verification-backups/20260928-152121-924950`에서 전체 테이블 행 수·무결성·외래키 검사 통과, 운영 DB 미교체.
- 남은 위험: 종료로 이어지는 모델 CFG 기준이며 암묵적 예외·모든 비종료 행위·실제 경로 조건·함수 간 값 전파·힙/참조·전체 PDG/taint 인증이 아니다. 제곱 크기 집합 알고리즘은 한도로 제한되며 대형 CFG 실측과 최적화가 남는다. 전체 16개 대상의 프로젝트 맥락·정밀 분석·최종 수용 목표는 미완료다.
- 다음 조치: 연산 값 의존을 기존 도달 정의·제어 의존과 연결하고, 호출 인자/반환 문맥을 통합해 함수 간 분석을 진전시킨다.

## 2026-09-29 - P2 C# 연산·할당·반환 값 의존

- 변경 파일: `src/SMSR.CSharpAnalysis/{ValueContracts,ValueBuilder,ValueOperations,ValueExpressions,ValueCallPorts,ValueFacts,ValueSelfCheck,ValueBoundarySelfCheck,DefinitionContracts,DefinitionOperations,DefinitionFacts,Program}.cs`, `src/SMSR.App/Mvp/GraphCSharp{Analysis,SelfCheck,VersionSelfCheck,ConnectionsSelfCheck,ValuesSelfCheck,ValuesPage,FlowPage}.cs`, `GraphAdvancedPage.cs`, P2 보고서·계획서 MD/HTML·사용 가이드.
- 변경 사유: 도달 정의와 제어 의존만으로는 서로 다른 변수·연산 결과·반환값 사이의 연결을 따라갈 수 없다. 기존 컴파일러 연산과 도달 정의 계산을 재사용해 함수 내부 값 그래프와 호출 경계 근거를 추가했다. 새 SDK/의존성은 설치하지 않았다.
- 구현 내용: 기존 읽기/쓰기 지점을 연산 객체로 대응하고 연산 입력·할당·도달 정의·임시 값 도달 정의를 연결한다. 전위/후위 증감·복합 할당의 결과를 구분하며 조건 임시 값은 CaptureId별로 기존 고정점 계산을 적용한다. 반환/조건/호출 인자·수신자·결과 포트와 대상 ID/매개변수 순번/호출 위치를 제공한다. 미해석 호출·힙·수신자·사용자 연산자에는 입력→결과 간선을 임의 생성하지 않는다. 함수별 값 노드+간선+포트 5만 한도와 기존 계산/시간/출력 상한을 적용한다.
- 실행 명령: `dotnet build src/SMSR.App/SMSR.App.csproj -c Release --no-restore`; 보조 분석기 Release build 및 `--self-test`; `dotnet publish src/SMSR.App/SMSR.App.csproj -c Release --no-restore -o artifacts/live-p2-csharp-values`; 게시 앱 `--graph-lsp-self-test`, `--graph-self-test`, `--graph-advanced-self-test`, `--tracking-self-test`, `--codex-config-self-test`, `--self-test`; stdio 검사; SQLite 별도 복원 검사; `git diff --check`; 실제 색인·UI 분석·HTTP 저장 조회.
- 검증 결과: 빌드 경고/오류 0, 컴파일러 및 게시 앱 자체검사 모두 종료 0, stdio 40도구. 산술 변수 전달·덮어쓰기·복합/전후 증감·단일/중첩 조건 임시 값·반복·불확실 호출 차단·힙/참조/수신자·원문 미노출·5만 항목 상한 통과. 마지막 사용자 정의 변환의 불확실 결과 차단 검사도 소스 빌드에서 통과했다. HTTP/MCP/DB 입력→반환 경로와 가짜 호출 결과 간선 부재, 구버전 0/2/3/4/5/6 갱신 필요 판정 통과.
- 실제 적용: 버전 7·리비전 20(437파일/1027노드/639관계), `Demo.Flow`의 값 노드 14개·연결 17개·반환/조건 포트 3개·DB `stale=false` 확인. `Demo.Run`의 미해석 호출 결과를 실제 화면에서 구분했다. 브라우저 오류 0, `smsr-p2-csharp-values.png` 보존. 확인된 이전 서버 실행본만 새 게시본으로 교체했고 127.0.0.1 바인딩을 유지했다.
- 실패와 수정: 무인자 메서드 문서 ID에도 괄호가 있을 것으로 가정한 정답집 조회가 실패했다. 선언 ID를 기준으로 조회하도록 고쳐 통과했다. 문서 패치가 실제 제목과 달라 적용되지 않았다. 앞선 문서 문맥 실패와 같은 유형이므로 추정 제목으로 재시도하는 방식을 중단하고 현행 제목/인접 내용을 읽은 뒤 작은 패치로 나누어 검증했다.
- 데이터 보호: 별도 복원 `verification-backups/20260928-153607-473110`의 전체 행 수·무결성·외래키 검사 통과. 운영 DB 미교체. 원문 질문 제외·비밀/리터럴 값 미기록·전역 Git 훅 미적용을 유지한다.
- 남은 위험: 미해석 경계가 존재하므로 연결 부재는 안전성 증거가 아니다. 현재 함수 내부 값 의존이며 함수 간 문맥별 값 전달·힙/별칭·캡처·예외·프로젝트 참조·16개 언어 정밀 의미 분석과 최종 PDG/taint 수용은 미완료다. 식의 실제 값·조건 성립·실행 이력을 계산한 것이 아니다.
- 다음 조치: 함수 경계 포트와 본문 값 그래프를 호출 문맥별로 연결하고, 재귀·다중 호출·반환 요약·불확실 값의 전파 정책을 검증한다.

## 2026-09-29 - P2 C# 호출별 반환 의존 요약

- 변경 파일: `src/SMSR.CSharpAnalysis/{ReturnContracts,ReturnSites,ReturnTraversal,ReturnSummaries,ReturnOutput,ReturnSelfCheck,ReturnBoundarySelfCheck,Contracts,Analyzer,Program}.cs`, `src/SMSR.App/Mvp/GraphCSharp{ReturnsPage,ReturnsSelfCheck,Analysis,SelfCheck,VersionSelfCheck,ConnectionsSelfCheck,FlowPage,ValuesPage}.cs`, `GraphAdvancedPage.cs`, P2 보고서·계획서 MD/HTML·사용 가이드.
- 변경 사유: 함수 내부 값 그래프와 인자 대응만으로는 어느 입력이 다른 함수의 반환에 영향을 주는지 알 수 없다. 기존 근거를 호출별 반환 요약으로 연결해 같은 함수를 여러 번 호출할 때 값이 섞이지 않도록 했다. Ponytail 원칙에 따라 기존 컴파일러·값 그래프·저장 계약을 재사용하고 새 SDK/DB/의존성은 추가하지 않았다.
- 구현 내용: 호출자·호출 위치·대상·매개변수 순번의 정확한 대응, 확인된 입력 집합 고정점 후 불확실성 해소, 호출별 입력→결과 간선과 함수 반환 요약. 재귀 값 순환·외부/가상·지원 불가 경계를 미확정으로 보존한다. 반환에 쓰이지 않는 미해석 결과는 반환 영향으로 오염시키지 않는다. 기존 내부 값 그래프를 변경하지 않고 버전 8 보고서에 별도 저장한다. 계산 200만 작업 및 기존 시간/출력 상한을 적용한다.
- 실행 명령: 보조 분석기/앱 `dotnet build -c Release --no-restore`; 분석기 `--self-test`; 앱 `--graph-advanced-self-test`; `dotnet publish src/SMSR.App/SMSR.App.csproj -c Release --no-restore -o artifacts/live-p2-csharp-returns`; 게시 앱 `--graph-lsp-self-test`, `--graph-self-test`, `--graph-advanced-self-test`, `--tracking-self-test`, `--codex-config-self-test`, `--self-test`; `scripts/test-mcp-stdio.ps1`; `scripts/verify-graph-backup.py`; 계획 상태표 열 수 검사; `git diff --check`; 실제 색인·화면 분석·HTTP 저장 조회.
- 검증 결과: 빌드 경고/오류 0, 보조 분석기와 게시 앱 자체검사 모두 종료 0, stdio 40도구. 호출 격리·중첩·이름/기본/확장 인자·지역 함수·재귀/상호/무기저 재귀·선언 순서 독립성·미해석 결과 사용/폐기·제어 구분·계산 상한 통과. HTTP/MCP/DB에서 반환 입력과 호출별 간선 확인, 구버전 0/2/3/4/5/6/7 갱신 필요 판정 통과.
- 실제 적용: 게시본 버전 8·리비전 21(446파일/1039노드/642관계), 예제 반환 요약 7개. `Demo.Connect`의 확인된 입력 `input`·호출별 연결 2개, `Demo.Run`의 미확정 반환 영향을 화면에서 확인했다. DB `stale=false`, 진단 0·브라우저 오류 0. 확인된 이전 실행본만 교체하고 127.0.0.1 바인딩을 유지했다.
- 실패와 수정: 사용자 변환이 항상 인자 변환 메타데이터에 있을 것이라는 정답집 가정이 실패했다. 실제 컴파일러는 자식 변환 노드로 표현했으며 해당 값의 미확정 상태가 호출 반환까지 보존됨을 확인해 검사를 고쳤다. HTML 상태표에 중복 셀을 넣은 편집을 즉시 수정했다. 위치 기반 표 검사는 다른 첫 표를 선택해 실패했으므로 제목이 포함된 상태표 하나를 선택해 17행 모두 3열임을 검증했다. 이후 표 편집은 제목 기반 전체 행 검증을 함께 수행한다.
- 데이터 보호: 별도 복원 `verification-backups/20260928-155407-936104`의 전체 테이블 행 수·무결성·외래키 검사 통과. 운영 DB 미교체. 원문 질문/비밀 값 미기록·전역 Git 훅 미적용·신규 SDK 미설치 유지.
- 남은 위험: 명시적 데이터 의존의 모델 경로 합집합이며 실제 값/경로 특수화·암묵 제어 흐름·힙/별칭·부수 효과·재귀 종료·taint 증명이 아니다. 함수별 요약은 지원 입력 파일 묶음에 한정한다. 전체 16개 언어의 프로젝트 문맥·정밀 심벌/PDG/taint와 최종 수용은 미완료다.
- 다음 조치: 반환 요약의 불확실 영향에 원인/경로 근거를 연결하고 제어·부수 효과 경계와 프로젝트 문맥을 보강한다. 나머지 언어는 설치 범위 결정 및 실제 의미 정답집을 확인하며 전체 목표를 유지한다.

## 2026-09-29 - P2 C# 호출 인자 근원과 경로

- 변경 파일: `src/SMSR.CSharpAnalysis/{ArgumentOrigins,ArgumentOriginsSelfCheck,ArgumentPathSelfCheck,ReturnContracts,ReturnTraversal,ReturnSummaries,ReturnOutput,Program}.cs`, `src/SMSR.App/Mvp/GraphCSharp{ArgumentsPage,ArgumentsSelfCheck,Analysis,SelfCheck,VersionSelfCheck,ReturnsSelfCheck,ConnectionsSelfCheck,FlowPage}.cs`, `GraphAdvancedPage.cs`, `samples/graph-csharp/{Entry.cs,README.md}`, P2 보고서·계획서 MD/HTML·사용 가이드.
- 변경 사유: 반환 요약만으로는 반환값 없는 출력/전송 호출에 어떤 입력이 전달되는지 추적할 수 없다. 기존 값 그래프·호출별 반환 요약·탐색·저장 계약을 재사용해 호출 인자까지의 입력 의존을 연결했다. 새 SDK/DB/의존성은 도입하지 않았다.
- 구현 내용: 호출 인자별 입력 순번과 대표 경로, 물리 위치·값 ID·호출 대상 판정·값 불확실성을 저장한다. 역방향 탐색에서 최초 발견 간선을 보존해 비순환 근거 경로를 만들며 기존 요약의 호출 격리를 유지한다. void 반환 불가와 인자 분석 가능을 구분하고 덮어쓰기·기본 인자·미확정 영향을 그대로 반영한다. 인자/입력/근거 간선 2만 항목, 기존 계산 200만 작업·시간·출력 한도를 적용한다.
- 실행 명령: 분석기/앱 Release `dotnet build --no-restore`; 분석기 `--self-test`; 앱 `--graph-advanced-self-test`; `dotnet publish src/SMSR.App/SMSR.App.csproj -c Release --no-restore -o artifacts/live-p2-csharp-arguments`; 게시 앱 `--graph-lsp-self-test`, `--graph-self-test`, `--graph-advanced-self-test`, `--tracking-self-test`, `--codex-config-self-test`, `--self-test`; `scripts/test-mcp-stdio.ps1`; `scripts/verify-graph-backup.py`; 상태표 열 수·`git diff --check`; 실제 색인/브라우저 분석/HTTP 저장 조회.
- 검증 결과: 빌드 경고/오류 0, 컴파일러 및 게시 앱 자체검사 전부 종료 0, stdio 40도구. void·중첩 반환·호출 격리·덮어쓰기·이름/기본 인자·반복·재귀/외부 불확실성 통과. 근거의 입력·유효 간선·연속성·끝점·비순환성·한도 검증. HTTP/MCP/DB 입력→호출 인자 2간선과 이전 버전 0/2/3/4/5/6/7/8 갱신 필요 판정 통과. 계획 상태표 18행/3열 검증.
- 실제 적용: 버전 9·리비전 22(451파일/1047노드/645관계), 함수 8개. `Demo.Publish`는 호출 인자 3개와 `input → Console.WriteLine(int)`의 6간선 근거를 제공한다. 기본 인자 의존 없음과 외부 본문 판정을 별도로 표시했다. 진단 0·브라우저 오류 0·DB `stale=false`. 확인된 이전 실행본만 새 게시본으로 교체했고 127.0.0.1 바인딩을 유지했다.
- 검증 중 조정: Windows 경로에 셸 glob을 직접 넘긴 검색이 실패했다. 같은 유형의 재시도를 피하도록 디렉터리와 `rg -g` 파일 필터를 분리해 확인했다. 소스/API 정답집 자체의 실패는 없었다. HTML은 제목이 포함된 상태표를 직접 선택해 모든 행의 열 수를 검사했다.
- 데이터 보호: 별도 복원 `verification-backups/20260928-160723-502211`에서 전체 테이블 행 수·무결성·외래키 검사 통과. 운영 DB 미교체. 소스·비밀 값·원문 질문 미기록, 전역 Git 훅 미적용·신규 SDK 미설치 유지.
- 남은 위험: 입력별 모델 데이터 경로 하나이며 실제 실행·경로 성립·부수 효과·힙/별칭·암묵 제어·source/sink/sanitizer 정책이나 보안 인증이 아니다. 반환 요약 경유는 대상 본문의 전체 경로를 펼치지 않는다. 중첩 void 내부 sink 전개·C# 프로젝트 맥락·나머지 언어 정밀 분석 및 전체 PDG/taint 수용은 미완료다.
- 다음 조치: 명시적 source/sink 정책과 함수 경계 위험 경로를 연결하고, 설치된 다른 컴파일러로 진행 가능한 언어를 확인해 16개 대상의 실제 의미 분석을 확대한다.

## 2026-09-29 - P2 Java 17 실제 컴파일러 분석

- 변경 파일: `src/SMSR.JavaAnalysis/*.java`, `src/SMSR.App/GraphRuntime/{java_bundle,test_java_bundle,test_java_boundaries,worker}.py`, `src/SMSR.App/Mvp/GraphJava*.cs`, `GraphAdvanced{Page,Rendering,Endpoints,Tools,SelfCheck}.cs`, `StdioGraphAdvancedTools.cs`, `McpHttpGateway.cs`, `SMSR.App.csproj`, `scripts/{build-java-analysis,test-mcp-stdio}.ps1`, Java 예제·계획서·P2 보고서·사용 가이드.
- 변경 사유: 16개 언어 전체라는 사용자 범위에 따라 C# 외의 실제 의미 모델을 확대했다. 설치된 JDK의 표준 compiler API와 기존 실행/저장/화면 구조를 재사용했다. 새 SDK·외부 라이브러리·DB를 추가하지 않았다.
- 구현 내용: Java 17 명시 파일 묶음의 선언/참조/오버로드/제네릭 타입과 호출·인자 대응, 가상/참조/람다/컴파일 오류 판정, UTF-16 물리 위치. 원문·상수·annotation 값·원시 진단 메시지 미저장. 소스/클래스/모듈/processor 경로 차단, JVM 환경 옵션 제거, 시간/메모리/출력 제한. Java 버전 1 보고서와 stale 판정, HTTP/MCP 및 화면 연결.
- 실행 명령: `scripts/build-java-analysis.ps1`; `dotnet build src/SMSR.App -c Release --no-restore`; `dotnet publish src/SMSR.App -c Release --no-restore -o artifacts/live-p2-java`; `test_java_bundle.py` 소스/게시 클래스 정답집; C# 분석기 `--self-test`; 게시 앱 `--graph-advanced-self-test`, `--graph-self-test`, `--graph-lsp-self-test`, `--tracking-self-test`, `--codex-config-self-test`, `--self-test`; `scripts/test-mcp-stdio.ps1`; `scripts/verify-graph-backup.py`; 실제 색인/화면 분석/저장 조회; 상태표 열 수와 `git diff --check`.
- 검증 결과: 빌드·게시 성공, Java/C# 정답집 및 게시 앱 자체검사 모두 종료 0, stdio 42도구. Java 오버로드·제네릭·가상/직접/메서드 참조·람다·varargs·CRLF/탭/한글·원문 보호·외부 소스 차단 통과. HTTP/MCP/DB 입력 검사·묶음 순서 독립·구버전/소스 변경 stale·프로젝트 삭제 정리 통과.
- 실패와 수정: javac의 파일 래퍼로 선언 위치가 누락되어 URI 허용 목록으로 수정했다. 외부 심벌 ID에 소유자를 추가했다. 통합 정답집의 PascalCase MCP 인자 오류는 실제 도구 스키마의 camelCase로 고쳤고 Java 장기 호출에도 기존 3분 제한을 적용했다. 문서 편집에서 중복 제목/행과 잘못된 패치 문맥이 반복되어 삽입을 멈추고 실제 제목·전체 행 기준으로 재확인했다. 최종 상태표 19행/3열 및 추가 개발 행 하나를 확인했다. Windows 검색은 디렉터리와 `-g` 필터를 사용한다.
- 실제 적용: 확인된 이전 프로세스만 교체한 게시본 `artifacts/live-p2-java/SMSR.App.exe`, 127.0.0.1:49783. 리비전 23(472파일/1081노드/658관계), Java 선언 16개·호출 6개·진단 0·저장 결과 `stale=false`. 직접 오버로드와 가상 호출의 정적 대상 표시를 화면에서 확인했다.
- 데이터 보호: `verification-backups/20260928-163021-463989`의 별도 복원에서 전체 테이블 행 수·무결성·외래키 검사 통과. 운영 DB 미교체. 원문 질문 제외·Git 전역 훅 미적용·신규 SDK 미설치 유지.
- 남은 위험: Java 표준 API와 명시 파일만의 컴파일러 증거다. 프로젝트 의존성/생성 코드·완전한 타입 표현·실제 런타임 디스패치·Java CFG/PDG/taint는 미지원이다. 전체 16개 언어 목표는 완료하지 않았다. 배포 전에 자체 Java 분석기를 별도 컴파일해야 하며 실행 호스트의 JDK 17 이상이 필요하다.
- 다음 조치: 다른 언어의 설치된 의미 분석기와 정답집을 확대하고 프로젝트 맥락·함수 간 데이터/제어 및 taint 근거를 계속 구현한다.

## 2026-09-29 - P2 Python 컴파일러 변수·제어 근거

- 변경 파일: `src/SMSR.App/GraphRuntime/{python_compiler,python_bindings,python_control,test_python_compiler,test_python_control,test_python_boundaries,symbols}.py`, `src/SMSR.App/Mvp/{GraphAnalysis,GraphPythonPage,GraphPythonSelfCheck,GraphPythonStorageSelfCheck,GraphAdvancedPage,GraphAdvancedRendering,GraphAdvancedTools,StdioGraphAdvancedTools,GraphAdvancedSelfCheck}.cs`, `samples/graph-python`, P2 보고서·계획서 MD/HTML·사용 가이드.
- 변경 사유: 16개 대상 중 Python의 기존 구문 분석을 실제 컴파일러 증거로 보강했다. Ponytail 기준에 따라 기존 CPython 3.12 표준 라이브러리와 실행·파일 분석·화면·SQLite 경로를 재사용했다. 새 SDK·의존성·DB·MCP 도구는 추가하지 않았다.
- 구현 내용: 코드를 실행/import하지 않고 컴파일된 코드 객체의 지역/클로저 슬롯·동적 네임스페이스 구분·물리 위치·명령·점프·예외 범위를 보존한다. 같은 줄 람다도 코드 객체 인덱스로 구분하며 generator/yield 이후는 일반 전이가 아닌 재개 근거로 표시한다. 상수/원문/진단 원문 미저장, 런타임3.12·원문2MB·500코드 범위·5만항목 제한. 기존 파일 분석 버전2와 저장 직전 원문/리비전 재검증을 추가했다.
- 실행 명령: Python `test_python_compiler.py`, `test_language_coverage.py`, `test_runtime.py`, `test_java_bundle.py`; `dotnet build src/SMSR.App -c Release --no-restore`; 소스 앱 `--graph-advanced-self-test`; `dotnet publish src/SMSR.App -c Release --no-restore -o artifacts/live-p2-python`; 게시 Python 및 C# `--self-test`; 게시 앱 고급/그래프/LSP/추적/설정/전체 자체검사; `scripts/test-mcp-stdio.ps1`; `scripts/verify-graph-backup.py`; 실제 색인·브라우저 분석·HTTP 저장 조회; 상태표 구조·중복 행·`git diff --check`.
- 검증 결과: 빌드 경고/오류0, Python/Java/C# 정답집과 기존16개 구문·Cypher/휴리스틱 회귀 통과. 소스 고급 검사·게시 앱6종 종료0, stdio42도구. 지역/클로저/nonlocal/클래스/람다·분기/반복/예외/generator/await·원문 보호·상한 검증. HTTP/MCP/DB에서 클로저 ID 일치·구버전 갱신·원문 변경 거부·삭제 정리 통과.
- 실제 적용: 확인한 기존 Java 게시 프로세스만 교체했다. `artifacts/live-p2-python/SMSR.App.exe`, 127.0.0.1:49783. 리비전24(483파일/1096노드/662관계), 코드 범위6개, `choose`21명령/22전이, `protected`예외 범위3개. 외부 클로저 저장소와 읽기/쓰기 ID가 일치하며 저장 결과 `stale=false`·브라우저 오류0이다.
- 검증 중 조정: 존재하지 않는 `languages.py` 경로를 먼저 조회했으나 실제 목록의 `symbols.py`로 확인했다. 문서 삽입 중 중복 제목/행을 발견해 해당 전체 행 기준으로 수정하고 상태표20행/3열과 성능 행1개를 검사했다. 앞으로 상태표 편집은 인접 완성 행 전체를 문맥으로 사용한다. 소스 정답집 실패는 없었다.
- 데이터 보호: `verification-backups/20260928-164257-446950` 별도 복원에서 전체 테이블 행 수·무결성·외래키 검사 통과. 운영 DB 미교체, 원문 질문 제외·전역 Git 훅 미적용·신규 SDK 미설치 유지.
- 남은 위험: 컴파일러 저장소/인코딩된 제어 근거이지 실제 값·실행 경로·호출 대상·안전성 인증이 아니다. 스택 값/별칭/힙·함수 간 PDG/taint와 다른 Python 버전, 전체16개 언어 정밀 분석의 최종 수용은 미완료다.
- 다음 조치: Python 컴파일러 근거에 스택/도달 정의·함수 경계 전파를 연결하고, 나머지 언어의 실제 의미 분석기와 프로젝트 맥락을 계속 구현·검증한다.

## 2026-09-29 - P2 Python 슬롯 도달 정의

- 변경 파일: `src/SMSR.App/GraphRuntime/{python_reaching,python_definition_scope,python_definitions,python_compiler,test_python_definitions,test_python_definition_boundaries,test_python_reaching,test_python_compiler}.py`, `src/SMSR.App/Mvp/{GraphAnalysis,GraphPythonDefinitionsPage,GraphPythonDefinitionsSelfCheck,GraphPythonPage,GraphPythonSelfCheck,GraphPythonStorageSelfCheck,GraphAdvancedPage,GraphAdvancedTools,StdioGraphAdvancedTools}.cs`, Python 예제, 계획서 MD/HTML·P2 보고서·가이드.
- 변경 사유: 변수 저장소와 제어 전이만으로는 읽기에서 어느 입력/할당을 참조하는지 알 수 없다. 기존 CPython 모델에 슬롯별 GEN/KILL 고정점을 추가했다. 새 SDK·DB·도구를 도입하지 않고 기존 저장/조회/화면 구조를 재사용했다.
- 구현 내용: 입력·할당·삭제/입구 미할당→읽기 후보, 분기/반복 합류, 예외 명령 전후 후보 합집합, 재개·공유 셀 경계, 스택 복원/동적 네임스페이스 분석 불가. 파일 분석 버전3·25만 계산 작업·5만 출력 항목 한도. 원문/상수/진단 원문은 제외한다.
- 실행 명령: Python `test_python_compiler.py`, `test_language_coverage.py`, `test_runtime.py`, `test_java_bundle.py`; `dotnet build src/SMSR.App -c Release --no-restore`; 소스 고급 자체검사; `dotnet publish src/SMSR.App -c Release --no-restore -o artifacts/live-p2-python-definitions`; 게시 Python/C# 정답집·앱 고급/그래프/LSP/추적/설정/전체 자체검사·stdio 검사; 실제 색인/브라우저/HTTP 저장 조회; 백업 복원·상태표 구조·`git diff --check`.
- 검증 결과: 빌드 경고/오류0, Python/Java/C# 및 기존16개 구문·런타임 회귀 통과. 독립 단일 정의 경로 탐색과 소형 그래프500개 대조. 소스 고급 및 게시 앱6종 종료0, stdio42. HTTP/MCP/DB 입력/할당→읽기2간선·동일 슬롯ID·버전없음/2 갱신·원문 변경/삭제 검증 통과.
- 실패와 수정: 정답집이 소스 반환 한 곳을 읽기 명령 하나로 가정해 실패했다. 실제 CPython이 반환을 두 분기 끝으로 복제한 것을 확인했다. 구현의 명령별 ID는 유지하고 테스트만 같은 소스의 복수 명령을 대조하도록 수정했다. HTML은 이전 실수를 반복하지 않도록 완성 행 전체를 문맥으로 삽입했고 상태표21행/3열·중복 없는 새 행을 확인했다.
- 실제 적용: 확인한 이전 프로세스만 교체한 `artifacts/live-p2-python-definitions/SMSR.App.exe`, 127.0.0.1:49783. 리비전25(491파일/1108노드/666관계), 코드 범위7개. `choose` 읽기5/연결7, `maybe` 읽기2/연결3/미할당 후보1을 화면과 DB에서 확인했다. 기존 버전2 stale 및 재분석 버전3 `stale=false`, 브라우저 오류0.
- 데이터 보호: `verification-backups/20260928-165537-801928` 별도 복원에서 전체 테이블 행 수·무결성·외래키 검사 통과. 운영 DB 미교체, 원문 질문 제외·전역 Git 훅 미적용·신규 SDK 미설치 유지.
- 남은 위험: 슬롯 정의 위치의 모델 후보이며 실제 값/실행 경로·오류/안전성 인증이 아니다. 공유 셀 변경·스택 복원·힙/별칭·외부 프레임 조작·함수 간 값 전파/taint와 다른 언어 의미 모델·전체 P2 수용은 미완료다.
- 다음 조치: 슬롯 근거와 스택 연산/호출 입출력 값을 연결하고, 다른 언어의 실제 분석기 및 프로젝트 맥락 구현을 계속한다.

## 2026-09-29 - P2 Python 표현식 값 전달

- 변경 파일: `src/SMSR.App/GraphRuntime/{python_value_graph,python_value_transfer,python_values,python_compiler,test_python_values,test_python_value_boundaries,test_python_compiler}.py`, `src/SMSR.App/Mvp/{GraphAnalysis,GraphPythonValuesPage,GraphPythonValuesSelfCheck,GraphPythonPage,GraphPythonSelfCheck,GraphPythonStorageSelfCheck,GraphAdvancedPage,GraphAdvancedTools,StdioGraphAdvancedTools}.cs`, Python 예제/README, 계획서 MD/HTML·P2 보고서·가이드.
- 변경 사유: 슬롯 할당 위치만으로 우변 연산과 반환/호출 인자의 입력을 추적할 수 없어 기존 컴파일러·도달 정의에 스택 생산자 후보를 연결했다. 기존 저장/화면/프로세스 보호 경로를 재사용하며 새 SDK·DB·MCP 도구는 추가하지 않았다.
- 구현 내용: 스택 위치별 집합 고정점, 분기·단락·while 합류, 연산 피연산자·할당·반환·위치/이름 호출 인자 연결, 미해석 호출 결과 분리. 표준 명령 스택 증감 대조. 예외/중단/공유셀/복원/미지원 명령은 값 그래프 불가 사유를 명시한다. 분석 버전4, 기존 5만 출력·25만 계산·시간/출력 상한 공유. 상수 값·이름 인자 이름·원문 미저장.
- 실행 명령: 설치된 graph-runtime의 `test_python_compiler.py`, `test_language_coverage.py`, `test_runtime.py`, 게시 Java `test_java_bundle.py`; `dotnet build src/SMSR.App/SMSR.App.csproj -c Debug --no-restore`; 소스 `--graph-advanced-self-test`; `dotnet publish src/SMSR.App/SMSR.App.csproj -c Release --no-restore -o artifacts/live-p2-python-values`; 게시 C# `--self-test`, 앱 고급/그래프/LSP/추적/설정/전체 자체검사; `scripts/test-mcp-stdio.ps1`; 실제 색인·화면·HTTP·SQLite 검증; `scripts/verify-graph-backup.py`; 상태표 구조 검사; `git diff --check`.
- 검증 결과: 빌드 오류/경고0, Python·기존16문법·런타임·Java/C# 통과. 소스 고급 검사와 게시 앱6종 종료0, stdio42. 실제 HTTP/MCP/DB에서 7노드·6간선, 입력→반환 5간선 경로, 공유셀 제외, 버전 없음/3 stale·원문 변경 거부·프로젝트 삭제 검사 통과.
- 실패와 수정: 생성기에도 CPython이 암묵 예외 영역을 생성해 초기 사유 검사1회 실패했다. 기존 정의의 분석 불가 사유를 보존하고 중단 사유를 예외보다 우선하도록 수정했다. 추적 서버가 지정 opaque ID를 읽기 쉬운 ID로 변환해 첫 이벤트를 거부했고 반환된 workflowId로 기록을 연결했다. UI 키보드 스크롤이 정지한 것은 내부 영역의 스크롤 동작으로 해결했다.
- 실제 적용: 이전 PID21412의 실행 경로와 소유 포트를 확인한 뒤 `artifacts/live-p2-python-values/SMSR.App.exe` PID45792로 교체했다. 127.0.0.1:49783만 사용. 리비전26(498파일/1120노드/671관계), `publish` 15노드·11간선, 입력→호출 인자2곳 및 호출 반환 경계 분리 확인. 버전4 `stale=false`, 브라우저 오류0, 화면 증거 `smsr-p2-python-values.png` 저장. 상태표22행 모두3열 정상.
- 데이터 보호: `verification-backups/20260928-171641-213033`의 별도 복원에서 전체 행 수·무결성·외래키 검사 통과. 운영 DB 미교체, 원문 질문 제외·전역 Git 훅 미적용·신규 SDK 미설치 유지.
- 남은 위험: 경로 조건을 풀지 않는 보수적 후보다. 연산자 오버로드·힙/별칭/부수 효과·예외 스택·중단/공유셀·함수 대상과 함수 간 요약/정밀 taint, 다른 언어 실제 분석기·전체 P2 수용은 미완료다. 코드 파일은 작은 모듈로 분리했으며 기존 compiler 진입 파일만 목표3000자를 소폭 초과한다.
- 다음 조치: Python 미지원 명령/예외·함수 간 입력/반환 연결과 나머지 언어 의미 모델을 이어서 구현·검증한다. 전체16개 언어 목표를 축소하지 않는다.

## 2026-09-29 - P2 Python 반복·메서드 프로토콜

- 변경 파일: `src/SMSR.App/GraphRuntime/{python_value_flow,python_value_protocols,python_value_solver,python_value_transfer,python_values,test_python_protocols,test_python_iteration,test_python_value_boundaries,test_python_compiler}.py`, `src/SMSR.App/Mvp/{GraphAnalysis,GraphPythonProtocolsSelfCheck,GraphPythonSelfCheck,GraphPythonStorageSelfCheck,GraphPythonValuesPage,GraphAdvancedTools,StdioGraphAdvancedTools}.cs`, Python 예제/README, 계획서 MD/HTML·P2 보고서·추적 가이드.
- 변경 사유: 일반 for와 속성/메서드 호출 때문에 함수 전체 값 근거가 끊기던 범위를 기존 CPython 값 모델로 확장했다. 기존 출력/계산/프로세스 상한, 저장 경로와 UI 표를 재사용하고 새 SDK/라이브러리/MCP 도구를 추가하지 않았다.
- 구현 내용: 분기별 반복 스택, 정상 종료 시 END_FOR 우회·정리, 반복 요소/고정 분해 순번, 속성 조회·정규화 수신 객체 후보·명시적 인자 분리. 논리 스택은 런타임의 descriptor/self 확정을 뜻하지 않는다. 파일 분석 버전5, 값 분석용 정상 전이 및 건너뛴 위치 표시. 호출 내부·힙/별칭·프로토콜 값은 여전히 미해석이다.
- 실행 명령: Python `test_python_compiler.py`, `test_language_coverage.py`, `test_runtime.py`, 게시 Java `test_java_bundle.py`; `dotnet build src/SMSR.App/SMSR.App.csproj -c Debug --no-restore`; 소스 `--graph-advanced-self-test`; `dotnet publish src/SMSR.App/SMSR.App.csproj -c Release --no-restore -o artifacts/live-p2-python-protocols`; 게시 C# `--self-test`; 게시 앱 고급/그래프/LSP/추적/설정/전체 자체검사; `scripts/test-mcp-stdio.ps1`; 실제 색인·브라우저·HTTP·SQLite 확인; `scripts/verify-graph-backup.py`; HTML 상태표·`git diff --check`.
- 검증 결과: 빌드 오류/경고0. CPython 고정 fixture의 실제 명령 추적으로 빈 반복·정상 종료·break·continue 전이 대조 통과. 중첩 반복·조기 반환·미할당·분해 순번·속성/연속/저장된 메서드·이름 인자·수신/인자 격리·미해석 반환 분리·결정적 출력·마커 미노출 검사 통과. 대상 저장소 코드는 실행하지 않았다.
- 통합 검증: 소스 고급 및 게시 앱6종 종료0, Python·기존16문법·Cypher/휴리스틱·Java/C# 및 stdio42 통과. 실제 HTTP/MCP/DB에서 입력/수신 객체 경로 분리와 반복 종료·버전 없음/2/3/4 stale·원문 변경 거부·삭제 정리를 검증했다. 기존 미지원 기대값 검사1회 실패는 새 지원 양성 정답집으로 옮기고 나머지 음성 경계를 유지했다.
- 실제 적용: 이전 PID45792의 경로/포트 소유를 확인해 PID56944 `artifacts/live-p2-python-protocols/SMSR.App.exe`로 교체했다. 127.0.0.1:49783, 리비전27(504파일/1130노드/675관계). `walk` 18노드14간선, 종료6→50/건너뛴48, 다음 요소6→10. 버전5 `stale=false`, 브라우저 오류0, 화면 증거 `smsr-p2-python-protocols.png`. 상태표23행 모두3열 정상. 새 코드/테스트 파일은 모두3000자 이내다.
- 데이터 보호: `verification-backups/20260928-173146-772441` 별도 복원에서 전체 행 수·무결성·외래키 통과. 원문/상수 값·이름 인자 문자열 미저장, 원문 질문 제외·전역 Git 훅 미적용·SQLite 원본·신규 SDK 미설치 유지.
- 남은 위험: 경로 조건·힙/별칭·descriptor/실제 self·프로토콜/함수 내부·예외 스택·생성기·공유셀·가변 분해/인자 펼침과 나머지 언어 실제 의미 분석·전체 P2 수용이 남아 있다. 후보를 실제 실행·안전성 또는 정밀 taint로 승격하지 않는다.
- 다음 조치: Python 예외/호출 경계와 함수 간 연결 및 다른 언어 의미 분석기를 계속 확장하고, 16개 언어 전체 요구를 별도 정답집으로 검증한다.

## 2026-09-29 - P2 JavaScript·TypeScript·TSX 컴파일러 분석

- 변경 파일: `GraphRuntime/typescript_{runtime,bundle}.py`, `typescript_{main,host,symbols,calls,facts}.cjs`, `test_typescript_{bundle,boundaries}.py`, `worker.py`; `Mvp/GraphTypeScript{Request,Analysis,Tools,Page,SelfCheck,StorageSelfCheck}.cs`, 고급 엔드포인트/화면/렌더러/자체검사·MCP 게이트웨이, 앱 프로젝트, stdio 검사, `samples/graph-typescript/`, 계획서 MD/HTML·추적 가이드·P2 보고서.
- 변경 사유: 기존 구문 후보에서 실제 타입 검사기의 선언·참조·호출 시그니처 근거로 JS/TS/TSX를 확장한다. Ponytail 지침에 따라 설치 Node/VS Code 컴파일러와 기존 Java 묶음의 저장/조회 패턴을 재사용했다. Graphify 재구축이나 새 SDK/라이브러리 설치는 하지 않았다.
- 구현: 메모리 입력·상대 import·표준 타입만 읽는 호스트, noEmit, 설정/패키지/플러그인/대상 코드 미실행, Node preload/coverage/cache 환경 제거. 정적 오버로드·제네릭·JSDoc·TSX 시그니처, 심벌/alias·스코프/shorthand 참조, 물리 UTF16 위치. 원문·리터럴 값·진단 메시지 미저장. rest/spread/JSX 대응·실제 호출 대상은 미해석으로 명시.
- 실행 명령: `test_typescript_bundle.py`, `test_typescript_boundaries.py`, `test_language_coverage.py`, `test_python_compiler.py`, `test_runtime.py`; 게시 Java `test_java_bundle.py`; `dotnet build src/SMSR.App/SMSR.App.csproj --no-restore`; 소스 `--graph-advanced-self-test`; `dotnet publish src/SMSR.App/SMSR.App.csproj -c Release --no-restore -o artifacts/live-p2-typescript`; 게시 C# `--self-test`; 게시 앱 고급/그래프/LSP/추적/설정/전체 자체검사; `scripts/test-mcp-stdio.ps1`; `scripts/verify-graph-backup.py`; HTML 구조·`git diff --check` 및 실제 화면/HTTP/DB 확인.
- 검증 결과: 빌드 오류/경고0. TS/JS/TSX 양성·경계 정답집, 기존16문법·Python·Java/C#·Cypher/휴리스틱 회귀 통과. 소스 고급 및 게시 앱6종 종료0, stdio44. 통합에서 Origin 보호·실제 MCP·SQLite 원문 제외·순서 무관 키·버전/원문 stale·잘못된 경로/오래된 원문 거부·삭제 연동 검증.
- 실제 적용: 기존 PID56944의 정확한 경로·포트 소유 확인 후 PID44572 `artifacts/live-p2-typescript/SMSR.App.exe`로 교체. 127.0.0.1:49783, 리비전28(519파일/1155노드/685관계). 예제4파일 심벌19·참조33·호출4, 진단0·버전1·`stale=false`. 화면 증거 `smsr-p2-typescript.png`, 브라우저 오류0. 상태표24행 모두3열.
- 데이터 보호: `verification-backups/20260928-175324-926437` 스냅샷을 별도 파일로 복원해 모든 테이블 행 수·무결성·외래키 통과. 운영 DB 미교체. 원문 질문 제외·전역 Git 훅 미적용·SQLite 원본 유지.
- 실패/수정: 불완전한 문맥의 패치2건은 적용 전 거부돼 정확한 기존 줄로 수정했다. 계획 HTML의 중복 성능 행을 제거하고 전체 상태표 구조를 검사했다. 코드 실행·회귀 검사 실패는 없었다.
- 남은 위험: 설치 TypeScript6.0만 지원하며 컴파일러 업데이트 시 재분석이 필요하다. 실제 프로젝트 설정/외부 의존성·동적 호출·heap/alias·rest/spread/JSX 전개·전체16개 언어의 정밀 의미/함수 간 PDG/taint 및 최종 P2 수용은 미완료다. 타입 요약/진단 상한은 결과에 표시한다. 새 구현 파일은3000자 이내이며 저장 통합 검사1개는 명확한 검증을 유지해3233자다.
- 다음 조치: Java/JS/TS/Python의 함수 간 제어·값 모델과 나머지 언어의 실제 분석기/정답집을 계속 확장한다. 16개 전체 요구를 줄이지 않으며 추가 런타임 설치는 승인된 범위가 정해진 뒤 진행한다.

## 2026-09-29 - P2 JS/TS 호출별 본문 입출력 연결

- 변경 파일: `GraphRuntime/typescript_{functions,connections,calls,facts}.cjs`, `test_typescript_connections.py`; `Mvp/GraphTypeScript{Analysis,SelfCheck,StorageSelfCheck,ConnectionSelfCheck,Page,ConnectionsPage}.cs`, `GraphAdvancedPage.cs`; TypeScript 예제 README·P2 보고서·추적 가이드·계획서 MD/HTML.
- 변경 사유: 정적 시그니처만 있던 호출을 입력 묶음의 구현 본문·매개변수·명시 반환 위치와 연결한다. Ponytail/Karpathy 지침에 따라 기존 컴파일러 API·위치 ID·저장·UI 표와 제한을 재사용했다. 새 의존성/런타임/도구는 추가하지 않았다.
- 구현: 오버로드의 선택 선언/구현 본문 분리, 호출별 입력/반환 후보, 중첩 함수 반환 격리, 화살표 식 본문, 명시 this 매개변수 제외, 기본값의 undefined 조건. alias 사용은 선언으로 잘못 표시하지 않고 `ALIAS_REFERENCE`로 구분했다. 비동기/생성기·생성자/TSX·rest/spread·구조분해·외부/모호한 본문·컴파일 오류는 연결 보류 사유를 남긴다. 분석 버전2, 구버전 stale.
- 실행 명령: Python `test_typescript_connections.py`, `test_typescript_bundle.py`, `test_typescript_boundaries.py`, `test_language_coverage.py`, `test_python_compiler.py`, `test_runtime.py`; 게시 Java `test_java_bundle.py`, C# `--self-test`; `dotnet build src/SMSR.App/SMSR.App.csproj --no-restore`; 소스 고급 자체검사; `dotnet publish src/SMSR.App/SMSR.App.csproj -c Release --no-restore -o artifacts/live-p2-typescript-connections`; 게시 앱6종 자체검사·`scripts/test-mcp-stdio.ps1`; 실제 화면/HTTP/DB·`scripts/verify-graph-backup.py`·계획 HTML 구조·`git diff --check`.
- 검증 결과: 빌드 오류/경고0, 소스 고급/게시 앱6종 종료0, stdio44, 전체 언어별 회귀 통과. 오버로드 구현과 서로 다른 call ID의 반환 목적지, 재귀/화살표/중첩·this·기본값 조건·지원 불가 경계를 검증했다. HTTP/MCP/DB에서 본문/매개변수/반환/call ID 보존, 버전1 stale·원문 변경 거부·삭제 정리가 통과했다.
- 실제 적용: PID44572의 정확한 경로·포트 소유 확인 후 PID51360 `artifacts/live-p2-typescript-connections/SMSR.App.exe`로 교체. 127.0.0.1:49783, 리비전29(522파일/1161노드/688관계). 예제4본문/4호출, 연결3/입력3/반환3/TSX 제외1, 버전2 `stale=false`. 실제 화면 상세표와 제외 사유, 브라우저 오류0 확인. 화면 증거 `smsr-p2-typescript-connections.png`, 상태표25행 모두3열.
- 데이터 보호: `verification-backups/20260928-180334-002151` 별도 복원에서 모든 테이블 행 수·무결성·외래키 통과. 운영 DB 미교체, 원문/리터럴/진단 원문 및 원문 질문 미저장, 전역 Git 훅 미적용·신규 SDK 미설치 유지.
- 남은 위험: 정적 구현 후보가 실제 호출 대상이라는 보장은 없다. receiver/heap/alias·finally·반환 도달성·암묵 반환·함수 내부 값 의존과 전체16개 언어 정밀 PDG/taint·최종 P2 수용은 미완료다. 인수에서 반환으로 바로가기 간선을 추측하지 않는다. 새 코드/테스트 파일은3000자 이내이며 기존 저장 통합 검사의 예외는 유지한다.
- 다음 조치: 함수 내부 제어/값 모델과 호출별 본문 연결을 결합하고, 미지원 언어의 실제 의미 분석기를 이어서 확장한다. 전체16언어 목표를 축소하지 않는다.

## 2026-09-29 - P2 JS/TS 함수 내부 값 후보

- 재계획: 여러 증분에 걸쳐 부분 문맥만 지정한 패치가 같은 이유로 세 번 거부됐다. 해당 방식의 재시도를 멈추고 대상 원문을 다시 읽었다. 이후 패치는 현재 파일의 완전한 줄을 기준으로 파일별 변경을 적용하고 검증한다. 거부된 묶음에서는 파일 변경이 없었음을 확인했다.
- 초기 검증 수정: hoist 정답집의 `var x`가 TypeScript에서 매개변수의 number와 다른 선언 타입으로 진단됐다. 같은 타입을 명시한 정상 fixture로 수정했고 값 도달 검사와 독립 분기 열거 검증이 통과했다.
- 변경 파일: `GraphRuntime/typescript_value_{state,graph,expressions,calls,statements}.cjs`, `typescript_values.cjs`, `typescript_{facts,connections}.cjs`, `test_typescript_{values,value_boundaries,value_oracle}.py`; `Mvp/GraphTypeScript{Analysis,SelfCheck,StorageSelfCheck,ValuesSelfCheck,Page,ValuesPage}.cs`, `GraphAdvancedPage.cs`; TypeScript 예제·README·추적 가이드·P2 보고서·계획서 MD/HTML.
- 변경 사유: 호출별 본문 근거 안에서 매개변수/지역 변수의 대입·분기·while 도달 정의와 값 의존 후보를 계산한다. 기존 컴파일러 심벌 ID·물리 위치·프로세스/출력 한도·SQLite 저장/화면 패턴을 재사용했다. 새 SDK/라이브러리/MCP 도구는 추가하지 않았다.
- 구현: 대입 GEN/KILL, if·단락 합류, while 고정점, 조기 반환, var hoist·블록 스코프, 입력/읽기/연산/할당/반환/호출 인자. 미해석 호출 반환은 입력과 분리한다. 호출별 연결에 실제 존재하는 인수/매개변수/반환 값 노드 ID를 기록한다. 미지원 구문/컴파일 오류는 함수 전체 빈 값 그래프와 사유로 반환하고 자원 한도 실패는 보고서 전체를 저장하지 않는다.
- 실행 명령: TS 값·경계·독립 분기 정답집 및 기존 TS 묶음/보호/본문 연결, `test_language_coverage.py`, `test_python_compiler.py`, `test_runtime.py`, 게시 Java `test_java_bundle.py`, C# `--self-test`; `dotnet build src/SMSR.App/SMSR.App.csproj --no-restore`; 소스 고급 자체검사; `dotnet publish src/SMSR.App/SMSR.App.csproj -c Release --no-restore -o artifacts/live-p2-typescript-values`; 게시 앱 고급/그래프/LSP/추적/설정/전체6종, `scripts/test-mcp-stdio.ps1`; 실제 화면/HTTP/SQLite·`scripts/verify-graph-backup.py`, HTML 상태표·`git diff --check`.
- 검증 결과: 빌드 오류/경고0, 소스 고급·게시 앱6종 종료0, stdio44, 새 TS6종·기존16문법·Python/Java/C#/Cypher 회귀 통과. 대상 코드 실행 없이32함수×32독립 분기 조합과 데이터 도달을 대조했다. 과다 대입 입력도 부분 성공 없이 실패했다. 통합에서 입력→읽기→반환3노드/2간선과 호출별 값 ID, 버전2 stale·원문 변경 거부·삭제 검증 통과.
- 실제 적용: 이전 PID51360의 경로/포트 소유 확인 후 PID36528 `artifacts/live-p2-typescript-values/SMSR.App.exe`로 교체. 127.0.0.1:49783, 리비전30(527파일/1170노드/692관계). 예제 함수7/호출7, 값 분석6/객체 리터럴 불가1. overwrite5노드3간선(입력→반환 없음), choose10노드8간선·repeat12노드11간선(도달 있음)을 DB에서 검증하고 화면 간선 표로 확인했다. 버전3 최신, 브라우저 오류0, 화면 증거 `smsr-p2-typescript-values.png`, 상태표26행 모두3열.
- 데이터 보호: `verification-backups/20260928-181928-014458` 별도 복원에서 전체 행 수·무결성·외래키 통과. 운영 DB 미교체, 원문/상수 값/진단 원문·원문 질문 미저장, 전역 Git 훅 미적용·신규 SDK 미설치 유지.
- 추가 문서 수정: 문맥을 옮겨 적는 과정의 오타로 상태표 패치1회가 다시 거부됐다. 원문 줄을 읽어 그대로 패치 문맥으로 사용하는 방식으로 변경해 적용하고 MD/HTML 상태표를 확인했다.
- 남은 위험: 정상 흐름 MAY 모델이다. 예외/finally·for/break/continue·중첩 함수/클래스·프로퍼티·동적 호출/eval/arguments·증감/복합 대입·기본/가변/구조분해 매개변수·중단 프로토콜은 잔여다. 제어 근거는 구문 조건이며 완전한 제어 의존/실행 가능 경로/heap/alias/연산자 효과/함수 간 요약/정밀 taint가 아니다. 전체16언어 정밀 의미·최종 P2 수용은 미완료다. 새 값 모듈/테스트는3000자 이내, 기존 facts 진입3016자·저장 통합 검사3233자는 명확한 기존 구조를 유지했다.
- 다음 조치: 미지원 제어 구문과 호출별 함수 간 값 요약을 확장하고, 나머지 언어의 실제 의미 분석/정답집을 계속 진행한다. 전체16언어 요구를 유지한다.

## 2026-09-29 - P2 JS/TS 반복 전이·갱신 값

- 변경 파일: `GraphRuntime/typescript_value_{loops,updates,statements,expressions}.cjs`, `test_typescript_{loops,value_boundaries}.py`; `Mvp/GraphTypeScript{Analysis,SelfCheck,StorageSelfCheck,ConnectionSelfCheck,LoopsSelfCheck,ValuesPage}.cs`; TypeScript 예제/README, 추적 가이드, P2 보고서, 계획서 MD/HTML.
- 변경 사유: 기존 정의 집합·고정점·심벌 해소를 재사용해 for/do, 중첩 반복 break/continue 및 증감/복합 대입을 확장했다. continue 이후 갱신식, break의 갱신식 생략, do의 최소1회 본문, 전위/후위 반환 값을 구분한다. 새 의존성·SDK 설치 없음.
- 실행 명령: `dotnet build src/SMSR.App/SMSR.App.csproj --no-restore -v minimal`; `dotnet publish src/SMSR.App/SMSR.App.csproj -c Release --no-restore -o artifacts/live-p2-typescript-loops`; TS7종 및 `test_language_coverage.py`, `test_python_compiler.py`, `test_runtime.py`; 게시 Java/C# 자체검사; 게시 앱 고급/그래프/LSP/추적/설정/전체6종, `scripts/test-mcp-stdio.ps1`, `scripts/verify-graph-backup.py`, HTTP/SQLite/브라우저 검사, `git diff --check`.
- 검증 결과: 빌드 경고/오류0. 12개 반복·갱신 정답집과 기존32함수×32분기 조합, 미지원 경계 및 TS7종 통과. 16언어 문법·Python/Java/C#/Cypher 회귀 통과. 게시 앱6종 종료0·stdio44. HTTP/MCP/DB continue/break 경로 및 버전3 stale 통과.
- 실제 적용: 경로/리스너 소유를 확인한 PID36528만 종료하고 PID59132 `artifacts/live-p2-typescript-loops/SMSR.App.exe`를 숨김 실행. 127.0.0.1:49783 유지. 리비전31(529파일/1174노드/694관계), 예제8함수/8호출, counted18노드17간선과 입력→반환 후보 확인. 버전4 최신·브라우저 오류0, 화면 증거 `smsr-p2-typescript-loops.png`.
- 데이터 보호: `verification-backups/20260928-183341-274034`의 별도 복원에서 무결성·행 수·외래키 검사 통과. 운영 DB를 복원본으로 교체하지 않았다. 원문 질문 제외, 전역 Git 훅 미적용 유지.
- 남은 위험: 정상 흐름의 보수적 후보다. 상수 조건 만족성·라벨·for-in/of·switch·예외/finally·힙/별칭·연산자 효과·함수 간 값 요약과 전체16언어 정밀 의미/PDG/taint는 미완료다. 기존 통합 검사 파일의3000자 초과는 구조 유지를 위한 예외이며 새 모듈은 작게 분리했다.
- 다음 조치: 호출별 본문 연결과 값 그래프의 함수 간 요약을 검증 가능한 범위로 결합하고, 전체16언어의 미지원 의미 분석을 계속 확장한다.

## 2026-09-29 - P2 JS/TS 호출별 반환 데이터 요약

- 변경 파일: `GraphRuntime/typescript_{summary_state,summaries,facts}.cjs`, `test_typescript_summaries.py`; `Mvp/GraphTypeScript{Analysis,SelfCheck,StorageSelfCheck,ConnectionSelfCheck,SummarySelfCheck,Page,SummaryPage}.cs`, `GraphAdvancedPage.cs`; TypeScript 예제/README·추적 가이드·P2 보고서·계획서 MD/HTML.
- 변경 사유: 기존 로컬 값 그래프와 호출별 본문/매개변수 대응을 재사용해 반환 데이터 의존을 계산한다. 호출별 파생 간선은 원래 값 그래프와 분리한다. 새 의존성·SDK·도구 추가 없음.
- 구현: 매개변수/미해석 값 레이블을 로컬 데이터 간선과 호출별 입력 대응을 통해 단조 고정점으로 전파한다. 직접/상호 재귀와 다단계 호출을 처리하고, 서로 다른 호출의 결과 노드를 연결하지 않는다. 외부 호출·미지원 본문·외부 읽기는 미해석 ID로 유지한다. 계산/출력 상한을 공유하고 초과 시 저장하지 않는다.
- 실행 명령: TS 요약/반복/값/독립 분기/경계/묶음/보호/본문 연결8종, `test_language_coverage.py`, `test_python_compiler.py`, `test_runtime.py`, 게시 Java/C# 자체검사; `dotnet build src/SMSR.App/SMSR.App.csproj --no-restore -v minimal`, `dotnet publish src/SMSR.App/SMSR.App.csproj -c Release --no-restore -o artifacts/live-p2-typescript-summaries`; 게시 앱6종·`scripts/test-mcp-stdio.ps1`·`scripts/verify-graph-backup.py`, 실제 HTTP/SQLite/브라우저·HTML 표·`git diff --check`.
- 검증 결과: 빌드 경고/오류0, 15함수 독립 기대값(다단계·순서 변경·직접/상호 재귀·격리·덮어쓰기·미해석/미사용/암묵 반환·미지원 본문) 통과. TS8종·16문법·Python/Java/C#/Cypher 회귀, 게시 앱6종 종료0, stdio44. HTTP/MCP/SQLite에서 별도 파일 wrapper의 입력0→반환 및 호출 내부 간선, 버전4 stale 확인.
- 실제 적용: 소유 경로/포트 확인 후 PID59132만 종료하고 PID58028 `artifacts/live-p2-typescript-summaries/SMSR.App.exe` 숨김 실행. 리비전32(532파일/1180노드/697관계), 버전5 최신. 예제10함수 중 relayed는 영향 입력[0]/호출 간선1, erased는 빈 입력/간선0으로 DB와 화면에서 확인. 브라우저 오류0, 증거 `smsr-p2-typescript-summaries.png`, 상태표28행 모두3열.
- 데이터 보호: `verification-backups/20260928-184223-301208` 별도 복원 무결성/전체 행수/외래키 통과. 운영 DB 미교체, 원문 질문 제외·전역 Git 훅 미적용 유지.
- 남은 위험: 정적 본문 후보의 데이터 의존만 요약한다. 제어 의존·종료·조건 만족성·실제 동적 대상·예외·힙/별칭/부수효과는 포함하지 않는다. 빈 영향/미해석 목록은 안전성·완전성의 증거가 아니다. 반복 전체 스캔은 계산 상한이 있으며 실측 병목 시 worklist로 개선할 수 있다. 기존 진입/통합 파일3000자 초과는 구조 유지를 위한 예외다. 전체16언어 정밀 분석·PDG/taint와 최종 P2 수용은 미완료다.
- 다음 조치: 함수 간 미해석/부수효과 및 제어 의존 범위를 확장하고 다른 언어의 실제 의미 분석과 정답집을 계속 구현한다.

## 2026-09-29 - P2 Java 메서드 도달 정의·값 후보

- 변경 파일: `src/SMSR.JavaAnalysis/Value{Budget,State,Graph,Expressions,Updates,Statements,Loops}.java`, `MethodValues.java`, `Collector.java`, `Analyzer.java`; `GraphRuntime/test_java_{values,value_oracle}.py`; `Mvp/GraphJava{Analysis,SelfCheck,StorageSelfCheck,ValuesSelfCheck,Page,ValuesPage}.cs`, `GraphAdvancedPage.cs`; Java 예제/README·추적 가이드·P2 보고서·계획서 MD/HTML.
- 변경 사유: 기존 JDK17 javac 심벌·위치·선언/호출 결과와 프로세스 보호를 재사용해 Java 메서드 내부 값 근거를 추가한다. 새 SDK/의존성/MCP 도구 없음.
- 구현: 심벌별 대입 GEN/KILL, if·단락 합류, while/for/do 고정점, 가장 가까운 반복문의 break/continue, 조기 반환, 전위/후위 증감·복합 대입. 호출 입력/수신 객체는 경계에 기록하며 반환은 분리한다. 미지원 본문은 빈 그래프와 사유, 컴파일 오류는 전체 값 근거 무효화. 계산25만/통합 출력2만 항목 및 기존 JVM/시간/출력 상한 유지.
- 검증 중 발견/수정: 32메서드 독립 분기 정답집 첫 실행에서 `int x=a,y=b`의 두 선언이 같은 시작 위치로 보고되어 값이 합쳐졌다. 원시 javac 위치와 생성 노드를 대조해 원인을 확인하고 공통 ID에 끝 위치를 포함했다. 재컴파일 후 정답집이 통과했으며 재시도 반복/테스트 완화는 하지 않았다.
- 실행 명령: `scripts/build-java-analysis.ps1 -Javac C:/Program Files/Eclipse Adoptium/jdk-17.0.20.101-hotspot/bin/javac.exe`; Java 값/독립 분기/묶음3종; `dotnet build src/SMSR.App/SMSR.App.csproj --no-restore -v minimal`; `dotnet publish src/SMSR.App/SMSR.App.csproj -c Release --no-restore -o artifacts/live-p2-java-values`; 게시 앱6종·게시 Java3종·16문법/Python/Cypher/TS요약/C# 회귀·`scripts/test-mcp-stdio.ps1`; 실제 UI/HTTP/SQLite·`scripts/verify-graph-backup.py`·HTML 상태표·`git diff --check`.
- 검증 결과: 빌드 경고/오류0, Java18메서드 기대값 및32메서드×32독립 분기 경로 통과. 게시 앱6종 종료0, stdio44, 기존 언어 회귀 통과. HTTP/MCP/DB에서 입력 전달/제거·심벌 ID·버전1 stale·원문 변경 거부/삭제 검사 통과. 대상 Java 코드는 실행하지 않았다.
- 실제 적용: 경로/포트 소유 확인한 PID58028만 종료 후 PID44148 `artifacts/live-p2-java-values/SMSR.App.exe` 숨김 실행, 127.0.0.1:49783 유지. 리비전33(544파일/1195노드/700관계), Java 버전2 최신·9메서드. overwrite5노드3간선 입력→반환 없음, choose9노드8간선·counted16노드17간선 도달 있음을 DB와 화면에서 확인. 브라우저 오류0, 증거 `smsr-p2-java-values.png`, 상태표29행 모두3열.
- 데이터 보호: `verification-backups/20260928-185440-857101` 별도 복원에서 무결성·전체 행수·외래키 통과. 운영 DB 미교체, 원문 질문 제외·전역 Git 훅 미적용 유지.
- 남은 위험: 정상 흐름 MAY 후보이며 실제 조건 만족성·예외/finally·힙/배열/별칭·변환/부수효과·람다·함수 간 요약·완전한 제어 의존/PDG/taint는 미완료다. 새 Java 모듈은3000자 이하, 기존 앱 통합 파일의 초과는 구조 유지 예외다. 전체16언어 정밀 의미와 최종 P2 수용은 계속 잔여다.
- 다음 조치: Java 호출별 본문 입출력 및 함수 간 값 요약을 연결하고, 나머지 언어의 실제 의미 분석/정답집을 확장한다.

## 2026-09-29 - P2 Java 호출별 본문·반환 데이터 요약

- 변경 파일: `src/SMSR.JavaAnalysis/Method{SummaryState,Connections,Summaries}.java`, `SymbolFacts.java`, `ValueGraph.java`, `CallFacts.java`, `Collector.java`, `MethodValues.java`, `Analyzer.java`; `GraphRuntime/test_java_{summaries,summary_boundaries}.py`; `Mvp/GraphJava{Analysis,SelfCheck,StorageSelfCheck,SummarySelfCheck,Page,SummaryPage}.cs`, `GraphAdvancedPage.cs`; Java 예제/README·추적 가이드·P2 보고서·계획서 MD/HTML.
- 변경 사유: 기존 javac 심벌과 메서드 내부 값 근거를 재사용해 직접 호출의 입력·본문·반환을 연결한다. 새 SDK/의존성/MCP 도구 없이 분석 버전3으로 올리고 버전2 결과를 오래된 결과로 구분한다.
- 구현: 호출별 값 ID 격리, 정확한 컴파일러 선언 ID로 오버로드 본문 선택, 데이터 의존 고정점으로 다단계·직접/상호 재귀 요약, 미해석 값 전파. 호출 인자가 실제 본문의 반환에 영향을 줄 때만 호출 요약 간선을 만든다. 가상/가변 인수·미지원 본문·외부 본문은 사유를 보존하며 입력→결과 관계를 임의 생성하지 않는다. 기존 계산/출력/JVM/시간 상한 유지.
- 실행 명령: `scripts/build-java-analysis.ps1 -Javac C:/Program Files/Eclipse Adoptium/jdk-17.0.20.101-hotspot/bin/javac.exe`; Java 묶음/값/독립 분기/함수 간 요약4종; `dotnet build src/SMSR.App/SMSR.App.csproj --no-restore -v minimal`; `dotnet publish src/SMSR.App/SMSR.App.csproj -c Release --no-restore -o artifacts/live-p2-java-summaries`; 게시 앱6종·게시 Java4종·16언어/Python/TS/C#/Cypher 회귀·`scripts/test-mcp-stdio.ps1`; 실제 UI/HTTP/SQLite·`scripts/verify-graph-backup.py`·HTML 상태표·`git diff --check`.
- 검증 결과: 빌드 경고/오류0, 15메서드 기대값·호출 격리·재귀·오버로드·가상/가변 경계·컴파일 오류 무효화 통과. 게시 앱6종 종료0, stdio44, 기존 언어 회귀 통과. HTTP/MCP/DB에서 입력0→반환과 호출 내부 간선 및 버전2 stale 확인. 대상 Java 코드는 실행하지 않았다.
- 실제 적용: 경로/포트 소유 확인 후 기존 PID44148만 종료, PID56852 `artifacts/live-p2-java-summaries/SMSR.App.exe` 숨김 실행. 127.0.0.1:49783 유지, 리비전34(551파일/1205노드/703관계). 실제 Java 버전3·11메서드 저장, HTTP stale=false. relayed 입력[0]/호출 간선1, erased 빈 입력/간선0을 DB와 화면에서 확인. 브라우저 오류0, 증거 `smsr-p2-java-summaries.png`, 상태표30행 모두3열.
- 데이터 보호: 새 결과 저장 후 `verification-backups/20260928-190917-246575` 별도 복원 무결성·전체 행수·외래키 통과. 운영 DB 미교체, 원문 질문 제외·전역 Git 훅 미적용 유지.
- 남은 위험: 정적 본문의 데이터 의존 후보만 다룬다. 제어 의존·종료·조건 만족성·예외·힙/별칭/부수효과·변환 효과와 완전한 PDG/taint는 포함하지 않는다. 빈 영향/미해석 목록은 안전성 또는 완전성의 증거가 아니다. 제한된 전체 스캔 고정점은 실측 병목 시 worklist로 전환한다. 새 요약 모듈은3000자 이하, 기존 통합 파일 초과는 구조 유지 예외다. 전체16언어 정밀 의미와 최종 P2 수용은 미완료다.
- 다음 조치: 남은 언어의 실제 의미 분석과 독립 정답집, 함수 간 제어/부수효과 및 불확실성 범위를 계속 확장한다.

## 2026-09-29 - P2 Python 정상 경로 제어 의존

- 변경 파일: `GraphRuntime/control_postdominance.py`, `python_control_dependence.py`, `python_compiler.py`, `test_control_postdominance.py`, `test_python_control_dependence.py`, `test_python_compiler.py`; `Mvp/GraphPythonControl{Page,SelfCheck}.cs`, `GraphPython{Page,SelfCheck,StorageSelfCheck}.cs`, `GraphAdvancedPage.cs`, `GraphAnalysis.cs`; Python 예제/README·추적 가이드·P2 보고서·계획서 MD/HTML.
- 변경 사유: 기존 CPython 정상 전이·좌표·실행 보호와 C# 후지배 검증 방식을 재사용해 Python의 조건별 명령 제어 의존을 추가한다. 나머지 SDK는 PATH/Visual Studio 설치 경로/대표 경로에서 발견되지 않았으며 신규 설치는 하지 않았다. 없는 Graphify 그래프를 근거로 추론하지 않고 현재 소스를 확인했다.
- 구현: 도달 명령만 정규화하고 반환/명시적 raise를 합성 종료 -1에 연결한다. 비트셋 후지배 고정점·즉시 후지배·조건별 제어 의존을 계산한다. 예외/중단/종료 불가 영역은 구분된 사유와 빈 제어 의존을 반환한다. 기존 계산25만/출력5만 한도를 공유한다. 파일 분석 버전6·기존 HTTP/MCP/DB·명령/소스 위치 화면에 연결했다.
- 검증 중 발견/수정: 생성기에 컴파일러 합성 예외 범위도 있어 중단 사유를 우선 표시했다. 두 반환 합류 표본의 예상이 실패하여 실제 명령을 확인했다. CPython이 짧은 반환 구간을 분기별로 복제했으며 알고리즘 출력은 그 명령 흐름과 일치했다. 복제된 반환과 실제 공유 구간을 별도 표본으로 유지하고 독립 정답집으로 재검증했다. 기대값을 단순 삭제하지 않았다.
- 실행 명령: `dotnet build src/SMSR.App/SMSR.App.csproj --no-restore -v minimal`; `dotnet publish src/SMSR.App/SMSR.App.csproj -c Release --no-restore -o artifacts/live-p2-python-control -v minimal`; 게시 `test_python_compiler.py`·Java4종·C# 분석기 자체검사·16언어/TS요약/Cypher 회귀; 게시 앱6종·`scripts/test-mcp-stdio.ps1`; 실제 HTTP/SQLite/UI·`scripts/verify-graph-backup.py`·계획서 표·`git diff --check`.
- 검증 결과: 빌드 경고/오류0, 정점 제거 후 종료 경로를 탐색하는 독립1000그래프 정답집 통과. 분기·반복·짧은 반환 복제·공유 반환·None·for 종료·예외/중단/비종료·예산 거부 검증 통과. HTTP/MCP/DB TRUE/FALSE 반환 및 합성 종료, 비종료 사유, 이전 버전5 stale 통과. 게시 앱6종 종료0·stdio44·기존 언어 회귀 통과.
- 실제 적용: 경로/포트 소유 확인한 PID56852만 종료하고 PID59036 `artifacts/live-p2-python-control/SMSR.App.exe` 숨김 실행. 127.0.0.1:49783 유지, 리비전35(557파일/1215노드/707관계). 버전6 최신·11범위 저장. decision2·choose24·walk8 제어 의존, protected/stream/endless 사유를 실제 DB에서 확인. 화면 TRUE/FALSE 반환 표·브라우저 오류0, 증거 `smsr-p2-python-control.png`, 상태표31행 모두3열.
- 데이터 보호: `verification-backups/20260928-192033-061519` 별도 복원 무결성·외래키·전체 행수 통과. 운영 DB 미교체·원문 질문 제외·전역 Git 훅 미적용 유지. 대상 코드 실행 없음.
- 남은 위험: 유한 종료 경로 후지배이며 실제 종료·경로 조건·암묵적 예외·taint/안전성 증명이 아니다. 조건부 반복의 무한 경로는 종료 경로 의미에 포함하지 않는다. 예외/중단/종료 불가 영역과 전체16언어 정밀 분석·함수 간 PDG/taint는 계속 잔여다. 새 모듈3000자 이하, 기존 통합 파일 초과는 구조 유지 예외다.
- 다음 조치: 나머지 언어 분석기의 설치 범위를 확정하고 실제 의미 분석을 확장하며 기존 언어의 함수 간 데이터/제어·불확실성 연결을 계속 구현한다.

## 2026-09-29 - P2 Go 컴파일러 기반 소스 준비 (미검증)

- 변경 파일: `src/SMSR.GoAnalysis/{main,input,analyze,facts,types,calls,arguments}.go`, `{analyze,boundaries,bundle}_test.go`, `README.md`; `scripts/build-go-analysis.ps1`; P2 보고서·계획서 MD/HTML·개발 이력.
- 변경 사유: 16언어 정밀 분석의 남은 Go 경로를 실제 표준 `go/parser`·`go/types` 기반으로 준비한다. 단일 디렉터리/패키지의 명시적 파일 묶음, 물리 UTF-16 위치, 선언·참조·호출자·타입 형태·직접/인터페이스/함수값 호출과 인자 대응을 구현했다. 현재 코드가 동작한다는 실행 증거는 아직 없다.
- 안전 설계: 공식 Go source importer에 cgo 명령 실행 경로가 있어 사용하지 않는다. 신뢰한 SDK의 std export 준비와 대상 분석을 분리하고 gc importer에 제한된 자료 조회만 제공한다. 대상 코드·빌드·go.mod·cgo·generate·init/main은 실행하지 않는다. 상수/struct tag/원문 진단은 제외한다. 파일500·원문16 MiB·요청32 MiB·출력16 MiB·AST/타입 작업5만과 메모리 soft limit을 둔다. 향후 기존 GraphWorker 시간/Job/출력 제한 연결 필요.
- 검증 준비: 다중 파일 호출, 이름 가림, 제네릭, 인터페이스/함수값, 표준 라이브러리, 가변/펼침/다중 반환, 경로·UTF-16·CRLF·line 지시문·비밀 제외 테스트를 작성했다. SDK 빌드 스크립트는 프로세스 환경만 조정/복원하며 네트워크 패키지·툴체인 자동 다운로드와 외부 캐시 프로그램을 사용하지 않는다.
- 실행 명령: 기존 graph-runtime의 `tree_sitter_language_pack.get_parser('go')`로 모든 새 Go 파일 파싱; PowerShell `Language.Parser.ParseFile`로 빌드 스크립트 파싱; UTF-16 컬럼 독립 계산; `git diff --check`·계획서 상태표 검사. Go build/test와 빌드 스크립트 실행은 하지 않았다.
- 검증 결과: Go10파일 구문 오류0, 새 Go파일 모두3000자 이하, PowerShell 구문 오류0. UTF-16 표본의 기대 컬럼을 독립 계산값20으로 맞췄다. 실제 컴파일러 의미 검사·테스트 실행은 **미검증**이다. 표준 자료 생성/라이선스 복사도 미실행이다.
- 현재 결정 대기: 공식 Go SDK를 SMSR 전용 위치에 설치할지 사용자에게 질문했다. 답변 전 다운로드·설치·PATH/전역 설정 변경은 하지 않았다. 실행 중인 PID59036 Python 제어 의존 배포본과 운영 DB를 변경하지 않았으며 Go API/MCP/UI도 노출하지 않았다.
- 남은 위험: 코드가 실제 SDK에서 컴파일되는지와 기대값 정확성은 아직 입증되지 않았다. export 자료의 호환성·재빌드 수명주기·프로세스 제한·실제 저장/화면 통합은 검증해야 한다. 모듈/외부 패키지/빌드 설정·CFG/PDG/taint는 잔여다. Go 및 전체16언어 P2 완료로 표시하지 않는다.
- 다음 조치: 설치 허용 또는 기존 Go SDK 절대 경로를 받으면 실제 컴파일/정답집부터 검증·수정하고, 별도 후속 노드로 기존 앱 저장/조회/화면에 연결한다.

## 2026-09-29 - P2 실제 저장소 조회 성능 게이트

- 변경 파일: `scripts/benchmark-live-graph.py`, `graph_benchmark_oracle.py`, `graph_benchmark_snapshot.py`, `test_graph_benchmark.py`; `docs/graph-live-performance-2026-09-29.md`, P2 진행 보고서·계획서 MD/HTML·개발 이력.
- 변경 사유: 사용자 결정인 성능 실측 후 DB 판단에 실제 저장소 근거를 보탠다. 기존 HTTP 경로/영향도 조회를 재사용했다. Graphify 캐시가 없어 현재 코드와 DB 계약을 직접 확인했으며 새 그래프 생성·외부 패키지 설치는 하지 않았다.
- 구현: 운영 SQLite 읽기 전용 트랜잭션으로 리비전35 자료를 읽고 별도 BFS 정답과 비교한다. 도달32/비도달8 경로, 유입 상위20/무작위20 영향도 노드, 유형별 예열5·측정40, 리비전 고정·p95 2초 검사. 작은 실제 그래프용 상한2,000노드/10,000관계가 있으며 대형 시험을 대신하지 않는다.
- 실행 명령: graph-runtime Python으로 `scripts/test_graph_benchmark.py`, `scripts/benchmark-live-graph.py`; 읽기 전용 health/freshness·환경/배포본 해시 확인; `git diff --check`; 계획서 상태표 검사. 앱 코드 미변경으로 빌드/재배포는 하지 않았다.
- 검증 결과: 동일 표본5회·각 유형40회 정답 일치, 경로 p95 9.42~22.36ms, 영향도10.56~25.21ms, 응답 잘림0. 마지막 수정본 실제 실행도 통과. 자기검사에서 순환/최단/비도달·잘못된 응답·영향도 누락/중복/상한·p95·표본 재현·크기 거부·리비전 선택/연결 해제 통과. 새 코드4파일 각3,000자 이하.
- 검증 중 수정: 초기 자체 표본에 양성 쌍이32개 미만이라 입력이 거부되어 충분한 독립 사슬을 구성했다. 임시 DB 회귀를 추가하자 Windows 파일 잠금으로 정리가 실패했다. sqlite 연결 문맥 관리자는 트랜잭션만 관리하고 close하지 않으므로 조회 헬퍼와 테스트에 `contextlib.closing`을 적용하고 다시 통과했다. 최초 실측 노드 완료 후 추가한 이 검사는 보고서 검증 단계에서 반영했으며 최종 검증은 수정본 기준이다. 하나의 패치에 같은 파일 작업을 중복 지정한 요청은 도구가 원자적으로 거부했고 단일 작업으로 합쳐 적용했다.
- 데이터 보호: 운영 DB/앱/색인은 변경하지 않았다. 원문 질문 제외, 전역 Git 훅 미적용, 신규 SDK 설치 대기 유지. 테스트는 임시 DB에만 쓴다. 문서의30,752,768바이트는 전체 운영 DB 파일 크기이며 그래프별 사용량이나 증가량으로 보고하지 않는다.
- 남은 위험: 측정 당시 stale(추가16·변경4)이며 현재 파일 트리가 아닌 저장된557파일/1,215노드/707관계 조회다. 미해결 문서 참조2건은 유지. 대형 실제 부하/동시성/냉캐시/색인/본문검색/UI 및16언어 정밀 분석·PDG/taint는 미완료. 현재 규모에는 DB 교체 근거가 없다.
- 다음 조치: Go SDK 설치 결정/기존 경로를 받으면 실제 컴파일 검증과 앱 통합을 계속하고, 대형 실제 부하 측정은 별도로 확장한다.

## 2026-09-29 - P2 Java 정상 CFG·조건별 제어 의존

- 변경 파일: `SMSR.JavaAnalysis/Control{Graph,Statements,Loops,Expressions,Operands}.java`, `MethodControl.java`, `MethodValues.java`, `Analyzer.java`; `GraphRuntime/java_control.py`, `java_bundle.py`, `test_java_control{,_paths}.py`; `Mvp/GraphJavaControl{Page,SelfCheck}.cs`, Java 분석/화면/통합검사 및 고급 화면; Java 예제·추적 가이드·P2 보고서·계획서 MD/HTML·개발 이력.
- 변경 사유: 기존 Java 값 분석의 구문상 제어 후보만으로는 조기 반환 뒤의 조건 의존이나 단락/반복 경로를 설명할 수 없다. 설치 JDK17 AST·기존 소스 위치/예산·검증된 공용 후지배 계산을 재사용해 독립 정상 CFG와 조건별 제어 의존을 추가했다. 새 패키지/SDK는 설치하지 않았다.
- 구현: if·단락/삼항·while/for/do·라벨 없는 break/continue·반환/명시 throw, 식의 평가 순서와 람다 생성/본문 구분. 도달 불가 정점 제외·합성 종료 -1·유한 종료 경로의 즉시 후지배/분기별 관계. 종료 불가 영역은 정상 CFG만 유지하고 제어 의존은 사유와 빈 목록으로 반환한다. 예외/미지원 문법은 빈 CFG와 사유다. 기존 값 후보와 제어 결과는 별도 지원 상태다.
- 검증 근거: JLS17 정상/급격 완료 규정과 javac 공개 AST를 확인했다. 독립 불리언 조합별 단락/삼항 실행 순서·조기/공유 반환·continue 갱신·무한 반복/try·리터럴 제외·결정성·계산 한도와 소형1000그래프 정점 제거 후 종료 탐색 정답집을 대조했다. 대상 프로그램은 실행하지 않았다.
- 실행 명령: `scripts/build-java-analysis.ps1`; `dotnet build src/SMSR.App/SMSR.App.csproj --no-restore -v minimal`; `dotnet publish src/SMSR.App/SMSR.App.csproj -c Release --no-restore -o artifacts/live-p2-java-control -v minimal`; 게시 `test_java_control.py`, `test_java_bundle.py`, `test_java_values.py`, `test_java_summaries.py`, `test_python_compiler.py`, `test_runtime.py`, `test_language_coverage.py`, `test_typescript_summaries.py`; 게시 앱6종 자체검사·`scripts/test-mcp-stdio.ps1`; HTTP/SQLite/UI 및 `scripts/verify-graph-backup.py`·표/크기/공백 검사.
- 검증 결과: 빌드 경고/오류0. 고급 HTTP/MCP/SQLite 검사에서 버전4·TRUE/FALSE 반환/종료 불가 사유·원문 미저장·버전3 stale·변경/삭제 보호 통과. 게시 앱6종 종료0, stdio44, 기존 언어 회귀 통과. 처음 잘못 사용한 `--graph-advanced-self-check` 종료0은 검증 근거에서 제외하고 소스에서 확인한 `--graph-advanced-self-test`로 다시 실행해 통과했다.
- 실제 적용: 경로/포트 소유가 일치한 PID59036만 종료 후 PID58656 `artifacts/live-p2-java-control/SMSR.App.exe --background --ensure-server`를 숨김 실행. 127.0.0.1:49783 유지. 리비전36(585파일/1259노드/727관계), Java 버전4 최신·14메서드·진단0. decide6노드/7전이/4관계, selected7/9/4, counted17/18/10, endless3/3/0 및 종료 불가 사유 확인. 실제 DB와 화면의 TRUE/FALSE 반환 일치·브라우저 오류0. 화면 증거 `smsr-p2-java-control.png` 저장.
- 데이터 보호: 운영 DB 미교체. 적용 전 `verification-backups/20260928-195518-089428`, 적용 후 `20260928-195823-376766`의 별도 복원에서 무결성/외래키/전체 행수 검사 통과. 원문 질문 제외·전역 훅 미적용·Go SDK 결정 대기 유지.
- 문서/크기: 상태표33행 모두3열, 신규 모듈 모두3000자 이하, 변경 공백 검사 통과. 기존 통합 파일 크기는 기존 구조 유지 예외다. Ponytail/Karpathy에 따라 새 프레임워크 없이 기존 컴파일러·후지배·저장 경로를 재사용했다.
- 남은 위험: 유한 종료 경로 의미이며 실제 종료·조건 만족성(불리언 리터럴 제외)·암묵적 예외·호출 내부 효과를 증명하지 않는다. try/switch/확장 for/라벨/동기화/익명 클래스 초기화와 함수 간 제어/완전한 PDG·taint는 잔여다. 전체16언어 의미 분석·대형 성능/최종 수용도 미완료다.
- 다음 조치: 기존 언어의 미지원 제어/값 경계와 함수 간 연결을 확장하고, Go 설치 결정 또는 기존 SDK 경로가 제공되면 실제 컴파일 검증을 이어간다.

## 2026-09-29 - P2 JS/TS 정상 CFG·조건별 제어 의존

- 변경 파일: `GraphRuntime/typescript_control{,_statements,_loops,_expressions,_operands}.cjs`, `typescript_facts.cjs`, `typescript_functions.cjs`, `typescript_bundle.py`, `java_control.py`, `test_typescript_control{,_paths}.py`; `Mvp/GraphTypeScriptControlSelfCheck.cs`, TypeScript 분석/저장 검사/화면 및 공용 Java 제어 화면; TypeScript 예제·추적 가이드·P2 보고서·계획서 MD/HTML·개발 이력.
- 변경 사유: JS/TS의 구문상 제어 후보와 실제 AST 정상 분기 근거를 구분한다. Ponytail/Karpathy에 따라 기존 TypeScript 컴파일러·작업 예산·Java의 도달성/후지배 계산·저장/화면 계약을 재사용했다. 새 패키지나 SDK는 설치하지 않았다.
- 구현: if·단락/삼항·while/for/do·라벨 없는 break/continue·반환/명시 throw의 정상 CFG. TRUE/FALSE 진릿값과 NULLISH/NON_NULLISH를 구분하며 논리 대입은 오른쪽 식이 실행되는 경로에서만 대입한다. 식 평가 순서·지연 함수 본문 제외·불리언 리터럴 도달성·유한 종료 후지배를 적용한다. 미지원 문법은 빈 CFG와 사유, 종료 불가 영역은 전이를 남기고 빈 제어 관계와 사유를 반환한다. 분석 버전6이며 버전5 결과는 stale 처리한다.
- 실행 명령: `dotnet build src/SMSR.App/SMSR.App.csproj --no-restore -v minimal`; `dotnet publish src/SMSR.App/SMSR.App.csproj -c Release --no-restore -o artifacts/live-p2-typescript-control -v minimal`; 게시 런타임의 `test_typescript_control.py`, `test_typescript_bundle.py`, `test_typescript_values.py`, `test_typescript_loops.py`, `test_typescript_summaries.py`, `test_typescript_connections.py`, `test_typescript_boundaries.py`, `test_java_control.py`, `test_java_summaries.py`, `test_python_compiler.py`, `test_language_coverage.py`, `test_runtime.py`; 게시 앱6종 자체검사·`scripts/test-mcp-stdio.ps1`; `scripts/verify-graph-backup.py`; HTTP/SQLite/UI·공백/상태표/크기 검사.
- 검증 결과: 빌드 경고/오류0, 게시 앱6종 종료0·stdio44·언어 회귀 통과. 조건 조합별 평가 순서·조기/공유 반환·논리 대입·continue 갱신·무한 반복·미지원 사유·결정성·비밀 리터럴 제외·1000개 소형 그래프 독립 후지배 정답집 통과. 최초 JS 표본이 strict의 implicit-any 진단으로 거부되어 JSDoc 타입을 추가한 뒤 통과했다. 컴파일러 strict 설정은 완화하지 않았다. HTTP/MCP/SQLite 계약에서 버전6과 NULLISH 대입/TRUE·FALSE 반환 및 stale 보호를 검증했다.
- 실제 적용: 이전 배포 경로와 포트 소유가 일치하는 PID58656만 종료하고 PID56744 `artifacts/live-p2-typescript-control/SMSR.App.exe --background --ensure-server`를 숨김 실행했다. 127.0.0.1:49783 유지. 리비전37(588파일/1265노드/731관계), JS/TS 버전6·14함수·진단0·stale=false. decide7노드/4관계, nullSet8/2, selected9/5, endless3/0 및 NON_EXIT_REACHABLE_REGION 확인. SQLite의 조건 관계와 화면의 NULLISH 대입이 일치한다. Java 버전4도 재분석하고 기존 TRUE/FALSE 반환 표 회귀 확인, 브라우저 경고/오류0. 화면 증거 `smsr-p2-typescript-control.png` 저장.
- 데이터 보호: 운영 DB 미교체. 적용 전 `verification-backups/20260928-201012-154944`, 적용 후 `20260928-201550-427586`의 별도 복원에서 무결성·외래키·전체 테이블 행수 일치. 원문 질문 제외·전역 Git 훅 미적용·Go SDK 결정 대기 유지.
- 문서/크기: 계획서 상태표34행 모두3열, 신규 코드8파일 모두3000자 이하, 변경 공백 검사 통과. 기존 통합 파일은 기존 구조 유지 예외다.
- 남은 위험: 정상 AST 근거이며 종료 보장·조건 만족성·암묵적 예외·getter/연산자/호출 내부 효과·함수 간 제어/완전한 PDG·taint는 아니다. nullish 정적 분기는 값이 리터럴이어도 보수적으로 둘 다 남긴다. try/switch/for-in/of/라벨/optional chain/객체·구조분해·async/generator 등 미지원 경계가 남는다. 16언어 전체 정밀 분석·대형 부하/최종 수용은 미완료다.
- 다음 조치: 기존 언어의 미지원 제어/값 경계와 함수 간 분석을 확장한다. Go SDK 설치 허용 또는 기존 SDK 경로가 제공되면 실제 컴파일/통합을 검증한다.

## 2026-09-29 - P2 JS/TS switch 정상 CFG

- 변경 파일: `GraphRuntime/typescript_control_switch.cjs`, `typescript_control_statements.cjs`, `test_typescript_switch{,_paths}.py`; `Mvp/GraphTypeScriptSwitchSelfCheck.cs`, TypeScript 분석 버전/저장/통합검사/화면; TypeScript 예제·README·추적 가이드·P2 보고서·계획서 MD/HTML·개발 이력.
- 변경 사유: 기존 정상 CFG에서 빠진 switch 선택·default·fall-through·중첩 전이를 추가한다. Ponytail/Karpathy에 따라 공용 AST 위치/예산/후지배/저장/화면을 재사용했다. Graphify 캐시가 없음을 확인하고 현재 소스 계약을 직접 추적했으며 별도 그래프 추출·신규 의존성/SDK 설치는 하지 않았다.
- 구현: 선택식 1회 평가 후 CASE_TEST를 소스 순서로 연결한다. CASE_MATCH/CASE_NO_MATCH는 엄격 일치/불일치의 정적 전이이고 실제 값을 풀지 않는다. default는 모든 case 불일치 후 선택하며, 선택된 본문은 다음 case 조건식을 거치지 않고 이어진다. break의 switch 종료와 continue의 외부 반복 갱신/조건을 분리한다. 값/도달 정의의 switch 미지원은 그대로 표시한다. 버전7 저장, 버전6 이하 stale.
- 실행 명령: `dotnet build src/SMSR.App/SMSR.App.csproj --no-restore -v minimal`; `dotnet publish src/SMSR.App/SMSR.App.csproj -c Release --no-restore -o artifacts/live-p2-typescript-switch -v minimal`; graph-runtime Python의 `test_typescript_switch.py`, `test_typescript_control.py`, `test_typescript_bundle.py`, `test_typescript_values.py`, `test_typescript_loops.py`, `test_typescript_summaries.py`, `test_typescript_connections.py`, `test_typescript_boundaries.py`, `test_java_control.py`, `test_python_compiler.py`, `test_language_coverage.py`, `test_runtime.py`; 게시 앱6종 자체검사·`scripts/test-mcp-stdio.ps1`·`scripts/verify-graph-backup.py`; HTTP/SQLite/UI·상태표/크기/공백 검사.
- 검증 결과: 빌드 경고/오류0, 소스 및 게시 고급 통합 검사 통과. 게시 앱6종 종료0·stdio44·기존 언어 회귀 통과. default 위치/없음·break 조합72배치, 선택/불일치/복수 일치360경로를 독립 본문/평가 순서 정답과 대조했다. 빈/default-only/grouped case·중첩 switch/loop·명시 throw·미지원 try·결정성·비밀 리터럴 제외·JS JSDoc 검사 통과. 기존1000그래프 후지배 정답집도 통과. 대상 프로그램은 실행하지 않았다.
- 계약 검증: HTTP/실제 MCP/SQLite에서 CASE_TEST2개·SWITCH_VALUE1개·선택 전이4개·반환3개의 제어 관계 및 값 UNAVAILABLE 분리를 확인했다. 이전 버전6 stale, 입력 경로·해시 변경·원문 미저장·삭제 정리 보호를 유지한다.
- 실제 적용: 경로/포트 소유를 확인한 PID56744만 종료하고 PID46260 `artifacts/live-p2-typescript-switch/SMSR.App.exe --background --ensure-server`를 숨김 실행했다. 127.0.0.1:49783 유지. 리비전38(591파일/1270노드/734관계), 버전7·진단0·16함수·stale=false. switchPick15노드/17전이/10관계, switchFall18/20/10. 읽기 전용 SQLite 검사와 화면의 CASE_MATCH/CASE_NO_MATCH 반환 일치·브라우저 경고/오류0. `smsr-p2-typescript-switch.png` 저장.
- 데이터 보호: 운영 DB 미교체. 전/후 `verification-backups/20260928-202601-568753`, `20260928-202755-144165`의 별도 복원에서 무결성·외래키·전체 테이블 행수 일치. 원문 질문 제외·전역 Git 훅 미적용·Go SDK 설치 결정 대기 유지.
- 문서/크기: 계획서 상태표35행 모두3열·신규 코드4파일 모두3000자 이하·공백 검사 통과. 최초 스킬 경로 확인 실패는 카탈로그의 플러그인 경로로 바로잡았다. HTML 편집 중 중복 셀을 제거하고 열 수를 검사했다.
- 남은 위험: 실제 case 값 만족성·암묵적 예외/TDZ·호출 내부 효과·switch 값/도달 정의·함수 간 제어/정밀 PDG·taint는 남아 있다. 원문·리터럴 값은 저장하지 않는다. 16언어 전체 정밀 분석 및 P2 최종 수용을 완료로 표시하지 않는다.
- 다음 조치: 지원 제어 경로를 값/함수 간 분석으로 확장하고 언어별 미지원 경계를 줄인다. SDK 설치 허용/경로가 제공되면 Go 실제 컴파일·통합을 이어간다.

## 2026-09-29 - P2 JS/TS switch 값·함수 간 반환 요약

- 변경 파일: `GraphRuntime/typescript_value_switch.cjs`, `typescript_value_statements.cjs`, `test_typescript_switch_values.py`, `test_typescript_switch_value_paths.py`; `Mvp/GraphTypeScriptSwitchValueSelfCheck.cs`, switch/분석 버전/저장/통합검사 및 값 화면; TypeScript 예제·README·추적 가이드·P2 보고서·계획서 MD/HTML·개발 이력.
- 변경 사유: 정상 switch 제어 흐름만 존재하고 값/도달 정의가 미지원이던 공백을 줄인다. Ponytail/Karpathy에 따라 기존 상태 복사/합류·반복 프레임·심벌/값 노드·호출별 고정점 요약을 재사용했다. 추가 SDK/의존성을 설치하지 않았다.
- 구현: case 검색을 순서대로 평가하며 일치 시점의 상태를 복사한다. 뒤쪽 case의 대입을 앞선 선택 경로에 섞지 않고 default는 전체 검색 뒤 상태로 시작한다. 본문 진입·이전 본문 fall-through 상태를 합치며 대입은 이전 정의를 교체한다. break는 switch 종료, continue는 외부 반복에 전달한다. 조건 후보는 데이터 요약에서 제외한다. 버전8 및 버전7 이하 stale로 갱신했다.
- 실행 명령: `dotnet build src/SMSR.App/SMSR.App.csproj --no-restore -v minimal`; `dotnet publish src/SMSR.App/SMSR.App.csproj -c Release --no-restore -o artifacts/live-p2-typescript-switch-values -v minimal`; graph-runtime Python의 `test_typescript_switch_values.py`, `test_typescript_switch.py`, `test_typescript_control.py`, `test_typescript_bundle.py`, `test_typescript_values.py`, `test_typescript_loops.py`, `test_typescript_summaries.py`, `test_typescript_connections.py`, `test_typescript_boundaries.py`, `test_java_control.py`, `test_python_compiler.py`, `test_language_coverage.py`, `test_runtime.py`; 게시 앱6종 자체검사·`scripts/test-mcp-stdio.ps1`·`scripts/verify-graph-backup.py`; HTTP/SQLite/UI·상태표/크기/공백 검사.
- 검증 결과: 224배치·672개 독립 선택/본문 경로의 입력→반환 도달 및 요약 일치. case 대입·덮어쓰기·default 위치·break·조기/암묵 반환·중첩 switch/loop의 continue·이름 가림·호출별 relay/erased·외부 미해석 값 격리·결정성 검사 통과. 앞 case 조기 반환에 뒤 case 대입 입력이 들어오지 않는 개별 반환 검사도 통과했다. 기존 switch제어72배치/360경로·1000그래프 후지배와 언어 회귀 통과. 대상 코드는 실행하지 않았다.
- 검증 중 수정: 중첩 테스트에서 외부 case로 0으로 좁혀진 같은 변수를 내부 case1과 비교하자 TS2678이 발생했다. 내부 선택 인자를 독립적으로 바꾸어 정상 표본으로 검증했다. 컴파일러 오류 하향 정책은 유지했다. PowerShell 크기 조회의 foreach 뒤 직접 파이프 문법을 고쳐 다시 확인했다. HTML 삽입 중 중복 셀을 바로잡고 전체 행/열 검사를 통과했다.
- 저장 계약: HTTP/실제 MCP/SQLite에서 switch 반환 입력1·2, relay 호출 간선2개와 정확한 인수/결과 ID, 덮어쓰기 반환 영향/호출 간선0개를 검사했다. 버전7 stale·원문/경로/해시·삭제 보호 통과. 빌드 경고/오류0, 소스 고급 검사와 게시 앱6종 종료0·stdio44 통과.
- 실제 적용: 경로와 포트 소유가 일치한 PID46260만 종료 후 PID42856 `artifacts/live-p2-typescript-switch-values/SMSR.App.exe --background --ensure-server`를 숨김 실행했다. 127.0.0.1:49783 유지. 리비전39(594파일/1275노드/736관계), 버전8 최신·진단0·19함수. switchPick16값노드/24간선, switchFall21/32, switchRelayed13/11·호출 요약2개·입력1/2, switchKilled11/13와 switchErased10/8·빈 영향 입력 확인. 실제 SQLite/화면 결과 일치·브라우저 오류0. 증거 `smsr-p2-typescript-switch-values.png` 저장.
- 데이터 보호: 운영 DB 미교체. 적용 전 `verification-backups/20260928-203823-128855`, 적용 후 `20260928-204001-703594`의 별도 복원에서 무결성·외래키·전체 테이블 행수 일치. 원문 질문 제외·전역 Git 훅 미적용·Go SDK 설치 결정 대기 유지.
- 문서/크기: 상태표36행 모두3열, 신규 코드4파일 각각3000자 이하, 공백 검사 통과. 기존 통합 파일은 구조 유지 예외다.
- 남은 위험: 경로 조건을 풀지 않는 정상 흐름 MAY 후보다. 실제 case 만족성·암묵적 예외/TDZ·힙/별칭·호출/연산자 효과·함수 간 제어·완전한 PDG/taint 및 전체16언어 정밀 분석은 미완료다. 영향 입력이나 미해석 목록이 비어도 안전성 판정이 아니다.
- 다음 조치: 언어별 미지원 제어/값 경계와 함수 간 연결을 계속 확장한다. 허용된 SDK/분석기 환경이 제공되면 나머지 언어의 실제 의미 해소와 정답집 검증을 이어간다.

## 2026-09-29 - P2 Python 함수 내부 입출력 요약

- 변경 파일: `GraphRuntime/python_summary_slots.py`, `python_value_summary.py`, `python_compiler.py`, `test_python_value_summary.py`, `test_python_summary_boundaries.py`; `Mvp/GraphPythonSummarySelfCheck.cs`, `GraphPythonSummaryPage.cs`, Python/고급 화면·분석 버전·저장 검사; Python 예제·README·추적 가이드·P2 보고서·계획서 MD/HTML·개발 이력.
- 변경 사유: 기존 Python 값 그래프에서 입력이 어느 반환/호출 인자에 도달하는지 요약하지 못하던 공백을 줄인다. Ponytail/Karpathy에 따라 기존 값 간선·출력/계산 예산·저장/화면 헬퍼를 재사용했다. 새 SDK·의존성을 설치하지 않았다. Graphify 캐시 없이 현재 소스 계약을 직접 확인했다.
- 구현: 입력 종류와 CPython fast-slot 순서, 반환·호출 대상 후보/수신 객체/인자별 데이터 의존을 계산한다. 외부/속성/반복/분해·미할당·호출 결과의 미해석 ID를 전파한다. 조건 후보는 데이터 요약에서 제외하고, 덮어쓰기 이후 이전 입력을 제거한다. 호출 인자→결과를 추측 연결하지 않는다. 파일 분석 버전7·이전 버전6 stale. 원문/기본값/이름 인자 이름은 저장하지 않는다.
- 실행 명령: `dotnet build src/SMSR.App/SMSR.App.csproj --no-restore -v minimal`; `dotnet publish src/SMSR.App/SMSR.App.csproj -c Release --no-restore -o artifacts/live-p2-python-summary -v minimal`; graph-runtime Python의 `test_python_value_summary.py`, `test_python_compiler.py`, `test_typescript_summaries.py`, `test_typescript_switch_values.py`, `test_typescript_control.py`, `test_java_control.py`, `test_language_coverage.py`, `test_runtime.py`; 게시 앱6종 자체검사·`scripts/test-mcp-stdio.ps1`·`scripts/verify-graph-backup.py`; HTTP/읽기 전용 SQLite/UI·상태표/파일 크기/공백 검사.
- 검증 결과: 32개 덮어쓰기 프로그램/1024분기 조합의 독립 데이터 입력 정답, 매개변수5종·호출별 미해석 격리·수신 객체/반복 인자·미지원·예산·비밀 리터럴 제외·결정성 통과. 초기 독립 정답의 입력 조합을 다양화하여 a/b/c와 상수 덮어쓰기 부분집합으로 강화했다. 소스 및 게시 통합 검사, 게시 앱6종 종료0·stdio44, 기존 Python/JS/TS/Java 및16언어 구문·Cypher 보호 회귀 통과. 빌드 경고/오류0. 대상 프로그램은 실행하지 않았다.
- 저장 계약: HTTP/실제 MCP/SQLite에서 입력→반환, 조건 제외, 호출 인자와 미해석 결과 분리, 프로토콜 경계, 공유셀 미지원, 버전6 stale 및 원문/해시/경로/삭제 보호를 확인했다.
- 실제 적용: 경로/포트 소유가 일치한 PID42856만 종료 후 PID52492 `artifacts/live-p2-python-summary/SMSR.App.exe --background --ensure-server`를 숨김 실행했다. 127.0.0.1:49783 유지. 리비전40(600파일/1285노드/740관계), 파일 버전7·stale=false. 실제 SQLite에서 summarized 반환 입력1/2, cleared 입력 영향 없음, publish 대상1·인자0·반환 미해석 결과 ID 일치를 확인했다. Computer Use로 동일한 화면 표시·브라우저 경고/오류0을 확인하고 `smsr-p2-python-summary.png`를 저장했다.
- 데이터 보호: 운영 DB 미교체. 전/후 `verification-backups/20260928-205255-483422`, `20260928-205425-143824` 별도 복원의 무결성·외래키·전체 테이블 행수 일치. 원문 질문 제외·전역 Git 훅 미적용·Go SDK 설치 결정 대기 유지.
- 문서/크기: 계획서 상태표37행 모두3열, 신규 코드6파일 모두3000자 이하, 공백 검사 통과. 기존 통합 파일은 기존 구조 유지 예외다.
- 남은 위험: 입력 번호는 컴파일러 슬롯 순서이며 소스 선언 순서/호출 바인딩이 아니다. 가변 인자는 컨테이너 슬롯으로 남는다. 실제 동적 대상·함수 간 반환·예외·힙/별칭·연산자/프로토콜 사용자 코드 효과·경로 조건은 미해소다. 빈 영향/미해석 목록은 안전성 증명이 아니다. 전체16언어 정밀 분석·PDG/taint·대형 부하·최종 수용은 미완료다.
- 다음 조치: 정적으로 근거를 확보할 수 있는 호출 대상/입출력 연결을 확장하고, 나머지 언어의 의미 분석기·정답집을 이어간다. SDK 설치 허용 또는 기존 SDK 경로가 제공되기 전에는 Go 실제 컴파일 완료를 주장하지 않는다.

## 2026-09-29 - P2 Python 지역 함수 객체·호출 본문 연결

- 변경 파일: `GraphRuntime/python_local_origins.py`, `python_local_calls.py`, `test_python_local_calls.py`, `test_python_local_boundaries.py`, Python 값 전달/컴파일러/요약; `Mvp/GraphPythonLocalCallSelfCheck.cs`, `GraphPythonLocalCallPage.cs`, Python 요약/고급 화면·버전/저장 검사; Python 예제·README·추적 가이드·P2 보고서·계획서 MD/HTML·개발 이력.
- 변경 사유: Python 함수 이름을 추측 연결하지 않으면서 함수 생성과 지역 값 전달로 확인 가능한 본문을 연결한다. Ponytail/Karpathy에 따라 기존 값 노드·도달 정의·예산·저장/UI 헬퍼를 재사용했다. Graphify 캐시 없음 확인 후 현재 소스를 추적했으며 추출 파이프라인이나 신규 SDK/의존성은 실행·설치하지 않았다.
- 구현: 코드 객체와 MAKE_FUNCTION 근거를 생성하고 함수 객체의 코드 입력 및 지역 읽기/쓰기만으로 정체성 후보를 전파한다. 연산·컨테이너·속성·외부 조회는 정체성을 전달하지 않는다. 유일 후보·지원 본문·모든 위치 매개변수와 정확한 인자 개수일 때 호출별 인자→매개변수, 본문 반환→이번 호출 결과 위치 ID를 연결한다. 다중/외부 후보는 AMBIGUOUS, 기타는 UNRESOLVED/불가 사유로 표시한다. 파일 분석 버전8·버전7 이하 stale.
- 실행 명령: `dotnet build src/SMSR.App/SMSR.App.csproj --no-restore -v minimal`; `dotnet publish src/SMSR.App/SMSR.App.csproj -c Release --no-restore -o artifacts/live-p2-python-local-calls -v minimal`; 설치 graph-runtime Python의 `test_python_local_calls.py`, `test_python_value_summary.py`, `test_python_compiler.py`, `test_typescript_summaries.py`, `test_java_control.py`, `test_language_coverage.py`, `test_runtime.py`; 게시 앱6종 자체검사·`scripts/test-mcp-stdio.ps1`·`scripts/verify-graph-backup.py`; HTTP·읽기 전용 SQLite·브라우저·상태표/크기/공백 검사.
- 검증 결과: 32프로그램/1024분기 조합의 독립 함수 정체성 정답, 별칭·덮어쓰기·분기 다중/외부 후보·반복·두 호출 격리·명시 기본값 대체·정적 주석·이름/가변 인자·미지원 본문·예산 경계 통과. 데코레이터의 CPython 호출 스택은 기존 값 모델에서 UNAVAILABLE임을 확인하고 테스트 기대치를 그 경계로 수정했다. 기존 Python/JS/TS/Java·16언어 구문·Cypher 보호 회귀 통과. 대상 저장소 코드는 실행하지 않았다.
- 저장/배포 검사: Debug 및 게시 고급 통합의 HTTP/MCP/SQLite 본문 ID와 호출별 입출력 ID 일치, 기존 미해석 결과 유지·외부 대상 미해소·버전7 stale 및 원문/해시/경로/삭제 보호 통과. 빌드 경고/오류0·게시 앱6종 종료0·stdio44 통과.
- 실제 적용: 경로·포트 소유가 일치한 PID52492만 종료하고 PID57956 `artifacts/live-p2-python-local-calls/SMSR.App.exe --background --ensure-server` 숨김 실행. 127.0.0.1:49783 유지. 리비전41(606파일/1295노드/744관계), 파일 버전8·stale=false. local_call 유일 본문·인자/반환 연결 각1개, local_choice 외부 후보 포함 AMBIGUOUS·입출력 연결0개를 실제 SQLite/화면에서 확인했다. Computer Use 검증·브라우저 경고/오류0, `smsr-p2-python-local-calls.png` 저장.
- 데이터 보호: 운영 DB 미교체. 전후 `verification-backups/20260928-210322-738612`, `20260928-210506-143383` 별도 복원에서 무결성·외래키·전체 테이블 행수 일치. 원문 질문 제외·전역 Git 훅 미적용·Go SDK 결정 대기 유지.
- 기록/문서: 지정된 불투명 추적 ID를 서버가 새 읽기 쉬운 ID로 치환했다. 최초 이벤트의 계획 없음 응답 뒤 반환 ID로 바로잡았으며 save_plan은1회만 호출했다. 상태표38행3열, 신규 코드6파일과 수정 값 전달 코드3000자 이하·공백 검사 통과.
- 남은 위험: 함수 객체/코드의 런타임 변경·외부 프레임 변경·경로 조건·힙/별칭·함수 간 값 요약 전파는 미해소다. 위치 연결을 데이터 전파나 실행 확정으로 승격하지 않으며 호출 반환은 계속 미해석이다. 공유셀·데코레이터·생략 기본값/이름/가변 인자 바인딩, 전체16언어 정밀 분석·PDG/taint·대형 부하·최종 수용은 잔여다.
- 다음 조치: 연결된 본문의 입출력 데이터 요약을 호출별로 적용하고, 미해석 경계를 보존하며 함수 간 값 전파 정답집을 확장한다. 나머지 언어의 의미 분석기 연결과 환경 결정도 전체 P2 잔여로 유지한다.

## 2026-09-29 - P2 Python 호출별 반환 데이터 전파

- 변경 파일: `GraphRuntime/python_summary_labels.py`, `python_call_summaries.py`, `python_call_summary_output.py`, `test_python_call_summaries.py`, `test_python_call_summary_oracle.py`; Python 기존 요약/컴파일러/지역 호출 테스트; `Mvp/GraphPythonCallSummarySelfCheck.cs`, Python 자체검사/저장/버전/요약 화면; 예제·README·추적 가이드·P2 보고서·계획서 MD/HTML·개발 이력.
- 변경 사유: 직전 단계의 지역 본문 입출력 위치 연결을 실제 반환 데이터 영향 요약으로 확장한다. Ponytail/Karpathy에 따라 기존 데이터 라벨·계산/출력 예산·호출별 값 ID·저장/화면 계약을 재사용했다. 추가 SDK/의존성 설치 없이 진행했다.
- 구현: 유일 지역 본문·정확한 위치 인자 바인딩에 한해 본문 반환의 매개변수 라벨을 해당 호출 인자 라벨로 치환한다. 호출마다 입력을 분리하며 고정점으로 다단계 의존을 전파한다. 덮어쓰기/조건 구분을 보존하고, 미지원/모호/외부 호출 및 본문 내부 미해석 값은 원래 ID로 전달한다. 함수 반환·호출 입력/결과 요약과 BODY_DATA_DEPENDENCY_CANDIDATE 간선을 제공한다. 간선 출력 순서를 정렬해 해시 시드에 따른 흔들림을 없앴다. 파일 분석 버전9·버전8 이하 stale.
- 실행 명령: `dotnet build src/SMSR.App/SMSR.App.csproj --no-restore -v minimal`; `dotnet publish src/SMSR.App/SMSR.App.csproj -c Release --no-restore -o artifacts/live-p2-python-call-summaries -v minimal`; graph-runtime Python의 `test_python_call_summaries.py`, `test_python_local_calls.py`, `test_python_value_summary.py`, `test_python_compiler.py`, `test_typescript_summaries.py`, `test_java_control.py`, `test_language_coverage.py`, `test_runtime.py`; 게시 앱6종 자체검사·`scripts/test-mcp-stdio.ps1`·`scripts/verify-graph-backup.py`; HTTP/읽기 전용 SQLite/UI·표/크기/공백 검사.
- 검증 결과: 전달/덮어쓰기/인자 순서/다단계·중첩 호출/호출 격리/조건 제외/미해석 전파/미사용 외부 호출/공유셀 재귀 미지원/예산 검사를 통과했다.16프로그램·256분기 조합의 독립 반환 데이터 정답과 프로세스별 해시 시드1/17/997에서 결과 해시 일치. 기존 Python의1024분기 조합·1000그래프 후지배 및 JS/TS/Java·16언어 구문·Cypher 보호 회귀 통과. 대상 코드는 실행하지 않았다.
- 저장 계약: Debug 및 게시 고급 검사에서 HTTP/MCP/SQLite의 local/nested 입력0·간선1개와 source/target/body ID, erased 빈 영향/간선0을 확인했다. 버전8 stale·원문/해시/경로/삭제 보호 유지. 빌드 경고/오류0·게시 앱6종 종료0·stdio44 통과.
- 실제 적용: 경로·포트 소유가 일치한 PID57956만 종료 후 PID54004 `artifacts/live-p2-python-call-summaries/SMSR.App.exe --background --ensure-server` 숨김 실행, 127.0.0.1:49783 유지. 리비전42(612파일/1305노드/748관계)·파일 버전9·stale=false. 실제 SQLite/화면에서 local_call/local_nested 입력0·간선1, local_erased 빈 영향/간선0, local_choice 미해석 유지 확인. Computer Use 화면 검증·브라우저 오류0, `smsr-p2-python-call-summaries.png` 저장.
- 데이터 보호: 운영 DB 미교체. 전후 `verification-backups/20260928-211425-160358`, `20260928-211617-406812` 별도 복원의 무결성·외래키·전체 행수 일치. 원문 질문 제외·전역 Git 훅 미적용·Go SDK 결정 대기 유지.
- 문서/크기: 상태표39행3열, 신규 코드6파일과 수정 요약 화면 각각3000자 이하·공백 검사 통과. 이전 버전7/8의 위치/미해석 계약은 이력으로 표시하고 최신 버전9 설명을 우선했다.
- 남은 위험: 정상 경로 데이터 후보이며 실제 실행 대상·함수 객체 변경·조건 만족성·종료·예외·힙/별칭/부수효과의 증명이 아니다. 원시 OPAQUE_CALL_RESULT ID 이름은 유지되므로 최종 해석은 결과 요약을 따른다. 공유셀/재귀·생략 기본값/이름/가변 인자·전체16언어 정밀 PDG/taint·대형 부하/최종 수용은 미완료다. 빈 영향/미해석 목록은 안전성 판정이 아니다.
- 다음 조치: 호출 바인딩과 미지원 의미 경계를 확장하고, 나머지 언어 분석기 연결·정답집·전체 P2 수용 항목을 계속 검증한다.

## 2026-09-29 - P2 Python 명시적 이름 인자 바인딩

- 변경 파일: `GraphRuntime/python_keyword_bindings.py`, `python_local_calls.py`, `python_compiler.py`, `test_python_keyword_bindings.py`, `test_python_keyword_oracle.py`, 지역 호출 검사; `Mvp/GraphPythonKeywordSelfCheck.cs`, Python 통합/저장/버전/본문 연결/요약 화면; Python 예제·README·추적 가이드·P2 보고서·계획서 MD/HTML·개발 이력.
- 변경 사유: 위치 인자만 연결하던 Python 지역 호출에 명시적 이름 인자 대응을 추가한다. Ponytail/Karpathy에 따라 기존 호출별 값 ID·매개변수 슬롯·계산/출력 예산·저장/UI 계약을 재사용했다. 새 SDK나 의존성은 설치하지 않았다.
- 구현: CPython KW_NAMES 메타데이터를 메모리에서만 수집하고 호출 순서와 매개변수 순서를 대응시킨다. 위치 전용/이름 전용·중복·누락·알 수 없는 이름·과다 인자를 검사한다. 부적합/미지원 바인딩은 근거 코드만 남기고 입출력 연결과 반환 영향 전파를 차단한다. 이름 인자의 원문은 결과에 추가 저장하지 않는다. 파일 분석 버전10·버전9 이하 stale.
- 실행 명령: `dotnet build src/SMSR.App/SMSR.App.csproj --no-restore -v minimal`; `dotnet publish src/SMSR.App/SMSR.App.csproj -c Release --no-restore -o artifacts/live-p2-python-keywords -v minimal`; graph-runtime Python의 `test_python_keyword_bindings.py`, `test_python_call_summaries.py`, `test_python_local_calls.py`, `test_python_value_summary.py`, `test_python_compiler.py`, `test_typescript_summaries.py`, `test_java_control.py`, `test_language_coverage.py`, `test_runtime.py`; 게시 앱6종 자체검사·`scripts/test-mcp-stdio.ps1`·`scripts/verify-graph-backup.py`; HTTP/읽기 전용 SQLite/UI·표/크기/공백 검사.
- 검증 결과: 이름 순서24개×반환 슬롯4개의 독립 정답96사례, 위치/이름 혼합·이름 전용·명시 기본값 대체·중첩 호출·한글 이름·잘못된 바인딩·메타데이터 누락·예산·결정성·비밀 표식 제외 검사 통과. 기존 Python 호출별 반환/정체성·JS/TS·Java·16언어 구문·Cypher 보호 회귀 통과. 대상 코드는 실행하지 않았다. 빌드 경고/오류0, 게시 앱6종 종료0, MCP44도구 검사 통과.
- 저장 계약: HTTP/실제 MCP/SQLite에서 이름 인자 순번0/1→매개변수 슬롯1/0, 반환 영향 입력0 및 정확한 호출별 ID를 확인했다. 잘못된 바인딩 연결0·비밀 키워드 제외·이전 버전 stale·원문/경로/해시/삭제 보호 유지.
- 실제 적용: 경로와 포트 소유가 일치한 PID54004만 종료 후 PID58048 `artifacts/live-p2-python-keywords/SMSR.App.exe --background --ensure-server`를 숨김 실행했다. 127.0.0.1:49783 유지. 리비전43(616파일/1313노드/752관계), 파일 버전10·stale=false. keyword_reordered/keyword_only의 반환 입력0 및 바인딩을 SQLite와 대조했다. Computer Use로 화면의 순번0/1→슬롯1/0 표시 확인, 브라우저 경고/오류0, `smsr-p2-python-keywords.png` 저장.
- 데이터 보호: 운영 DB 미교체. 전후 `verification-backups/20260928-212524-113542`, `20260928-212720-221448` 별도 복원에서 무결성·외래키·전체 테이블 행수 일치. 원문 질문 제외·전역 Git 훅 미적용·Go SDK 설치 결정 대기 유지.
- 환경/문서: 설치된 MSVC C/C++ 컴파일러 경로는 발견했지만 아직 정밀 분석에 연결하거나 검증하지 않았다. Go/Clang의 확인한 기본 경로와 PATH에서는 실행기를 찾지 못했다. 계획서40행3열·신규 코드4파일3000자 이하 확인.
- 남은 위험: 생략 기본값·가변 인자·공유셀·데코레이터 등은 미지원이다. MISSING_ARGUMENT는 명시적 입력 누락/미지원 사유이며 Python 실행 오류 확정이 아니다. 실제 실행 대상·함수 객체 변경·경로 조건·예외/종료/힙 효과, 전체16언어 정밀 분석·완전한 PDG/taint·대형 부하·최종 수용은 미완료다.
- 다음 조치: 나머지 언어의 실제 의미 분석기 연결과 정답집 검증을 진행하고, 지원 언어의 미지원 의미 경계를 줄인다. 전체 P2는 진행 상태를 유지한다.

## 2026-09-29 - P2 Java switch 문 제어·값 흐름

- 변경 파일: `SMSR.JavaAnalysis/ControlSwitch.java`, `ValueSwitch.java`, 기존 제어/값 문 처리기; `GraphRuntime/test_java_switch.py`, `test_java_switch_paths.py`, `test_java_switch_boundaries.py`; `Mvp/GraphJavaSwitchSelfCheck.cs`, Java 분석 버전/통합/저장 검사·화면 안내; `samples/graph-java/Switches.java`·README·추적 가이드·P2 진행 보고서·계획서 MD/HTML·개발 이력.
- 변경 사유: 기존 Java 컴파일러 결과에서 switch 문 전체가 미지원이던 공백을 줄인다. Ponytail/Karpathy에 따라 기존 CFG 노드/전이·상태 합류·반복 프레임·후지배·호출별 반환 요약·저장/UI 계약을 재사용했다. Graphify 캐시는 없으므로 현재 소스를 직접 확인했다. 추가 SDK/의존성을 설치하지 않았다.
- 구현: 콜론형은 다음 본문으로 이어 실행하고 화살표형은 해당 본문 뒤 종료한다. 중간 default는 모든 상수 라벨 불일치 시 선택한다. 다중 라벨·문자열/enum·빈 switch·중첩 break/continue를 처리한다. 선택식은 한 번만 평가하고 case 상수는 실행식으로 방문하지 않는다. CASE_TEST/CASE_MATCH/CASE_NO_MATCH는 정규화된 선택 근거이며 JVM의 실제 비교 순서가 아니다. 값 상태는 선택 전 상태와 이어 실행 상태를 합류하고 대입으로 이전 정의를 제거한다. 버전5·버전4 이하 stale.
- 실행 명령: `scripts/build-java-analysis.ps1`; `dotnet build src/SMSR.App/SMSR.App.csproj --no-restore -v minimal`; `dotnet publish src/SMSR.App/SMSR.App.csproj -c Release --no-restore -o artifacts/live-p2-java-switch -v minimal`; graph-runtime Python의 Java switch/bundle/values/summaries/control 검사; 게시 Java 클래스 지정 회귀·Python keyword·TS switch values·16언어 구문·runtime 보호 검사; 게시 앱7종 자체검사·`scripts/test-mcp-stdio.ps1`·`scripts/verify-graph-backup.py`; HTTP/읽기 전용 SQLite/브라우저·표/크기/공백 검사.
- 검증 결과: 콜론28배치84경로·화살표4배치12경로를 독립 본문 실행 순서/반환 영향 정답과 대조했다. 그룹 라벨·default 위치/없음·문자열/enum·선택식 대입·중첩 전이·조기 반환·직접 호출 전달/영향 제거·명시 throw 제어/값 미지원 분리·switch 식 미지원·중복 라벨 진단·원문 비밀 제외·결정성 통과. 기존1000그래프 후지배 및 Java/Python/JS/TS/16문법/Cypher 보호 회귀 통과. 대상 코드는 실행하지 않았다.
- 저장 계약: Debug 및 게시 고급 검사에서 HTTP/MCP/SQLite의 pick 입력1/2·CASE_MATCH/CASE_NO_MATCH 반환, erase 빈 영향, rule2라벨을 검증했다. 이전 버전4 stale·원문/경로/해시/삭제 보호 유지. 빌드 경고/오류0·게시 앱7종 종료0·stdio44도구 통과.
- 실제 적용: 경로와 포트 소유를 재확인한 PID58048만 종료 후 PID38660 `artifacts/live-p2-java-switch/SMSR.App.exe --background --ensure-server` 숨김 실행. 127.0.0.1:49783 유지. 리비전44(623파일/1323노드/756관계), Java버전5·진단0·stale=false. 실제 SQLite에서 switch5메서드와 relayed 영향1/2·간선2, erased 빈 영향·간선0을 대조했다. Computer Use 화면의 두 선택 전이와 반환 위치 일치·브라우저 경고/오류0, `smsr-p2-java-switch.png` 저장.
- 데이터 보호: 운영 DB 미교체. 전후 `verification-backups/20260928-214124-859669`, `20260928-214309-485855` 별도 복원의 무결성·외래키·전체 테이블 행수 일치. 원문 질문 제외·전역 Git 훅 미적용·SDK 설치 결정 대기 유지.
- 환경/기록: Windows 기본 도구 경로 및 기존 Ubuntu22.04 WSL의 PATH/Go 기본 경로에서도 추가 분석 실행기를 확인하지 못했다. 발견된 MSVC는 아직 정밀 분석에 연결하지 않았다. 진행 문서의 실제 이름은 `docs/p2-progress-2026-09-28.md`이며 이전 응답의 짧은 파일명 링크는 잘못됐다. 계획서 데이터40행(머리글 포함41행) 모두3열, 신규 코드/예제7파일3000자 이하·공백 검사 통과. 초기 파일명 추정 실패 후 실제 목록으로 바로잡았다.
- 남은 위험: 정상 흐름 MAY 후보이며 실제 조건 만족성·null/언박싱/암묵적 예외·종료·힙/별칭·변환/부수효과의 증명이 아니다. switch 식/yield·패턴·라벨/예외 경로·전체16언어 정밀 PDG/taint·대형 부하·최종 수용은 미완료다. 빈 영향은 안전성 판정이 아니다.
- 다음 조치: 지원 언어의 미지원 의미 경계를 확장하고, 허용된 SDK/분석기 환경이 확보되면 나머지 언어의 실제 의미 해소와 정답집·앱 연결을 이어간다. 전체 P2 목표를 유지한다.

## 2026-09-29 - P2 대형 그래프 HTTP 성능·정답 게이트

- 변경 파일: `Mvp/GraphPerformanceSelfCheck.cs`, `GraphBenchmarkFixture.cs`, `GraphBenchmarkOracle.cs`, `GraphBenchmarkOracleSelfCheck.cs`, `GraphBenchmarkMetrics.cs`; `docs/graph-large-performance-2026-09-29.md`, 실제 저장소 성능 보고서·P2 보고서·계획서 MD/HTML·개발 이력.
- 변경 사유: 기존 대형 검사가 경로 발견만 검사하고 영향 집합을 대조하지 않으며 p95 목표 초과도 정상 종료하던 검증 공백을 보완한다. Ponytail/Karpathy에 따라 기존 fixture SQL·서버·HTTP 계약을 재사용하고 검증기만 분리했다. 신규 의존성/SDK를 추가하지 않았다.
- 구현: 운영 데이터와 별도의 새 임시 DB·loopback 서버에서 공개 경로/영향 HTTP를 호출한다. 독립 산술 BFS로 최단 거리·반환 간선/소유권·역방향 집합·노드 속성·리비전·잘림을 확인한다. 0~5단계 경로와 깊이 제한 실패, 깊이1~5 영향4/13/29/54/90개를 사용한다. 예열5회 후40회, JSON 해석까지 측정한다. p95는38번째 값이고 비유한/음수/잘못된 표본수·2초 초과를 실패로 처리한다.
- 실행 명령: `dotnet build src/SMSR.App/SMSR.App.csproj --no-restore -v minimal`; Debug `--graph-benchmark`; `dotnet publish src/SMSR.App/SMSR.App.csproj -c Release --no-restore -o artifacts/verify-p2-large-graph -v minimal`; 게시 `--graph-benchmark`3회·`--graph-self-test`; `scripts/test_graph_benchmark.py`; 파일 크기/공백/보고서 링크/계획서 표 검사.
- 검증 결과: 빌드 경고/오류0. 잘못된 끝점·비최단 경로·리비전·잘림·영향 누락/중복/속성·p95·표본수·NaN/음수·목표 초과 음성 검사 통과. Debug p95 경로86.87/영향98.18ms. 게시3회 경로89.69/96.09/89.77ms, 영향94.42/99.97/94.02ms·모든 정답/게이트 통과. 게시 그래프 회귀와 기존 소형 실제 저장소 benchmark 정답 검사 통과. 첫 게시 측정은 다른 그래프 자체검사와 겹쳤음을 보고서에 명시했다.
- 측정 근거: 각 반복 파일 기록5,000·노드50,000·관계200,000, DB 본체71,397,376바이트. apphost .exe 해시는 관리 코드 식별자가 아니므로 게시 `SMSR.App.dll` SHA256을 보고서에 기록했다. 표본 경로 길이0/2/3/4/5/깊이내미도달 각5개·길이1은10개. 영향 잘림은 노드100개 상한이 아니라 더 깊은 관계가 있는 깊이 제한이다.
- 데이터 보호: 운영 앱 PID38660 `artifacts/live-p2-java-switch`와 운영 DB를 교체하지 않았다. 새 검증용 임시 폴더만 자체 정리됐고 검사 후 해당 접두사 폴더가 남지 않았다. 실제 소스 색인/원문/질문/비밀 수집 없음. SDK 설치·전역 Git 훅 변경 없음.
- 과정/문서: 최초 패치의 같은 파일 삭제/추가 조합이 도구 형식 검사에서 거부돼 파일 전체 갱신 패치로 바로잡았다. 실행 오류는 없었다. 수정/신규 코드5파일 각각3000자 이하. 기존 직접 서비스 호출 측정과 새 HTTP 측정을 동일 지표로 비교하지 않으며, 실제 소형 보고서와 대형 합성 보고서를 분리했다.
- 남은 위험: 균일 차수 순환 합성 데이터이며 실제 파일5,000개 색인·허브/고밀도·동시 조회/색인·본문 임베딩/Cypher·메모리·장시간·WPF/브라우저 렌더링·냉캐시를 검증하지 않았다. 실패 중에는 이전 JSON이 남을 수 있어 종료코드/보고서시각을 먼저 확인한다. 이번 부하에서는 SQLite 유지지만 전용 DB 우월성 비교나 P2 전체 완료가 아니다. 16언어 정밀 PDG/taint도 잔여다.
- 다음 조치: 실제 대형/허브·동시 색인과 화면/본문 검색 부하를 각각 검증하고, 언어별 의미 해소/함수 간 분석 잔여를 이어간다. 실제 부하 목표 초과 시 실행 계획/인덱스를 먼저 점검한 뒤 같은 정답·데이터로 전용 DB와 비교한다.

## 2026-09-29 - P2 실제 5천 파일 색인 중 서버·WPF 큐 응답성

- 변경 파일: `Mvp/GraphResponsivenessSelfCheck.cs`, `GraphIndexFixture.cs`, `GraphIndexProbes.cs`; `docs/graph-index-responsiveness-2026-09-29.md`, 대형 조회 보고서·P2 진행 보고서·계획서 MD/HTML·개발 이력.
- 변경 사유: 기존1천문서 health 검사와 SQL 합성 조회 검사만으로는 실제5천파일 색인 중 응답성 근거가 부족했다. Ponytail/Karpathy에 따라 기존 색인 API·서버·Dispatcher와 Git을 재사용했다. 새 라이브러리/SDK 없이 검증 경로를 확장했다.
- 구현: 실제Markdown5천개에서5만노드/20만관계를 색인한다. 새 임시 저장소와 DB를 분리하고 최초/변경 없음 요청의 개수·리비전·변경 수·무결성을 검사한다. 경계2파일/18제목의 링크·소유 경로·줄 번호를 대조한다. 독립 HTTP/WPF Input 큐 측정에서 최소5표본·유한 비음수·최대2초/200ms를 요구하며 p95 및 오류 표본 자체검사를 포함한다. 모든 비동기 작업 종료 후 검증 임시 폴더만 정리한다.
- 실행 명령: `dotnet build src/SMSR.App/SMSR.App.csproj --no-restore -v minimal`; Debug `--graph-responsiveness-self-test`; `dotnet publish src/SMSR.App/SMSR.App.csproj -c Release --no-restore -o artifacts/verify-p2-index-responsive -v minimal`; 게시 `--graph-responsiveness-self-test`3회·`--graph-self-test`·`--graph-benchmark`; 파일 크기/공백/계획서 표·링크 검사.
- 검증 결과: 빌드/게시 경고·오류0. 게시3회 관계·무결성·리비전·응답성 게이트 통과. 최초색인12.09/12.90/8.88초, 변경없음1.72/1.73/1.70초. HTTP 최대134.32ms, WPF 큐 최대16.82ms. 각DB 본체145,391,616바이트. 기존 그래프 회귀 및 대형 HTTP 검사 통과(경로p95 88.38ms, 영향90.00ms). Debug는 최종 관계 표본 추가 전 검사이므로 별도 보고했다.
- 데이터 보호: 운영 앱 PID38660 `artifacts/live-p2-java-switch`와 DB 미교체. 자체 임시 폴더 삭제 완료·잔여 없음. 원문 질문 로그·전역 훅 설정·SDK 설치 없음. 새 코드/수정 코드3파일 각각3000자 이하. 게시 관리DLL 해시를 보고서에 기록했다.
- 남은 위험: Markdown 균일 링크 부하이며 사용자 실제 저장소/언어 컴파일러 부하를 대표하지 않는다. WPF 빈 Input 큐이며 창·렌더링·실제 입력 검증은 아니다. 동시 경로/영향 조회, 파일 변경/삭제, 취소/장시간/메모리/허브·냉캐시와 전체16언어 정밀 PDG/taint·P2 최종 수용은 잔여다. 실패 시 이전JSON이 남을 수 있어 종료코드와 시각을 확인한다.
- 다음 조치: 실제 화면과 조회/색인 동시 부하, 언어별 의미 해소 잔여를 각각 검증한다. SQLite 유지 및 신규SDK 설치 결정 대기·전역 훅 미적용을 보존한다.

## 2026-09-29 - P2 Java switch 식·yield 값/제어 흐름

- 변경 파일: `SMSR.JavaAnalysis/ControlSwitch.java`, `ControlStatements.java`, `ControlOperands.java`, `ValueExpressions.java`, `ValueStatements.java`, 신규 `ValueSwitchExpression.java`; `GraphRuntime/test_java_yield.py`, `test_java_yield_paths.py`, `test_java_yield_boundaries.py`, 기존 switch 검사; `Mvp/GraphJavaYieldSelfCheck.cs`·Java 분석 버전/통합/저장 검사/화면 안내; `samples/graph-java/Expressions.java`·README·추적 가이드·P2 보고서·계획서 MD/HTML·개발 이력.
- 변경 사유: 기존 Java switch 문 분석에서 빠진 식 결과와 yield의 흐름을 연결한다. Ponytail/Karpathy에 따라 기존 case 검색·상태 합류·반복·제어 후지배·호출별 반환 요약·DB/UI를 재사용했다. Graphify 캐시가 없어 현재 소스를 확인했으며 새 그래프 구축이나 SDK 설치는 하지 않았다.
- 구현: 직접 화살표 결과식과 블록/콜론 yield의 값 노드를 가장 가까운 SWITCH_RESULT로 연결한다. yield 지점의 지역 변수 상태를 합류하고 정상 제어는 식 이후로 연결한다. 중첩 switch 문/조건/반복 안의 yield를 보존하며 안쪽 식의 yield는 바깥 식을 종료하지 않는다. enum 식의 일치 없는 경로는 합성 종료로 연결한다. 선택 조건은 데이터 의존에 포함하지 않는다. Java 분석 버전6, 버전5 이하 결과 stale.
- 실행 명령: `scripts/build-java-analysis.ps1`; `dotnet build src/SMSR.App/SMSR.App.csproj --no-restore -v minimal`; `dotnet publish src/SMSR.App/SMSR.App.csproj -c Release --no-restore -o artifacts/live-p2-java-yield -v minimal`; 게시 GraphRuntime와 JavaAnalysis를 지정한 yield/switch/control/values/summaries/bundle 검사; `test_language_coverage.py`; 게시 앱7종 자체검사·`scripts/test-mcp-stdio.ps1`; `scripts/verify-graph-backup.py`; HTTP·읽기 전용 SQLite·Computer Use 화면·공백/크기/계획 표 검사.
- 검증 결과: 신규30배치/90경로를 독립 선형 case 목록과 대조했다. 중첩 yield 소유권·반복/조건·상태 합류·입력 제거·호출 반환2개·비밀 제외·결정성·컴파일 오류 검사를 통과했다. 기존 switch96경로, 후지배1000그래프, Java 호출/값/선언 및16문법 구문·UTF16 정답집 통과. 빌드 경고/오류0, 게시7종 종료0, MCP44도구 통과. HTTP/실제 MCP/SQLite에서 결과 노드·소유권·입력 의존·버전/원문/해시/삭제 보호 검증.
- 실제 적용: 사전 경로·포트 소유가 일치한 PID38660만 종료하고 `artifacts/live-p2-java-yield/SMSR.App.exe --background --ensure-server`를 숨김 실행했다. 새 PID27100·127.0.0.1:49783 유지, 운영 DB 미교체. 색인 리비전45(637파일/1355노드/781관계), 예제4파일의 버전6·stale=false·진단0. HTTP/SQLite/화면에서 Expressions.pick 입력1/2, nested 입력2, relayed 입력1/2·호출 간선2개, erased 빈 영향 확인.
- 데이터 보호: 전후 `verification-backups/20260928-221637-357151`, `20260928-221821-800173` 별도 복원본의 무결성·외래키·전체 테이블 행 수 일치. 운영 원본 교체/원문 질문 기록/전역 Git 훅/SDK 설치 없음. Computer Use 화면 `smsr-p2-java-yield.png` 저장, 브라우저 경고/오류0.
- 과정: 존재하지 않는 검증 파일명 조회1회와 Windows 경로 glob 검색2회는 실제 파일/디렉터리+필터로 수정했다. 첫 브라우저 click 뒤 요청 시작이 관찰되지 않아 저장 결과 조회가 없음을 확인한 후, 현재 화면의 버튼에서 Enter로 실행했다. 처리 중→완료·실제 저장을 확인했으며 미확인 요청을 중복 실행하지 않았다. 코드/정답집 실패는 없었다. 신규 코드와 Java 수정 파일은3000자 이하, 기존 통합 C# 파일은 해당 패턴을 유지했다.
- 남은 위험: 정상 흐름 MAY 후보이며 조건 만족성·변환/예외·힙/별칭·실제 종료의 증명이 아니다. throw는 정상 CFG 종료만 제공하고 해당 메서드 값 분석은 미지원이다. try/finally·패턴·전체16언어 의미 해소/정밀 PDG/taint·P2 최종 수용은 잔여다. 앞선 버전 절은 이력이며 버전6 범위가 최신이다.
- 다음 조치: 언어별 미지원 의미 경계를 계속 구현하고 실제 정답집과 비교한다. SDK 설치 범위 답변 대기·전역 훅 미적용·SQLite 유지 결정은 보존한다.

## 2026-09-29 - P2 설치된 JDT 실제 LSP 연결 검증

- 변경 파일: `Mvp/GraphLspProtocol.cs`, `GraphLspDefinitions.cs`, `GraphLspSession.cs`, `GraphLspSelfCheck.cs`, 신규 `GraphJdtProfile.cs`, `GraphJdtFixture.cs`, `GraphJdtSelfCheck.cs`; `GraphRuntime/test_lsp_server.py`, `App.xaml.cs`; JDT 검증 보고서·P2 진행 보고서·계획서 MD/HTML·추적 가이드·개발 이력.
- 변경 사유: 모의 서버만 검증한 공통 LSP를 기존 설치 도구로 실제 검증한다. Ponytail/Karpathy에 따라 기존 프로토콜·원문/해시·경로 검증·수명 주기를 재사용했다. 신규 SDK를 설치하지 않고 VS Code Java 확장에 포함된 JDT/JRE를 사용한다.
- 구현: 내부 검사 옵션으로 설치 확장 절대 경로를 받는다. 임시 Eclipse 프로젝트·Java2파일·별도 SQLite를 생성하고 서버마다 설정/홈/데이터를 분리한다. 초기화 옵션을 전달하고 자식의 지정 JVM/소켓 환경 변수를 제거한다. 자동 빌드/가져오기 비활성화, 소스 불변·프로젝트 `.class` 부재를 검사한다. HTTP/MCP/UI 연결은 추가하지 않았다.
- 실행 명령: `dotnet build src/SMSR.App/SMSR.App.csproj --no-restore -v minimal`; Debug `--graph-jdt-self-test`; `dotnet publish src/SMSR.App/SMSR.App.csproj -c Release --no-restore -o artifacts/verify-p2-jdt -v minimal`; 게시 `--graph-jdt-self-test <설치 확장 경로>`·`--graph-lsp-self-test`·`--graph-self-test`·`--graph-advanced-self-test`; 설정/관리DLL 해시·임시 폴더/프로세스·공백/파일 크기/계획서 표 검사.
- 검증 결과: 빌드/게시 성공. Debug 실제 JDT3건 통과(총209.38초). 최종 게시본은 잘못된 JVM 옵션·클래스 경로·소켓 환경을 상위 검사 프로세스에 주입한 상태에서3건 통과(총203.47초). 정수/문자열 오버로드와 지역 변수 정의, 한글 파일명·CRLF·emoji UTF-16 범위 일치. 개별67,728/68,034/67,704ms는 초기화부터 서버 종료까지이며 요청 단독 지연이나 p95가 아니다. 최종 게시본에 소스 보존·미빌드 검사를 포함했다. 공통 LSP 옵션 전달/거부/경계/취소 회귀 및 기본·고급 그래프 자체검사 종료0.
- 식별/보호: JDT core1.61.0.202609021834/JRE21.0.12.1. 설치 config.ini 전후 해시 동일. 관리DLL 해시와 정확한 범위는 `graph-jdt-verification-2026-09-29.md`에 기록했다. 자체 임시 프로젝트는 정리 완료·시험 Java 프로세스 잔여 없음. 운영 앱 PID27100과 DB 미교체, 전역 환경/훅/원문 질문 로그 변경 없음. 설치 실행기/라이브러리를 앱에 복사하지 않았다.
- 과정: 문서 검색에 Windows 파일명 glob 경로를 쓴 조회1회가 실패해 디렉터리와 필터로 수정했다. 실제 검사 실패는 없었다. 신규3개 코드 파일은3000자 이하이며 기존 LSP/앱 통합 파일은 구조를 유지했다. 문서는 코드 크기 목표 예외다.
- 남은 위험: 통제된 소형 Eclipse 표본이며 Maven/Gradle·실제 클래스 경로·임의 프로젝트 빌드/네트워크 차단을 검증하지 않았다. 전체 세션 종료가 느리고 제품 경로는 미연결이다. 다른15언어/문법 실제 프로필·전체 의미 정확도·함수 간 PDG/taint와 P2 최종 수용은 잔여다. 이전 성공 JSON이 남을 수 있으므로 종료코드/시각을 확인한다.
- 다음 조치: 신뢰된 프로필과 수명 주기/지연을 검증한 뒤 제품 경로를 연결한다. 다른 언어별 정답집과 프로젝트 의미 분석을 계속 보강하며 신규 SDK 설치 결정 대기를 유지한다.

## 2026-09-29 - P2 Java LSP 제품 연결·사용자 요청 일시정지

- 변경 파일: `GraphLspProtocol.cs`, `GraphLspSession.cs`, `GraphLspJob.cs`, 신규 `GraphLspJobExit.cs`·`GraphLspExitSelfCheck.cs`, LSP 모의 검사; `GraphJdtInstallation/Request/Project/Execution/Query/Result/Page/Tools.cs`, JDT 제품/저장/경계/정리 검사, 기존 프로필; `App.xaml.cs`, 고급 API/화면/렌더링·MCP 게이트웨이·stdio 검사; `samples/graph-jdt`·P2 보고서·계획서 MD/HTML·추적 가이드·검증/재계획 문서.
- 변경 사유: 내부 JDT 검증을 실제 정의 조회·저장 기능으로 연결한다. Ponytail/Karpathy에 따라 기존 LSP·원문/해시·SQLite 파생 저장·HTTP/MCP·화면 패턴을 재사용했다. 설치된 확장만 사용하며 신규 SDK/라이브러리를 설치하거나 재배포하지 않았다.
- 구현: 기본 사용자 VS Code Red Hat Java1.56.0 경로와 manifest 식별자·연결 파일을 확인한다. 서명 검증은 아니므로 설치 폴더 자체를 신뢰해야 한다. 명시 파일/중첩 없는 소스 루트·UTF16 위치를 검증하고 Java 파일 복사본만 별도 프로젝트로 연다. 원본 설정은 복사하지 않으며 알려진 민감 패턴은 복사 전 거부한다. 프로필·좌표/해시·리비전·시각/제외 개수를 저장하고 원문/리터럴은 저장하지 않는다. 오래됨·삭제·실패 시 이전 결과 보존을 제공한다. 전체 실제 프로젝트 의미 해소가 아니라 입력 범위 정의 후보다.
- 종료/정리: shutdown 응답/exit 이후2초 대기 뒤 남은 프로세스를 종료하고 Job 전체 비활성을 확인한다. 소유 임시 파일 잠금만250/500ms 후2회 재시도하며 지속 오류는 실패한다. 정리 성공 후 DB에 저장한다. 초기3회 동일 실패에서 중단·재계획했고 Job 대기만으로는 해결되지 않아 파일 잠금 정책을 별도로 검증했다. 원인·시도·근거는 `jdt-product-debug-2026-09-29.md`에 남겼다.
- 실행 명령: Debug 빌드·LSP/JDT 실연결/제품 자체검사; `dotnet publish src/SMSR.App/SMSR.App.csproj -c Release --no-restore -o artifacts/live-p2-jdt -v minimal`; 게시 `--graph-lsp-self-test`·`--graph-jdt-product-self-test`·`--graph-self-test`·`--graph-advanced-self-test`·`--tracking-self-test`·`--self-test`·`--codex-config-self-test`·`--oauth-self-test`; `scripts/test-mcp-stdio.ps1 -ExpectedToolCount 46`; `scripts/verify-graph-backup.py`; 실제 브라우저·HTTP·공백/크기/표 검사.
- 검증 결과: 빌드 경고/오류0, 게시8종 종료0, stdio46도구. 실제 직접/HTTP/MCP 정답·한글/CRLF/emoji·선택 루트·원본 설정 보존·원문 미저장·민감 파일 거부·취소·잘못된 입력·오래됨·실패 보존·프로젝트 삭제·임시 잠금 회귀 통과. 실제3개 세션의 종료 포함 시간203.47→27.17초(Debug 비교)이며 요청 단독 p95가 아니다. MCP 검사 실패는 도구 구현이 아닌 시험 인자의 대소문자 계약을 수정했다. PowerShell 스크립트 뒤 빈 LASTEXITCODE 판정은 오탐이어서 실제 스크립트 성공/46개 응답을 재확인했다.
- 실제 적용: 사전 백업/별도 복원 무결성·외래키·행 수 검증 후 경로/포트가 일치한 PID27100만 종료했다. 새 앱 `artifacts/live-p2-jdt/SMSR.App.exe --background --ensure-server`, PID34100·127.0.0.1:49783. 적용 색인 리비전46(659파일/1397노드/804관계). 운영 DB는 교체하지 않았다. 관리DLL SHA256 `895F182A870367BBE9ABC337FCEAE7E3CB8B01C7FA492357C14FF51AD7AE610A`.
- 화면 검증: 정수/문자열 조회가 Choices.java의4줄23–27열/5줄26–30열(1부터·끝 제외)로 각각 연결됐고 저장 재조회도 동일했다. 처리 중→완료와 후보/프로필/시각 표시, 브라우저 경고·오류0. 첫 click에 상태 변화가 없어 결과 공란·버튼 활성으로 요청 미시작을 확인한 뒤 Enter로 실행했다. `smsr-p2-jdt-definition.png` 저장. Computer Use 스킬은 실제 UI 확인에만 사용했다.
- 데이터 보호/잔여 정리: 사전 `verification-backups/20260928-225908-370758` 및 사후 `verification-backups/20260928-230431-115315` 별도 복원본에서 무결성·외래키·전체 테이블 행 수 일치. 운영 SQLite 정의 보고서2건도 화면 좌표와 대조했다. 실패 당시 합성 임시 폴더4개는 삭제 명령이 도구 정책으로 거부되어 남겼으며 우회하지 않았다. 이후 성공 검사/실사용 임시 폴더는 자체 정리된다. 원본 삭제·전역 Git 훅 적용·SDK 설치·원문 질문 로그 기록 없음. 신규 코드 파일은3000자 이하, 기존 큰 통합 파일은 패턴 유지.
- 남은 위험: 알려진 비밀 패턴은 완전한 DLP가 아니며 임시 복사본은 실행 중 존재한다. 프로필은 한 설치 버전만 검증됐다. 프로젝트 설정/외부 의존성·정밀 진단·실제 디스패치·16언어 전체 의미 정확도·정밀 PDG/taint·대형/동시/본문 성능·최종 설치/사용자 수용은 미완료다. SQLite 유지, Go 등 신규 SDK 설치 승인 대기.
- 다음 조치: 사용자의 현재 작업 완료 후 중단 요청에 따라 이 범위의 검증/문서를 마감하고 목표를 일시정지한다. 자동으로 새 기능에 착수하지 않는다. 재개 항목은 P2 보고서 첫 절의 목록을 따른다.

## 2026-09-29 - 로컬 코드 탐색 화면 시안

- 변경 파일: `docs/graph-explorer-ui-preview-2026-09-29.html`, `docs/development-log.md`.
- 변경 사유: 긴 설명과 언어별 입력이 한 페이지에 나열된 고급 화면을 사용자가 이해하기 어렵다는 피드백에 따라, 제품 수정 전에 범주별 탐색 화면을 검토할 수 있게 한다.
- 실행 명령: 현재 `GraphAdvancedPage`/렌더링·색인 화면 확인, `git status --short`, `node --version`, HTML 인라인 JS 구문 검사, 시안 구조 검사, `git diff --check`.
- 검증 결과: 시안에 찾기·관계 탐색·코드 분석·고급 도구 4범주, 예시 검색·결과 선택·관계 그래프/목록 전환·근거 표시를 구현했다. JS 구문 검사와 정적 구조 검사가 통과했고 백엔드 호출은 없다. 기존 제품 화면과 DB는 변경하지 않았다.
- 남은 위험: 로컬 `file:` HTML 미리보기는 브라우저 보안 정책으로 차단되어 시안의 시각적·모바일 사용성은 브라우저에서 검증하지 못했다. 화면의 관계는 예시 데이터이며 실제 분석 정확도를 뜻하지 않는다.
- 다음 조치: 사용자가 샘플 배치를 검토한 뒤 승인한 범위만 실제 `/graph/advanced`에 연결하고, 실제 화면·키보드·모바일 사용성을 확인한다.

## 2026-09-29 - 코드 탐색 화면 시안 용어·추가 기능 수정

- 변경 파일: `docs/graph-explorer-ui-preview-2026-09-29.html`, `docs/development-log.md`.
- 변경 사유: 시안의 `관계 탐색`·`코드 분석`·`고급 도구`와 코드 내부 이름이 사용자에게 여전히 어렵다는 피드백을 반영했다.
- 실행 명령: 시안 HTML/JS 확인, `node -e` 인라인 JS 구문·용어 검사, 범주/접힘 카드/백엔드 호출/공백 정적 검사, `git diff --check -- docs/development-log.md`.
- 검증 결과: 범주를 `찾기`·`연결 보기`·`코드 흐름 보기`·`추가 기능`으로 바꾸고 코드 역할을 먼저 표시한다. 추가 기능 3가지는 사용자의 상황을 제목으로 둔 접힘 카드로 바꿨다. Cypher 입력은 내부 `직접 질의 쓰기`에서만 노출한다. 시안 JS 구문·용어 검사를 통과했고 백엔드 호출은 없다.
- 남은 위험: 브라우저의 `file:` 자동 미리보기 제한 때문에 실제 시각/반응형·클릭 동작은 사용자가 열린 로컬 파일을 새로고침해 검토해야 한다. 카드 동작은 제품 연결 전 예시다.
- 다음 조치: 시안에 대한 사용자 의견을 받아 실제 화면의 범주/기본 흐름을 확정한다.

## 2026-09-29 - 연결 보기와 코드 흐름 보기 시안 구분

- 변경 파일: `docs/graph-explorer-ui-preview-2026-09-29.html`, `docs/development-log.md`.
- 변경 사유: 두 메뉴가 같은 관계 그래프를 재사용해 의미 차이가 화면에서 드러나지 않았다.
- 실행 명령: 시안 HTML/JS 확인, `node -e` 스크립트 구문·필수 요소 검사, 그래프/단계/백엔드 호출/공백 정적 검사, `git diff --check -- docs/development-log.md`.
- 검증 결과: 연결 보기는 파일·문서·코드 간 4노드 예시를 유지한다. 코드 흐름 보기는 `변경 파일 반영` 내부의 6단계·변경 여부 분기 그래프와 단계별 근거를 별도로 표시한다. 그림/목록 전환도 현재 범주에 맞춰 전환한다. JS 구문·요소 검사가 통과하고 백엔드 호출은 없다.
- 남은 위험: 예시 흐름은 제품 분석 결과가 아니며 브라우저 `file:` 자동 미리보기 제한으로 시각·상호작용 검증은 사용자가 열린 파일을 새로고침해 확인해야 한다.
- 다음 조치: 사용자가 두 화면의 구분과 용어를 확인한 뒤 실제 제품 연결 범위를 결정한다.

## 2026-09-29 - 웹 공통 UI 인터랙션 시안

- 변경 파일: `docs/graph-explorer-ui-preview-2026-09-29.html`, `README.md`, `docs/development-log.md`.
- 변경 사유: 코드·문서 화면에만 국한되지 않는 웹 공통 화면 형식과 부드러운 상호작용을 먼저 확인할 수 있도록 했다.
- 실행 명령: `Get-Content`로 기존 웹 스타일과 thinking-orbs 원본·라이선스 확인, `node -e`로 시안 JavaScript 구문과 필수 요소 검사, `git diff --check`.
- 검증 결과: 작업 현황·코드 탐색이 같은 화면 형식을 공유한다. 관계 선택 시 연결선과 상세 정보를 강조하고, 코드 흐름의 변경된 경로를 재생·일시정지할 수 있다. 움직임 감소 설정에서는 자동 재생하지 않으며 탭을 숨기면 재생을 멈춘다. 시안 JavaScript 구문과 주요 요소 검사를 통과했다.
- 남은 위험: 시안은 예시 데이터이며 실제 제품 화면·서버 데이터와 연결되지 않았다. 로컬 `file:` URL의 자동 브라우저 검사 제한으로 클릭·시각 검증은 사용자가 새로고침해 확인해야 한다.
- 다음 조치: 공통 웹 UI 원칙을 확정한 뒤 대시보드와 코드·문서 화면에 동일한 구성·움직임을 적용한다.

## 2026-09-29 - 실제 웹 대기 상태에 thinking-orbs 적용

- 변경 파일: `src/SMSR.App/WebAssets/thinking-orbs-engine.js`, `src/SMSR.App/WebAssets/thinking-orbs-LICENSE.txt`, `src/SMSR.App/WebAssets/smsr-loading-orb.js`, `src/SMSR.App/SMSR.App.csproj`, `src/SMSR.App/Mvp/LocalServerEndpoints.cs`, `src/SMSR.App/Mvp/GraphPage.cs`, `src/SMSR.App/Mvp/GraphAdvancedPage.cs`, `src/SMSR.App/Mvp/DashboardPage.cs`, `src/SMSR.App/Mvp/DashboardLiveUpdates.cs`, `src/SMSR.App/Mvp/DashboardPanels.cs`, `scripts/test_web_loading_orb.mjs`, `README.md`, `docs/development-log.md`.
- 변경 사유: 시안의 장식용 회전점과 달리 실제 검색·색인·분석·작업 지시 대기 시점에만 상태별 오브를 표시한다. SMSR의 HTML 웹 화면에 React 런타임을 추가하지 않는다.
- 실행 명령: 제공된 `thinking-orbs` 저장소에서 `npm ci --ignore-scripts --no-audit --no-fund`, `npm run build`; SMSR에서 `node --check`, `node scripts/test_web_loading_orb.mjs`, `dotnet build src/SMSR.App/SMSR.App.csproj -c Debug --no-restore`, `dotnet publish src/SMSR.App/SMSR.App.csproj -c Debug --no-restore -p:PublishSingleFile=false -o artifacts/orb-publish-smoke`.
- 검증 결과: React 비의존 엔진과 MIT 전문을 앱 빌드·게시 출력 폴더에 포함했다. 250ms 이내에 끝나는 요청은 오브를 표시하지 않고, 긴 요청은 표시 후 종료 시 제거한다. 화면 밖·숨긴 탭에서 정지하고 움직임 감소 설정에는 정적 화면을 표시한다. JavaScript 검사, Debug 빌드와 게시가 통과했다.
- 남은 위험: 실행 중인 SMSR 앱은 새 빌드로 재시작하지 않았으므로 현재 브라우저 서버 화면에는 아직 반영되지 않았다. 실제 브라우저 시각 검증은 재시작 후 필요하다.
- 다음 조치: 앱 재시작 후 검색·색인·분석의 대기/완료/오류 상태와 밝은·어두운 테마를 수동 확인한다.

## 2026-09-29 - 공통 웹 탐색과 실제 데이터 그래프 화면

- 변경 파일: `src/SMSR.App/Mvp/WebNavigation.cs`, `GraphExplorerPage.cs`, `DashboardPage.cs`, `GraphPage.cs`, `GraphAdvancedPage.cs`, `GraphAdvancedEndpoints.cs`, `GraphSourcePage.cs`, `GraphEndpoints.cs`, `LocalServerEndpoints.cs`, `GraphIndexSelfCheck.cs`, `src/SMSR.App/WebAssets/graph-explorer.js`, `graph-explorer-flow.js`, `scripts/test_graph_explorer.mjs`, `README.md`, `docs/development-log.md`.
- 변경 사유: 승인된 시안의 공통 범주를 제품 웹 화면에 적용하고, 예시 그래프 대신 저장된 색인·관계·코드 분석 결과를 보여준다. 기존 상세 기능은 유지한다.
- 실행 명령: `git status --short`, `dotnet build src/SMSR.App/SMSR.App.csproj --no-restore -v:q`, `node --check src/SMSR.App/WebAssets/graph-explorer.js`, `node --check src/SMSR.App/WebAssets/graph-explorer-flow.js`, `node scripts/test_graph_explorer.mjs`, `node scripts/test_web_loading_orb.mjs`, `SMSR.App.exe --graph-self-test` (숨김 프로세스 종료 코드 확인), `git diff --check`.
- 검증 결과: 빌드 경고·오류 0개, JavaScript 구문 검사·코드 흐름 검사·대기 오브 검사와 서버 통합 자체검사가 통과했다. 관계는 `/api/graph/context`, 흐름은 `/api/graph/analysis`의 저장 결과를 사용하며 필요 시에만 분석을 시작한다.
- 남은 위험: 실행 중인 앱은 재시작하지 않아 현재 서버 화면의 실제 클릭·시각 검증은 미완료다. 관계 그래프는 선택 항목의 직접 이웃 최대 8개, 흐름은 한 함수의 문장 최대 9개를 요약한다.
- 다음 조치: 새 빌드로 앱을 재시작하고 실제 프로젝트에서 검색·그래프·모바일 폭·테마를 확인한다.

## 2026-09-29 - 실행 중인 앱에 새 웹 화면 반영

- 변경 파일: `src/SMSR.App/Mvp/GraphExplorerPage.cs`, `docs/development-log.md`; 새 배포 폴더 `artifacts/live-web-ui-20260929-r2`.
- 변경 사유: 실행 중인 구버전 앱을 새 웹 UI 게시본으로 전환하고, 실제 브라우저에서 확인된 중간 폭 그래프 잘림을 수정했다.
- 실행 명령: `dotnet publish src/SMSR.App/SMSR.App.csproj -c Debug --no-restore -p:PublishSingleFile=false -o artifacts/live-web-ui-20260929-r2 -v:q`, 게시본 `--graph-self-test`, 구버전 실행 프로세스 확인·종료, 사용자 DB와 설정 백업, 새 앱 `--background --ensure-server` 실행, 로컬 HTTP·브라우저 확인.
- 검증 결과: 게시본 자체검사 종료 코드 0, 새 앱 프로세스와 `127.0.0.1:49783` 서버 확인. `/graph/explore`·자산 200 응답, SMSR 색인 검색 및 관계 조회, 브라우저에서 관계 그래프 표시와 넓어진 배치 확인. 코드 흐름의 저장 결과 부재 안내도 확인했다.
- 남은 위험: 실제 코드 파일의 `분석·저장` 실행과 작은 모바일 폭의 시각 검증은 수행하지 않았다. 기존 배포 폴더 `artifacts/live-p2-jdt`와 전환 직전 데이터 백업 `%LocalAppData%\SMSR\verification-backups\web-ui-20260929-1046`, `web-ui-r2-20260929-1050`은 복구용으로 보존했다.
- 다음 조치: 필요 시 코드 분석을 명시 실행하고 모바일 폭을 확인한다. 앱 업데이트가 필요한 경우 새 배포 폴더를 기준으로 한다.

## 2026-09-29 - 관계 그래프 이름과 추가 기능 화면 정리

- 변경 파일: `src/SMSR.App/WebAssets/graph-explorer.js`, `graph-explorer-labels.js`, `src/SMSR.App/Mvp/GraphExplorerPage.cs`, `GraphAdvancedPage.cs`, `GraphAdvancedLayout.cs`, `LocalServerEndpoints.cs`, `GraphIndexSelfCheck.cs`, `scripts/test_graph_explorer_labels.mjs`, `docs/development-log.md`.
- 변경 사유: 관계 그래프의 파일명 강제 잘림과 내부 영문 표기를 없애고, 관계 그림의 움직임과 추가 기능의 사용성을 개선한다.
- 실행 명령: `node scripts/test_graph_explorer_labels.mjs`, `node scripts/test_graph_explorer.mjs`, `node --check src/SMSR.App/WebAssets/graph-explorer.js`, `dotnet build`, `dotnet publish`, 게시본 `--graph-self-test`, 로컬 서버·브라우저 확인.
- 검증 결과: 파일명 전체가 SVG 여러 줄과 접근성 이름에 표시되며 관계·종류가 한국어로 나온다. 노드·선 애니메이션을 확인했고, 추가 기능 8개 카드의 펼침과 기존 입력을 확인했다. 실행 앱을 `artifacts/live-web-ui-20260929-r4`로 교체했다.
- 남은 위험: 매우 긴 파일명은 SVG에서 여러 줄이 되어 그래프 영역을 스크롤해야 할 수 있다. 움직임 감소 설정에서는 애니메이션을 최소화한다.
- 다음 조치: 실제 파일 수가 많은 프로젝트에서도 가독성을 확인한다.

## 2026-09-29 - 작업 이력 저장과 선택 기록 보기

- 변경 파일: `src/SMSR.App/Mvp/Contracts.cs`, `LocalServerEndpoints.cs`, `DashboardPanels.cs`, `DashboardPage.cs`, `DashboardStyles.cs`, `DashboardLiveUpdates.cs`, `DashboardTimeline.cs`, `TrackingContractSelfCheck.cs`, `scripts/test_dashboard_timeline.mjs`, `docs/graph-tracking-guide.md`, `docs/development-log.md`.
- 변경 사유: 배경·진행 방식·최종 결과가 누락된 작업 그래프에서 사용자가 내용을 보완하고 저장할 수 있게 하며, 순서 슬라이더가 선택 기록만 보여주도록 한다.
- 실행 명령: `dotnet build src/SMSR.App/SMSR.App.csproj -c Debug --no-restore -v:q`, `node scripts/test_dashboard_timeline.mjs`, `dotnet publish`, 게시본 `--tracking-self-test`, 로컬 `/api/context` 출처 검사와 저장 확인, 브라우저 슬라이더·편집 확인.
- 검증 결과: 빌드·추적 자체검사·슬라이더 검사를 통과했다. 출처 없는 저장은 401이고 같은 출처 저장은 DB에 반영된다. 완료된 관계 그래프에 실제 배경·방식·결과를 입력해 화면에서 확인했다. 7/7에서 3/7로 바꾸면 해당 기록 한 건만 보이며 저장 후에도 선택 위치가 유지된다. 실행 앱을 `artifacts/live-web-ui-20260929-r5`로 교체했다.
- 남은 위험: 근거가 없는 과거 그래프의 배경·결과는 자동 추측하지 않으며 수동 보완이 필요하다. 각 기존 배포본과 `%LocalAppData%\SMSR\verification-backups\web-ui-r3-20260929-1108`, `web-ui-r4-20260929-1115`, `web-ui-r5-20260929-1124` 백업은 복구용으로 보존했다.
- 다음 조치: 새 작업 그래프 생성 시 배경·진행 방식을 즉시 기록하고 종료 시 결과를 입력한다.

## 2026-09-29 - 탐색 범례·이미지 관계와 작업 이력 조작 보완

- 변경 파일: `Mvp/GraphMarkdownSyntax.cs`, `GraphMarkdownSelfCheck.cs`, `GraphFileScanner.cs`, `GraphImageSelfCheck.cs`, `GraphExplorerPage.cs`, `GraphEndpoints.cs`, `GraphQueryService.cs`, `EventStoreGraphNodes.cs`, `EventStoreGraphFormat.cs`, `DashboardPage.cs`, `DashboardPanels.cs`, `DashboardStyles.cs`, `DashboardLiveUpdates.cs`, `DashboardTimeline.cs`, `DashboardTimelineStyles.cs`, `TrackingContractSelfCheck.cs`, `WebAssets/graph-explorer.js`, `graph-explorer-flow.js`, `graph-explorer-labels.js`, `README.md`, `docs/graph-tracking-guide.md`.
- 변경 사유: Markdown 머리말이 잘못된 제목 노드로 노출되는 문제를 고치고, 찾기·연결 보기의 파일 종류 구분과 이미지 관계를 추가했다. 코드 흐름의 지원 범위를 명확히 하고, 작업 이력을 직접 수정하며 선택 기록만 읽을 수 있게 했다.
- 실행 명령: `node scripts/test_dashboard_timeline.mjs`, `node scripts/test_graph_explorer_labels.mjs`, `node scripts/test_graph_explorer.mjs`, `node --check src/SMSR.App/WebAssets/graph-explorer.js`, `dotnet build src/SMSR.App/SMSR.App.csproj -c Debug --no-restore -v:q`, `dotnet publish` (r6), 게시본 `--tracking-self-test`, `--graph-self-test`, 로컬 프로젝트 재색인 및 브라우저 확인.
- 검증 결과: 빌드·스크립트·자체검사 통과. 실행 앱을 `artifacts/live-web-ui-20260929-r6`로 교체하고 SMSR 색인을 리비전 47로 갱신했다. 잘못된 YAML 제목은 사라졌고 이미지 7개가 메타데이터 노드로 검색된다. 찾기·연결 보기에는 종류 범례가, 코드 흐름 보기에는 범례가 없음을 확인했다. 연결선의 지속 애니메이션과 전체 문서 제목을 확인했다. 작업 이력의 두 번 클릭·포커스 이탈 자동 저장, 기록 번호 제거, 최하단 활동 접힘을 실제 화면과 새로고침으로 검증했다.
- 남은 위험: 이미지는 미리보기를 제공하지 않고 메타데이터만 보관한다. 코드 흐름은 실제 실행 순서가 아닌 지원 코드의 함수 내부 의존 관계이며 Markdown·HTML·이미지 분석에는 적용되지 않는다. `%LocalAppData%\SMSR\verification-backups\web-ui-r6-20260929` 백업을 보존했다.
- 다음 조치: 실제 사용 중 발견되는 파일 유형별 분류 오류와 좁은 화면의 긴 이름 배치를 계속 확인한다.

## 2026-09-29 - 관계 화면 간소화와 연결 문서 원문 열기

- 변경 파일: `src/SMSR.App/Mvp/GraphExplorerPage.cs`, `GraphIndexSelfCheck.cs`, `src/SMSR.App/WebAssets/graph-explorer.js`, `docs/development-log.md`.
- 변경 사유: 찾기·연결 보기의 관계 그래프 아래 번호 목록과 리비전·관계 개수·표시 제한 문구는 그래프를 중복 설명했다. 연결 노드를 누르면 그 항목의 관계로 이동하는 기존 동작을 유지하면서 원문 열기 동작을 알아보기 쉽게 했다.
- 실행 명령: `node --check src/SMSR.App/WebAssets/graph-explorer.js`, `node scripts/test_graph_explorer.mjs`, `node scripts/test_graph_explorer_labels.mjs`, `dotnet build src/SMSR.App/SMSR.App.csproj -c Debug --no-restore -v:q`, `dotnet publish` (r7·r8), 게시본 `--graph-self-test`, `python scripts/verify-graph-backup.py`, 같은 출처 `/api/graph/index`, 로컬 HTTP·브라우저 확인, `git diff --check`.
- 검증 결과: 빌드 경고·오류 0개와 스크립트·그래프 자체검사 통과. `artifacts/live-web-ui-20260929-r8` 실행 앱에서 중복 목록·기술 문구가 없고 관계선 설명은 툴팁에 남은 것을 확인했다. 연결 노드 클릭 시 해당 제목·원문 줄 위치로 이동하며 파란 `문서 원문 열기` 버튼을 표시한다. DB 백업·별도 복원 검사 후 SMSR 색인을 갱신했다. `development-log.md:607` 원문 페이지가 HTTP 200으로 열리고 해당 제목을 표시한다. 코드 흐름의 분석 목록은 유지했다.
- 남은 위험: 관계 그래프는 대표 이웃을 방향별 최대 4개 표시한다. 전체 관계는 기존 상세 도구에서 조회한다. 색인 이후 문서가 다시 바뀌면 기존 원문 링크는 갱신을 요구한다. DB 백업은 `%LocalAppData%\SMSR\verification-backups\20260929-025317-482288`에 보존했고 색인 형식은 변경하지 않았다.
- 다음 조치: 향후 코드 흐름 화면은 사람이 읽을 수 있는 함수·호출 중심 설계로 별도 검토한다.

## 2026-09-29 - 찾기 화면 통합과 작업 현황 복귀

- 변경 파일: `src/SMSR.App/Mvp/WebNavigation.cs`, `GraphExplorerPage.cs`, `GraphEndpoints.cs`, `GraphSourcePage.cs`, `GraphAdvancedPage.cs`, `GraphAdvancedLayout.cs`, `DashboardPage.cs`, `GraphIndexSelfCheck.cs`, `src/SMSR.App/WebAssets/graph-explorer.js`, `README.md`, `docs/graph-tracking-guide.md`, `docs/development-log.md`.
- 변경 사유: 동일한 관계 색인을 보여주던 찾기·연결 보기·코드 흐름 보기를 찾기로 통합하고 중복 버튼·로컬 프로젝트 탐색 표기를 제거했다. 다른 화면에서 작업 ID가 누락되면 작업 현황으로 돌아갈 수 없던 문제도 해결했다.
- 실행 명령: `dotnet build src/SMSR.App/SMSR.App.csproj -c Debug --no-restore -v:q`, `node --check src/SMSR.App/WebAssets/graph-explorer.js`, `node scripts/test_graph_explorer_labels.mjs`, `node scripts/test_graph_explorer.mjs`, `node scripts/test_dashboard_timeline.mjs`, `node scripts/test_web_loading_orb.mjs`, `dotnet publish` (r9), 게시본 `--graph-self-test`·`--tracking-self-test`, `python scripts/verify-graph-backup.py`, 같은 출처 `/api/graph/index`, 로컬 HTTP·브라우저 확인, `git diff --check`.
- 검증 결과: 빌드 경고·오류 0개와 스크립트·자체검사가 통과했다. 실행 앱을 `artifacts/live-web-ui-20260929-r9`로 교체했다. 브라우저에서 대시보드→찾기→추가 기능 및 예전 `view=flow` 주소의 찾기 전환을 확인했다. 작업 ID 없는 찾기 화면에서도 복구된 `작업 현황` 링크로 대시보드에 복귀했다. 운영 DB를 변경 전 `verification-backups/20260929-042613-401304`에 백업하고 별도 복원 무결성을 확인했으며, 문서 변경을 포함해 색인을 갱신했다.
- 남은 위험: 기존 그래프 리비전은 삭제하지 않았다. 리비전마다 전체 스냅샷을 저장하는 구조의 보관 기간·용량 정책은 별도로 결정해야 한다. 줄 단위 분석은 추가 기능에 남아 있으며 실제 호출 경로로 오해되지 않도록 이름을 정리했다.
- 다음 조치: 그래프 저장량을 실측해 안전한 리비전 보관 정책을 결정한다.

## 2026-09-29 - 완료 그래프 결과 누락 방지와 과거 기록 보완

- 변경 파일: `src/SMSR.App/Mvp/EventStoreWorkflowResults.cs`, `EventStoreWrites.cs`, `EventStore.cs`, `EventValidation.cs`, `TrackingContractSelfCheck.cs`, `DashboardPanels.cs`, `DashboardPage.cs`, `docs/development-log.md`.
- 변경 사유: 결과 기록 기능 도입 전 그래프의 최종 결과 공란과 이후 종료 이벤트의 결과 저장 누락을 해결한다. 실제 조사에서 기존 34개 중 결과 공란 31개는 완료 30개·진행 중 1개였다.
- 실행 명령: `dotnet build src/SMSR.App/SMSR.App.csproj -c Debug --no-restore -v:q`, 앱 `--tracking-self-test`·`--graph-self-test`·`--self-test`, `python scripts/verify-graph-backup.py`, `dotnet publish` (r10~r12), 로컬 HTTP·브라우저 확인, `git diff --check`.
- 검증 결과: 빌드 경고·오류 0개와 세 자체검사 통과. 종료 이벤트에는 요약 또는 오류가 필요하며, 모든 계획 노드가 종료되면 마지막 종료 기록을 출처가 드러나는 자동 요약으로 DB에 저장한다. 사용자가 작성한 결과는 자동 요약으로 덮어쓰지 않는다. 완료된 기존 30개 공란을 보완했고 명시 결과 3개는 보존했다. 진행 중 2개는 ‘종료 후 결과가 기록됩니다’로 표시한다. 백업 `verification-backups/20260929-043635-436172`의 별도 복원 무결성을 통과했고 실행 앱은 `artifacts/live-web-ui-20260929-r12`이다.
- 남은 위험: 과거 자동 요약은 마지막 종료 이벤트의 내용이므로 전체 작업을 종합한 사용자 작성 결과보다 정보가 적을 수 있다. 작업 배경·진행 방식이 없는 과거 기록은 근거 없이 만들어 넣지 않았다.
- 다음 조치: 필요한 과거 그래프의 자동 요약을 두 번 클릭해 검토·보완한다.

## 2026-09-29 - 프로젝트 폴더 선택·전체 색인

- 변경 파일: `src/SMSR.App/Mvp/GraphFileScanner.cs`, `GraphIndexService.cs`, `GraphIndexOptions.cs`, `EventStoreGraphScope.cs`, `EventStoreGraphSchema.cs`, `EventStoreGraphWrites.cs`, `GraphContracts.cs`, `GraphEndpoints.cs`, `GraphExplorerPage.cs`, `GraphPage.cs`, `GraphIndexSelfCheck.cs`, `src/SMSR.App/WebAssets/graph-explorer.js`, `README.md`, `docs/graph-tracking-guide.md`, `docs/development-log.md`.
- 변경 사유: 프로젝트를 선택한 뒤에도 Git 경로를 수동 입력해야 했고, 찾기 화면에서 미색인 상태와 시작 방법을 알기 어려웠다.
- 실행 명령: `dotnet build src/SMSR.App/SMSR.App.csproj --no-restore -v:q`, `dotnet run --project src/SMSR.App/SMSR.App.csproj --no-build -- --graph-self-test`, `node scripts/test_graph_explorer.mjs`, `node scripts/test_graph_explorer_labels.mjs`, `node --check src/SMSR.App/WebAssets/graph-explorer.js`, `python scripts/verify-graph-backup.py`, `dotnet publish`, 로컬 HTTP·브라우저 확인.
- 검증 결과: 전체·선택 폴더 색인과 범위 저장, 범위별 최신성, 전체 복원, 잘못된 폴더 차단, 경로 없는 API 요청을 자체검사했다. SMSR 기존 경로와 AO3.5_Main의 미색인 경로를 자동 인식했고, 브라우저에서 7개 폴더 선택 UI를 확인했다. 실행 앱은 `artifacts/live-web-ui-20260929-r14`로 교체했다. 운영 DB 백업은 `%LocalAppData%\SMSR\verification-backups\20260929-051107-707052`이며 복원 무결성 검사를 통과했다.
- 남은 위험: 경로를 자동 인식할 수 없는 별도 위치의 프로젝트는 상세 관계 도구에서 처음 한 번 경로를 연결해야 한다. 선택 폴더 색인은 최상위 폴더 기준이며 기존 색인 범위를 대체한다.
- 다음 조치: 새 폴더를 임의 위치에서도 선택할 수 있는 OS 폴더 선택기가 필요한지 실제 사용을 보고 판단한다.

## 2026-09-29 - 찾기 결과 종류 필터

- 변경 파일: `src/SMSR.App/Mvp/GraphExplorerPage.cs`, `GraphIndexSelfCheck.cs`, `src/SMSR.App/WebAssets/graph-explorer.js`, `docs/development-log.md`.
- 변경 사유: 찾기의 코드·문서·문서 제목·이미지 표시가 범례로만 동작해 종류별 결과를 바로 좁힐 수 없었다.
- 실행 명령: `node --check src/SMSR.App/WebAssets/graph-explorer.js`, `dotnet build src/SMSR.App/SMSR.App.csproj --no-restore -v:q`, `dotnet run --project src/SMSR.App/SMSR.App.csproj --no-build -- --graph-self-test`, `dotnet publish`, 로컬 화면 확인.
- 검증 결과: 종류 버튼 클릭 시 서버 종류 필터로 다시 검색하고, 같은 버튼 재선택 시 전체 결과로 돌아간다. 선택 상태는 `aria-pressed`로 드러낸다.
- 남은 위험: 종류 필터는 검색 결과에만 적용되며, 선택 항목의 관계 그래프는 실제 연결 종류를 그대로 표시한다.
- 다음 조치: 사용 중 여러 종류 동시 선택이 필요한지 확인한다.

## 2026-09-29 - 찾기 결과 페이지 이동

- 변경 파일: `src/SMSR.App/Mvp/EventStoreGraphNodes.cs`, `GraphQueryService.cs`, `GraphEndpoints.cs`, `GraphExplorerPage.cs`, `GraphIndexSelfCheck.cs`, `src/SMSR.App/WebAssets/graph-explorer.js`, `docs/development-log.md`.
- 변경 사유: 찾기가 앞의 60개만 표시하고 문서를 우선 정렬해 나머지 코드가 누락된 것처럼 보였다.
- 실행 명령: `node --check src/SMSR.App/WebAssets/graph-explorer.js`, `dotnet build src/SMSR.App/SMSR.App.csproj --no-restore -v:q`, `dotnet run --project src/SMSR.App/SMSR.App.csproj --no-build -- --graph-self-test`, `dotnet publish` (r16), 로컬 API·브라우저 확인, `git diff --check`.
- 검증 결과: API의 안정 정렬·offset 검사와 서로 다른 연속 페이지를 자체검사했다. 실행 앱을 `artifacts/live-web-ui-20260929-r16`으로 교체했다. AO3.5_Main에서 첫 60개와 다음 60개가 겹치지 않고, 브라우저에서 다음 페이지·코드 필터 시 첫 페이지 복귀를 확인했다.
- 남은 위험: 색인 자체는 최대 10,000개 파일이고 비밀·생성·대용량 파일은 제외된다. 페이지는 현재 색인 리비전에 대한 조회이며 색인을 갱신하면 결과 순서가 바뀔 수 있다.
- 다음 조치: 대형 저장소에서 전체 결과 건수 표시가 필요한지 사용성을 확인한다.

## 2026-09-29 - C# 정적 호출·프로젝트 참조 색인 첫 단계

- 변경 파일: `src/SMSR.CSharpAnalysis/Program.cs`, `CompilationInput.cs`, `FileCallIndex.cs`, `src/SMSR.App/Mvp/GraphWorker.cs`, `GraphCSharpFileRelations.cs`, `GraphIndexService.cs`, `EventStoreGraphFormat.cs`, `GraphStaticSelfCheck.cs`, `src/SMSR.App/WebAssets/graph-explorer-labels.js`, `src/SMSR.App/App.xaml.cs`, `docs/development-log.md`.
- 변경 사유: 기존 색인이 파일만 저장하고 코드 간 호출을 기본 그래프에 반영하지 않아 코드 관계를 찾을 수 없었다.
- 실행 명령: `dotnet build src/SMSR.App/SMSR.App.csproj -c Debug --no-restore`, C# 분석기 `--self-test`, 앱 `--graph-static-self-test`·`--graph-self-test`, `node scripts/test_graph_explorer_labels.mjs`, `python scripts/verify-graph-backup.py`, `dotnet publish` (r17), 로컬 색인 API·관계 조회, `git diff --check`.
- 검증 결과: 빌드 경고·오류 0개. C# 컴파일러가 확인한 파일 간 호출과 `.csproj`의 명시적 프로젝트 참조가 그래프에 저장되고, 호출 제거 후 재색인 시 오래된 관계가 삭제됨을 확인했다. 기존 그래프 자체검사도 재실행해 통과했다. AO3.5_Main의 729파일·약 8.0 MB 묶음을 14.9초에 분석해 파일 간 관계 1,900개를 얻었다. 실행 앱은 `artifacts/live-web-ui-20260929-r17`이며 AO3.5_Main을 재색인해 리비전 3, 파일 2,604개, 관계 2,397개(호출 2,353개·프로젝트 참조 18개)를 확인했다. 운영 DB 백업은 `%LocalAppData%\SMSR\verification-backups\20260929-060837-712615\snapshot.db`이며 복원 무결성을 통과했다.
- 남은 위험: 현재 기본 색인은 C# 파일 간 호출·프로젝트 참조만 추가한다. 함수별 노드, 타 언어의 호출 관계, 제어·데이터 흐름, 대형 프로젝트(프로젝트당 1,000파일 또는 16 MiB 초과)는 아직 지원하지 않는다. C# 분석은 입력 파일 묶음 안에서만 바인딩하므로 프로젝트 간 호출은 표시하지 않는다.
- 다음 조치: 실제 AO3.5_Main 색인 시간과 관계 정확도를 측정한 뒤 함수별 탐색과 다른 언어 추출을 확장한다.

## 2026-09-29 - 미디어 미리보기·관계 표시 확장·v1.7.0 배포

- 변경 파일: `GraphFileScanner.cs`, `GraphMediaService.cs`, `GraphMediaTypes.cs`, `GraphMediaSelfCheck.cs`, `GraphEndpoints.cs`, `GraphExplorerPage.cs`, `EventStoreGraphNodes.cs`, `GraphImageSelfCheck.cs`, `GraphIndexSelfCheck.cs`, `GraphWorker.cs`, `graph-explorer.js`, `graph-explorer-labels.js`, `scripts/test_graph_explorer_labels.mjs`, `SMSR.App.csproj`, `.gitignore`, `README.md`, `docs/releases/v1.7.0.md`, `docs/development-log.md`.
- 변경 사유: 문서 제목이 별도 검색 결과를 차지하고, 이미지·영상·음원은 웹에서 확인할 수 없으며, 관계 화면은 실제 저장된 관계를 방향별 4개만 표시했다.
- 실행 명령: `dotnet build`, 앱 `--graph-self-test`, `node --check`, `node scripts/test_graph_explorer_labels.mjs`, `python scripts/verify-graph-backup.py`, `dotnet publish`, `scripts/build-installer.ps1`, 로컬 HTTP·브라우저 확인, `git diff --check`.
- 검증 결과: 솔루션 빌드 경고·오류 0개, `--graph-self-test`·`--graph-static-self-test`·`--tracking-self-test`·`--self-test` 통과. 실행 앱 `artifacts/live-web-ui-20260929-r19`에서 이미지 범위 요청 206·`image/png`과 문서 제목 제외 검색을 확인했다. `SMSR-Setup-1.7.0.0-win-x64.exe`와 SHA-256 파일을 생성했고 체크섬을 재검증했다. 설치 배포 폴더의 자체 포함 C# 분석기에서도 두 그래프 자체검사를 통과했다. 운영 DB 백업 `%LocalAppData%\SMSR\verification-backups\20260929-063426-199165\snapshot.db`는 복원 무결성을 통과했다.
- 남은 위험: 기본 관계는 파일 단위이며 호출 횟수와 함수별 관계는 저장하지 않는다. 화면에는 방향별 최대 100개만 조회된다. 미디어는 코덱·크기 제한을 받으며 SVG는 미리보기를 차단한다.
- 다음 조치: 관계 페이지 이동, 호출 위치·횟수 저장, 함수별·다언어 관계와 대형 프로젝트 분석을 구현한다.

## 2026-09-29 - 중복 SMSR 훅 충돌 해소

- 변경 파일: 사용자 Codex `config.toml`의 `smsr-codex@personal` 활성화 설정, `docs/development-log.md`.
- 변경 사유: 구형 플러그인 훅이 제거된 `record_lifecycle` MCP 도구를 메시지·종료마다 호출해 오류를 반복했다. 현재 전역 명령형 훅은 별도로 등록되어 있다.
- 실행 명령: 플러그인·전역 훅·현재 MCP 도구 목록 비교, 설정 백업, TOML/JSON 구문 검사, 현재 훅 실행, Codex 설정·추적 자체검사.
- 검증 결과: 구형 플러그인만 비활성화했다. TOML/JSON 파싱, 전역 훅 8종 확인, 현재 `UserPromptSubmit` 훅 직접 실행(exit 0), 설치 앱의 설정·추적 자체검사(각 exit 0)를 통과했다. 전역 훅에는 `record_lifecycle` 호출이 없다. 사용자 설정 백업은 `config.toml.bak-smsr-hook-20260929_165653`이다.
- 남은 위험: 이미 실행 중인 Codex 대화는 플러그인 훅을 메모리에 유지할 수 있어 완전 재시작 전에는 같은 경고가 한 번 더 나올 수 있다.
- 다음 조치: Codex 완전 재시작 후 새 대화에서 훅 오류 재발 여부를 확인한다.
