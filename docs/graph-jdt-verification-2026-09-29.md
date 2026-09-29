# 설치된 Java 언어 서버 실연결 검증 — 2026-09-29

상태: **이 문서는 최초 내부 검증의 이력**이다. 후속 작업에서 제한된 Java 파일 복사본의 HTTP/MCP/UI/DB 연결과 종료 지연 보완을 마쳤다. 최신 상태는 [P2 진행 보고서](p2-progress-2026-09-28.md)의 제품 연결 절을 따른다. 아래 수치는 변경 전 검증 근거이며 전체16언어 정밀 분석 완료가 아니다.

## 범위와 재사용

- 설치된 VS Code `redhat.java-1.56.0-win32-x64`의 JDT LS core `1.61.0.202609021834`, JRE `21.0.12.1`을 명시적으로 사용했다. 신규 설치나 PATH 변경은 없다.
- 기존 `GraphLspDefinitions`의 색인/원문 해시·리비전·UTF-16 좌표·루트 내부 정의 위치 검증을 재사용했다. 초기화 옵션 전달만 추가했다.
- 서버 설정은 검사별 임시 폴더에 복사한다. 데이터·홈·임시 파일 경로도 검사별로 분리한다. 알려진 Eclipse 프로젝트와 Java 소스2개만 제공하며 운영 저장소를 서버에 열지 않는다.
- 자동 빌드·Maven/Gradle 가져오기·공유 인덱스를 끄고, 검사 종료 때 소스 불변과 프로젝트 안 `.class` 파일 부재를 확인한다. 이것은 해당 표본의 검사이지 임의 프로젝트 빌드/네트워크 접근을 차단하는 보안 샌드박스가 아니다.
- 자식 LSP 프로세스에서만 JVM 옵션·클래스 경로 및 지정된 소켓 연결 환경 변수를 제거한다. 전역 환경은 변경하지 않는다.

실행 형태는 [Eclipse JDT LS 공식 실행 안내](https://github.com/eclipse-jdtls/eclipse.jdt.ls#running-from-the-command-line)를 참고했다. 문서 최신 버전이 아니라 위의 실제 설치 파일로 검사했다.

## 정답과 측정

모든 좌표는 **0부터 시작하는 UTF-16 좌표, 끝 위치 제외**다. CRLF·한글 파일명·앞선 emoji를 포함한다. 각 건은 새 서버 세션에서 실행한다.

| 입력 위치 | 기대 정의 | 실제 범위 | 전체 세션 시간 |
|---|---|---|---|
| Entry.java, 줄1의 pick(정수) | 한글.java, 줄1 | 12–16 | 67,728ms |
| Entry.java, 줄2의 pick(문자열) | 한글.java, 줄2 | 15–19 | 68,034ms |
| Entry.java, 줄3의 지역 value 사용 | Entry.java, 줄3 | 23–28 | 67,704ms |

각각 후보1개·제외0개·정확한 파일/범위 일치를 요구한다. 최종 게시본은 종료코드0, 총203,470.8118ms였다. **시간은 초기화·조회·shutdown/exit와 프로세스 종료까지 포함하며, 정의 요청 단독 지연이나 p95가 아니다.** 이전 Debug 실행 로그에서는 정의 응답 뒤 종료 대기가 길었지만 구간별 정밀 계측은 하지 않았다. 현재 세션별 실행 방식은 대화형 제품 경로로 수용하지 않았다.

- Debug 선행 검사: 종료0, 총209,381.7458ms. 최종 게시본에 추가한 개별 시간 기록·`.class` 부재 검사는 이 실행에 포함되지 않는다.
- 최종 게시본: `2026-09-29 07:34:18 KST` 결과. 상위 검사 프로세스에 잘못된 JVM 옵션·클래스 경로와 loopback 소켓 변수를 주입해도 하위 JDT 연결이 성공했다. 소스 불변·미빌드 검사 통과.
- 공통 모의 서버 회귀: 초기화 옵션 전달, 프레임/인코딩, 서버의 파일 변경·명령 실행 요청 거부, 오래된 소스·경로·UTF-16 경계, 취소·하위 프로세스 정리 검사 통과.
- 게시본 기본 그래프·고급 그래프 자체검사 결과는 개발 이력에 기록한다.
- 완료 후 검사 Java 프로세스와 해당 임시 프로젝트 폴더는 남지 않았다. 운영 앱 `artifacts/live-p2-java-yield`와 운영 DB는 교체하지 않았다.

## 식별 근거와 재현

게시 폴더: `artifacts/verify-p2-jdt`.

- `SMSR.App.dll` SHA256: `71121BA52A4D354BE43021946069CB4A37CED6E2CE898F5FDC6C605918287352`.
- 설치된 `server/config_win/config.ini` 검사 전후 SHA256: `FD62637423FF16DBCD39A780A093BCF50779ED4F65BF4D631A1C46E306BC7529`로 동일.

PowerShell에서 기존 설치 폴더를 명시한다. 다른 버전은 재검증해야 하며 자동 다운로드하지 않는다.

```powershell
dotnet publish src/SMSR.App/SMSR.App.csproj -c Release --no-restore -o artifacts/verify-p2-jdt -v minimal
$app = (Resolve-Path artifacts/verify-p2-jdt/SMSR.App.exe).Path
$extension = 'C:/Users/surromind/.vscode/extensions/redhat.java-1.56.0-win32-x64'
$start = [System.Diagnostics.ProcessStartInfo]::new($app)
$start.UseShellExecute = $false
$start.CreateNoWindow = $true
$start.WindowStyle = 'Hidden'
$start.ArgumentList.Add('--graph-jdt-self-test')
$start.ArgumentList.Add($extension)
$test = [System.Diagnostics.Process]::Start($start)
$test.WaitForExit()
if ($test.ExitCode -ne 0) { throw "JDT check failed: $($test.ExitCode)" }
Get-Content "$env:TEMP/smsr-jdt-self-test.json"
```

결과 JSON에는 표본 좌표와 시간만 있으며 Java 원문/리터럴은 포함하지 않는다. 실패 상세는 `smsr-graph-jdt-self-test-error.txt`에 기록한다. 실패 때 이전 성공 JSON이 남을 수 있으므로 **종료코드와 결과 시각을 먼저 확인**한다. 세션별 제한은 기존2분이며3건은 순차 실행한다.

## 남은 수용 조건

1. 신뢰된 실행 파일/설정 등록과 수명 주기·취소·대기 시간 검증 후 HTTP/MCP/UI에 연결한다. 현재는 내부 검사 전용이다.
2. 실제 Maven/Gradle·클래스 경로·프로젝트 설정·대형 저장소를 별도로 검증한다. 지금은 작은 통제 표본뿐이다.
3. 다른15언어/문법의 실제 서버 프로필, 언어별 타입·별칭·호출 대상 정답집을 확보한다. 기존16문법 구문 검사를 의미 정확성 검사로 간주하지 않는다.
4. 함수 간 PDG/taint·힙/별칭·예외·동적 호출과 전체 P2 수용은 여전히 미완료다. 새 SDK 설치 결정 대기도 유지한다.
