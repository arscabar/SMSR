# SMSR 휴대용 Windows 배포본

이 ZIP은 소스 저장소, .NET SDK, Node.js, npm 또는 별도 Codex CLI 없이 실행할 수 있는 자체 포함형 SMSR입니다.

## 새 컴퓨터에서 시작

1. ZIP 전체를 원하는 폴더에 압축 해제합니다.
2. `SMSR.App.exe`를 실행합니다.
3. SMSR 서버가 실행 중인지 확인하고 Codex 데스크톱 앱·CLI·IDE를 다시 엽니다.
4. 이 Windows 사용자에서 처음 연결할 때만 SMSR OAuth 인증과 사용자 전역 훅을 승인합니다.

SMSR은 현재 실행 파일 경로를 Windows 로그인 자동 시작과 `~/.codex/hooks.json`에 등록하고, `~/.codex/config.toml`에 로컬 MCP 주소를 병합합니다. 다른 저장소에서도 같은 사용자 Codex 환경이면 별도 프로젝트 설정 없이 적용됩니다.

## 폴더를 옮길 때

SMSR을 완전히 종료한 뒤 전체 폴더를 옮기고 새 위치에서 다시 실행합니다. 자동 시작과 훅 명령은 새 실행 경로로 갱신됩니다. Codex는 훅 내용의 해시가 바뀌면 보안상 다시 신뢰를 요구할 수 있습니다.

OAuth 자격증명과 훅 신뢰는 Windows 사용자별 보안 데이터이므로 다른 PC로 복사되지 않습니다. 앱 데이터는 `%LOCALAPPDATA%\SMSR`, Codex 연동 설정은 현재 사용자의 `~/.codex`에 저장됩니다.

## 지원 범위

- `win-x64`: 일반적인 64비트 Intel/AMD Windows PC
- `win-arm64`: ARM64 Windows PC
- Windows 전용: UI가 WPF이므로 macOS와 Linux에서는 실행되지 않습니다.

배포본에 코드 서명이 없으면 새 PC에서 Windows SmartScreen 경고가 나타날 수 있습니다. 파일 출처를 확인한 뒤 실행하거나 정식 배포 단계에서 코드 서명을 추가해야 합니다.
