# SMSR

WPF 기반의 로컬 Codex 작업 관제 앱입니다. Codex가 구조화된 계획과 노드 상태를 MCP로 기록하면, SMSR은 SQLite에 저장하고 WPF·웹 대시보드에서 진행률과 최근 이벤트를 표시합니다.

## 실행

```powershell
dotnet run --project src/SMSR.App/SMSR.App.csproj
```

서버는 외부에 노출하지 않고 `http://127.0.0.1:49783`에서만 실행됩니다. 앱에서 워크플로우를 선택한 뒤 `대시보드 열기`를 누르면 좌측 에이전트, 중앙 의존성 순서도, 우측 작업 상세로 구성된 웹 화면을 볼 수 있습니다.

## Codex 연결

1. SMSR을 한 번 실행한다. 서버, Windows 자동 시작, Codex MCP와 전역 일일 기록·그래프 훅이 자동 구성된다.
2. 처음 등록하거나 기존 OAuth 연결에서 업데이트한 환경에서는 Codex를 한 번 다시 열고 전역 훅 신뢰만 승인한다.
3. 계산·검색·질문·읽기 전용 확인은 기록하지 않는다. 파일을 바꾼 간단한 작업은 날짜별 일지에만, 복잡한 작업은 설정에 따라 그래프와 일지에 기록한다.

`서버 · 연결` 탭의 `연결·그래프 추적 설정 복구` 버튼은 자동 설정이 실패했거나 실행 파일을 옮겼을 때만 사용한다. 작업 그래프는 사용자가 요청하거나 설정된 복잡도 기준을 만족할 때만 생성되고 해당 작업이 끝날 때까지만 갱신된다.

별도 Codex CLI, Node.js, npm은 필요하지 않습니다. SMSR은 `WindowsApps` 내부 실행 파일을 호출하지 않고 Codex가 공유하는 설정 파일을 직접 갱신합니다. 기존 설정 파일은 변경 전에 `config.toml.smsr.bak`으로 백업합니다.

## 다른 Windows PC에 설치

다른 PC에는 자체 포함형 단일 설치 프로그램을 전달합니다. 사용자가 받거나 실행하는 배포물은 Setup EXE 하나이며, 설치 내부 구성요소는 화면 앱과 stdio 브리지가 같은 런타임을 공유합니다.

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\build-installer.ps1
```

결과는 `artifacts\installer\SMSR-Setup-버전-win-x64.exe`와 자동 업데이트 검증용 `.sha256` 파일로 생성됩니다. 설치 프로그램은 관리자 권한 없이 현재 사용자에게 설치하며 시작 메뉴, Windows 자동 시작과 제거 프로그램을 등록합니다. 설치된 SMSR을 처음 실행하면 해당 PC의 Codex 공유 MCP 설정과 전역 기록 훅을 구성합니다. 대상 PC에는 .NET SDK가 필요하지 않습니다. 자세한 설치·업데이트·제거·무인 설치 방법은 [Windows 설치 프로그램 안내](docs/installer-quickstart.md)를 참고하세요.

설치 Wizard는 SMSR 브랜드 배너와 Windows 테마를 따르는 라이트·다크 화면을 제공합니다.

ZIP 휴대용 배포가 필요한 개발자는 [휴대용 배포 빠른 시작](docs/portable-quickstart.md)을 참고할 수 있지만, 일반 사용자 배포 기준은 설치 프로그램입니다.

등록되는 설정은 다음과 같습니다.

```toml
[mcp_servers.smsr]
command = "<이 PC에 설치된 SMSR.Bridge.exe 절대 경로>"
args = ["--mcp-stdio"]
startup_timeout_sec = 30
tool_timeout_sec = 60
enabled = true
```

Codex는 설치된 `SMSR.Bridge.exe` 콘솔 브리지를 직접 시작합니다. 대시보드 본체가 꺼져 있으면 브리지가 같은 폴더의 `SMSR.App.exe`를 백그라운드로 한 번만 시작하고 서버 준비를 기다립니다. 브리지는 현재 Windows 사용자만 복호화할 수 있는 DPAPI 전용 토큰으로 `127.0.0.1` 서버에 연결하므로 브라우저 OAuth, 별도 Codex CLI, Node.js, npm이 필요하지 않습니다. 실행 파일 절대 경로는 설치 또는 휴대용 실행 시 해당 PC의 실제 위치로 자동 다시 생성되며 소스에는 하드코딩하지 않습니다. HTTP OAuth endpoint는 호환·진단용으로 유지됩니다.

추적 규칙은 MCP `instructions`와 사용자 전역 Codex 훅으로 제공됩니다. 실제 파일 변경은 `record_daily_activity`로 제목·요약·파일·검증만 남깁니다. 그래프는 명시 요청 또는 `복잡한 프로젝트 작업 자동 추적`을 켠 상태에서 여러 단계·파일·검증 등 복잡도 조건을 두 개 이상 만족할 때만 생성합니다. 활성 그래프에는 최초·관련 후속 사용자 요청을 원문이 아닌 1~2문장 요약으로 노드 시작 이력에 남깁니다. 계산·짧은 검색·질문·작은 단일 수정은 그래프를 만들지 않습니다. 모든 노드가 종료되면 추적 범위를 닫고 이후 작업을 무조건 새 그래프로 만들지 않습니다. 프롬프트 원문, 비밀, 명령 원문, 도구 입력·출력은 저장하지 않습니다.

`--self-test`는 DPAPI 사용자 프로필이 로드된 일반 사용자 세션에서 실행해야 합니다. 조건이 맞지 않으면 앱이 충돌하지 않고 상세 오류를 표시합니다.

앱을 완전 종료했다가 다시 열어도 SQLite의 계획·이벤트·일일 작업은 유지됩니다. 첫 탭의 통합 캘린더는 한 달만 날짜 그리드로 표시하며 날짜별 그래프·작업 기록 수를 보여줍니다. 워크플로우 목록에는 작업명만 표시하고 상세 그래프·일일 기록은 화면에 중복 표시하지 않습니다. 다른 프로젝트의 새 이벤트가 도착해도 현재 보고 있는 화면을 강제로 바꾸지 않습니다. 웹 대시보드에는 Codex가 제공한 현재 목표 작업 전체와 그래프 연결 이후의 IN/OUT 토큰을 구분해 표시하고, 사용량을 받을 수 없는 환경에서는 추정값 대신 `수집 대기`를 표시합니다. 우측 상태 기록은 노드별 최신 상태 카드로 표시하며 개별 또는 전체 접기 상태를 실시간 갱신 뒤에도 유지합니다. 활성 heartbeat가 있는 진행·검증·재시도 노드에만 `이어 진행`, `더 빠르게`, `재설계` 버튼을 표시합니다. 버튼 지시는 SMSR 대기열에 저장되고 Codex의 다음 `record_event` 또는 `record_heartbeat` 응답으로 한 번 전달되므로 별도 입력·전송이 필요 없습니다.

`기록 관리`에서는 선택 작업, 현재 프로젝트 또는 전체 기록을 확인 후 삭제할 수 있습니다. 삭제 범위에는 SQLite 이벤트·계획·상태, 활동 JSONL과 자동추적 세션 매핑이 포함됩니다. 복구용으로 이미 내보낸 ZIP/HTML과 앱 설정은 유지됩니다.

## 설정

`설정` 탭에서 복잡 작업 자동 그래프와 앱 자동 업데이트를 체크박스로 선택할 수 있습니다. 자동 업데이트를 켜면 시작 시 최신 정식 GitHub 릴리스를 확인하고 함께 배포된 SHA-256 파일 검증 후 무인 설치·재실행합니다. 수동 `업데이트 확인`도 제공합니다. 작업계획서 영역에서는 구현 전 계획 검토를 켜거나 끄고 계획 생성 프롬프트를 편집할 수 있습니다. 일반 설정은 현재 사용자의 `%LocalAppData%\SMSR\settings.json`에 저장됩니다.

`AI 작업 요약`에서 Gemini API 키를 저장하면 API가 제공하는 `generateContent` 모델을 조회해 요약 모델을 선택할 수 있으며 기본값은 `gemini-3.8-flash`입니다. 저장 키는 마스킹해 다시 불러오며 눈 버튼으로만 표시합니다. `작업 현황` 달력은 첫 클릭을 단일 날짜로, 두 번째 클릭을 두 날짜 사이의 최대 1년 기간으로 선택합니다. AI 요약·질의 팝업에서는 특정 프로젝트 또는 전체 프로젝트를 독립적으로 선택하고 오늘이나 선택 기간의 기록을 처리할 수 있습니다. 연결 확인은 선택 모델을 실제 호출하며, 요약 실패 시에도 키 미연결로 덮지 않고 모델·할당량·응답 오류를 표시합니다. 선택 모델은 일반 설정에, API 키는 `%LocalAppData%\SMSR\gemini-api-key.bin`에 Windows 현재 사용자 DPAPI로 암호화해 각각 저장합니다. 키가 없거나 Gemini 호출이 실패한 경우에만 Codex 새 작업을 요약 요청으로 열며, 열린 작업에서 전송을 한 번 누르면 Codex가 SMSR MCP로 자료를 읽고 결과를 앱에 돌려보냅니다.

`작업 펫`에는 PNG·JPG·APNG·GIF·MP4를 여러 개 등록할 수 있습니다. 진행률 영역은 미디어 개수에 따라 0~100%를 동일 폭으로 자동 분할하며 추가·삭제할 때마다 겹침 없이 다시 배치됩니다. 파일당 최대 100MB이며 SMSR은 원본을 로컬 데이터 폴더에 복사한 뒤 선택 그래프의 진행률에 맞는 미디어를 표시합니다. 투명 PNG·APNG·GIF는 투명 창에서 표시되지만 MP4 알파 투명도는 Windows 기본 재생기가 지원하지 않습니다. 트레이 메뉴에서 펫을 표시하거나 숨길 수 있으며 차단·실패·진행·검증·완료 상태 움직임도 함께 적용됩니다.

시스템 트레이 아이콘을 우클릭하면 서버·Codex 연결 상태를 확인하고 SMSR 열기, 현재 선택된 대시보드 열기, 서버 시작·중지, 설정 열기, 펫 표시·숨김과 완전 종료를 실행할 수 있습니다. 상태는 연결됨=녹색, 연결 대기=주황색, 서버 중지=빨간색으로 표시하며 시작·중지 메뉴도 같은 의미 색상을 사용합니다. 대시보드나 서버·펫 명령은 현재 상태에 따라 사용할 수 있을 때만 활성화됩니다. 아이콘을 더블클릭하거나 시작 메뉴에서 SMSR을 다시 실행하면 숨겨진 기존 창을 복원합니다.

설정 파일 존재만으로 연결 완료로 표시하지 않습니다. Codex가 로컬 브리지를 시작하면 브리지가 보호된 연결 신호를 즉시 보내고, SMSR은 이를 받은 뒤 설정 버튼을 숨기고 `Codex 연결됨 · 도구 12개`를 표시합니다. 사용자가 확인용 도구 호출을 요청할 필요가 없습니다.

포트 `49783`을 이미 사용하는지 확인하려면 `Get-NetTCPConnection -LocalPort 49783`을 실행합니다. 충돌한 프로세스를 종료한 뒤 SMSR을 다시 시작해야 하며, MCP 등록 주소와 같은 포트로 변경해야 합니다.

## v1.5.0 작업 내용과 후속 작업

이번 릴리스는 다음 기능을 포함합니다.

- AI 요약·질의 팝업에서 특정 프로젝트 또는 전체 프로젝트와 오늘·선택 기간을 조합한다.
- 완료·진행·정체 가능·차단·실패를 분리하고 선택 노드의 활동과 상태 기록만 표시한다.
- 사용자가 등록한 이미지 한 장을 선택 그래프의 상태·진행률과 연결하고 트레이에서 표시·숨김한다.
- Codex가 로컬 세션에 보고한 현재 목표 작업과 그래프 연결 이후의 IN/OUT 토큰을 구분한다. 프롬프트나 응답 원문은 읽어 저장하지 않는다.

다음 작업은 현재 구조를 유지하면서 순서대로 검토합니다.

1. 실제 한국어 IME 입력, 다중 모니터 펫 배치, 완료·차단 그래프 화면을 수동 수용 검사한다.
2. 펫 크기·움직임 감소와 다중 모니터 배치를 사용성 확인 후 추가한다.
3. 기록 검색·상태 필터와 인수인계용 HTML·Markdown·JSON 묶음을 우선 구현한다.
4. Codex가 공개 토큰 사용량 계약을 제공하면 현재 로컬 세션 판독을 공식 입력으로 교체한다. 기존 그래프의 토큰은 추정해 소급하지 않는다.
5. 벡터 DB·지식 그래프는 구조화 기록 검색만으로 부족하다는 운영 근거가 생길 때까지 보류한다.

## 검증

```powershell
dotnet build SMSR.slnx --no-restore --verbosity:minimal
dotnet run --project src/SMSR.App/SMSR.App.csproj --no-build -- --tracking-self-test
dotnet run --project src/SMSR.App/SMSR.App.csproj --no-build -- --self-test
```

Self-check는 MCP `record_daily_activity`·`save_plan`·`record_event`·`record_heartbeat`·`list_workflows`, 날짜별 일지, 한 달 날짜 그리드, DPAPI Gemini 키 왕복, Gemini HTTP 계약, Codex 요약 요청·결과 MCP 왕복, 상태 카드 접기, 업데이트 다운로드·체크섬 거부와 `/dashboard` 반영을 검사합니다.

## Documents

- [SMSR v1.5.0 릴리즈 노트](docs/releases/v1.5.0.md)
- [SMSR v1.5.0 통합 테스트 보고서](docs/test-report-2026-09-16-v1.5.0.md)
- [SMSR 작업 관제 기능 확장 개발계획서](docs/smsr-feature-development-plan.md)
- [SMSR 작업 관제 기능 확장 개발계획서 HTML](docs/smsr-feature-development-plan.html)
- [SMSR v1.4.8 릴리즈 노트](docs/releases/v1.4.8.md)
- [SMSR v1.4.8 통합 테스트 보고서](docs/test-report-2026-09-16-v1.4.8.md)
- [SMSR v1.4.7 릴리즈 노트](docs/releases/v1.4.7.md)
- [SMSR v1.4.7 통합 테스트 보고서](docs/test-report-2026-09-15-v1.4.7.md)
- [SMSR v1.4.6 릴리즈 노트](docs/releases/v1.4.6.md)
- [SMSR v1.4.6 통합 테스트 보고서](docs/test-report-2026-09-15-v1.4.6.md)
- [SMSR v1.4.5 릴리즈 노트](docs/releases/v1.4.5.md)
- [SMSR v1.4.5 통합 테스트 보고서](docs/test-report-2026-09-15-v1.4.5.md)
- [SMSR v1.4.4 릴리즈 노트](docs/releases/v1.4.4.md)
- [SMSR v1.4.4 통합 테스트 보고서](docs/test-report-2026-09-15-v1.4.4.md)
- [SMSR v1.4.3 릴리즈 노트](docs/releases/v1.4.3.md)
- [SMSR v1.4.3 통합 테스트 보고서](docs/test-report-2026-09-15-v1.4.3.md)
- [일일 기록과 복잡 작업 그래프 안내](docs/graph-tracking-guide.md)
- [SMSR v1.4.2 릴리즈 노트](docs/releases/v1.4.2.md)
- [SMSR v1.4.2 통합 테스트 보고서](docs/test-report-2026-09-15-v1.4.2.md)
- [SMSR v1.4.1 릴리즈 노트](docs/releases/v1.4.1.md)
- [SMSR v1.4.1 통합 테스트 보고서](docs/test-report-2026-09-15-v1.4.1.md)
- [SMSR v1.4.0 릴리즈 노트](docs/releases/v1.4.0.md)
- [SMSR v1.4.0 통합 테스트 보고서](docs/test-report-2026-09-04-v1.4.0.md)
- [Windows 설치 프로그램](docs/installer-quickstart.md)
- [MCP 연결 및 이벤트 기록](docs/mcp-connection.md)
- [선택형 SMSR Codex 로컬 추적](docs/smsr-codex-local.md)
- [워크플로우 식별·동적 계획 경계 테스트 보고서](docs/test-report-2026-09-02-workflow-plan.md)
- [SMSR v1.2.0 릴리즈 노트](docs/releases/v1.2.0.md)
- [SMSR v1.1.1 릴리즈 노트](docs/releases/v1.1.1.md)
- [SMSR v1.1.0 릴리즈 노트](docs/releases/v1.1.0.md)
- [개발 이력](docs/development-log.md)
- [WPF MCP 작업 관제 앱 계획서](docs/wpf-mcp-dashboard-project-plan.md)
- [WPF MCP 작업 관제 앱 HTML 계획서](docs/wpf-mcp-dashboard-project-plan.html)
