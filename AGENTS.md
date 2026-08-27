# Repository Instructions

## WPF MCP Dashboard Development Rules

- 이 저장소의 Windows 앱 작업에서는 WPF, WPF-UI, .NET, ASP.NET Core Kestrel, SQLite, 로컬 MCP 서버에 능통한 시니어 Windows 개발자처럼 판단한다.
- 먼저 기존 구조와 패턴을 확인하고, 새 추상화나 새 의존성은 필요할 때만 추가한다.
- 새로 작성하거나 수정하는 코드 파일은 가능하면 파일당 3000자를 넘지 않게 유지한다. 넘을 경우 기존 패턴에 맞춰 작게 나누고, 문서/생성 파일은 예외로 둘 수 있다.
- 같은 이슈나 같은 원인의 실패가 3회 이상 반복되면 즉시 재시도를 멈추고 계획을 다시 세운다. 실패 원인, 시도 내역, 새 계획을 문서나 이슈 댓글에 남긴다.
- 개발 작업마다 작업 이력, 실행 명령, 변경 파일, 변경 사유, 검증 결과, 남은 위험을 기록한다.
- 원시 사고 과정은 기록하지 않는다. 대신 결정 요약, 근거 요약, 관찰된 로그, 다음 조치를 기록한다.
- MCP 기록 또는 로컬 대시보드 기능을 다룰 때는 에이전트가 HTML을 직접 수정하지 않고 이벤트와 상태 데이터만 기록하도록 설계한다.
- 로컬 서버는 기본적으로 127.0.0.1에만 bind하고, 외부 네트워크 노출은 명시 요청이 있을 때만 고려한다.
- API key나 token은 평문 저장하지 않는다. Windows DPAPI 또는 Credential Manager 같은 OS 보안 저장소를 우선 사용한다.

## Development Log Rules

기존 changelog/devlog 패턴이 있으면 그것을 따른다. 없으면 `docs/development-log.md`를 만들고 아래 형식을 사용한다.

```markdown
## YYYY-MM-DD - 작업 제목

- 변경 파일:
- 변경 사유:
- 실행 명령:
- 검증 결과:
- 남은 위험:
- 다음 조치:
```
