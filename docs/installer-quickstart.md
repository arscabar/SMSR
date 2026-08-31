# SMSR Windows 설치 프로그램

## 다른 PC에 설치

1. `SMSR-Setup-버전-win-x64.exe`를 실행합니다.
2. 설치 완료 화면에서 SMSR 실행을 선택합니다.
3. SMSR 서버가 실행되면 Codex 데스크톱 앱·CLI·IDE를 다시 엽니다.
4. 이 Windows 사용자에서 처음 연결할 때만 SMSR OAuth 인증과 사용자 전역 훅을 승인합니다.

관리자 권한은 필요하지 않습니다. SMSR은 `%LOCALAPPDATA%\Programs\SMSR`에 설치되고 시작 메뉴, Windows 로그인 자동 시작, 앱 및 기능의 제거 항목이 생성됩니다. 대상 PC에는 .NET SDK, Node.js, npm 또는 별도 Codex CLI가 필요하지 않습니다.

일반 작업은 SMSR에 기록하지 않습니다. 작업 그래프는 사용자가 그래프 추적을 요청한 경우에만 생성되며, 요청한 작업이 완료·실패·중단될 때까지만 갱신됩니다.

설치 Wizard는 SMSR 브랜드 환영·완료 화면을 표시하며 Windows의 라이트·다크 모드에 맞춰 자동 전환됩니다. `/VERYSILENT` 무인 설치에서는 Wizard 화면을 표시하지 않습니다.

## 시스템 트레이

트레이 아이콘의 우클릭 메뉴에서 현재 서버·Codex 상태, SMSR 열기, 현재 대시보드 열기, 서버 시작·중지, 설정 열기와 완전 종료를 사용할 수 있습니다. 연결됨은 녹색, 연결 대기는 주황색, 서버 중지는 빨간색으로 구분합니다. 선택된 워크플로우가 없으면 대시보드 메뉴는 비활성화되고, 서버 상태에 따라 시작 또는 중지만 활성화됩니다. 더블클릭하면 메인 창을 복원합니다.

## 업데이트와 제거

새 버전 설치 프로그램을 다시 실행하면 같은 위치에서 업그레이드됩니다. 설치 경로가 유지되므로 SMSR 전역 훅 명령도 같은 경로를 사용합니다.

Windows `설정 > 앱 > 설치된 앱 > SMSR > 제거`에서 삭제할 수 있습니다. 제거 프로그램은 SMSR 자동 시작과 SMSR 소유 Codex MCP·훅만 정리합니다. 다른 Codex 설정과 `%LOCALAPPDATA%\SMSR`의 대시보드 데이터는 보존합니다.

## 무인 설치

```powershell
SMSR-Setup-1.0.0.0-win-x64.exe /VERYSILENT /SUPPRESSMSGBOXES /NORESTART
```

설치 로그가 필요하면 `/LOG`를 추가합니다. 현재 배포본은 코드 서명되지 않았으므로 다른 PC에서 Windows SmartScreen 경고가 나타날 수 있습니다.

## 개발자용 빌드

```powershell
winget install --id JRSoftware.InnoSetup -e --source winget --scope user
powershell -ExecutionPolicy Bypass -File .\scripts\build-installer.ps1
```

결과는 `artifacts\installer\SMSR-Setup-버전-win-x64.exe`에 생성됩니다.
