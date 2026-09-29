# Go 분석기 — 구현 준비, 실행 검증 대기

현재 상태는 **미검증 소스**다. 설치된 Go SDK를 찾지 못해 Go 컴파일·테스트를 실행하지 않았다. 기존 tree-sitter Go 구문 검사와 PowerShell 파서 검사만 통과했다. 앱 배포·HTTP/MCP/UI에는 아직 연결하지 않았다. 이 상태를 Go 정밀 분석 완료로 간주하면 안 된다.

## 입력·분석 계약

- 표준 입력 JSON `{"files":[{"path":"pkg/example.go","text":"..."}]}`. 명시한 같은 디렉터리의 단일 패키지만 메모리에서 분석한다. 파일 순서를 정렬하고 중복·상위/절대/제어문자 경로를 거부한다.
- Go 언어 버전 `go1.23`, 타입 크기 `gc/amd64`. 현재 SDK의 `go/parser`·`go/types`를 사용한다. 파일을 명시적으로 선택했으므로 build tag/GOOS 파일 선택·go.mod·workspace·외부 패키지 설정은 반영하지 않는다.
- 선언·참조·정적 호출 대상·호출자·타입 형태·인자/매개변수 순번을 출력한다. 인터페이스 호출과 함수값 호출을 직접 호출과 구분한다. 함수값에는 실제 대상 ID를 생성하지 않는다. 타입 오류가 있으면 `COMPILER_CANDIDATE`로 표시한다.
- 가변 인수, 슬라이스 펼침, 다중 반환 인자 확장을 구분한다. 타입 변환은 함수 호출과 구분한다. CFG·PDG·taint·힙/별칭·실제 런타임 대상 분석은 아직 없다.
- 위치는 `//line` 지시문의 논리 경로가 아닌 입력 파일의 물리 경로와 UTF-16 좌표다. 리터럴·상수 값·struct tag·컴파일러 원문 오류는 출력하지 않는다. 이름/상대 경로는 근거로 보존한다.

## 표준 라이브러리와 실행 보호

공식 Go 소스 importer는 cgo 도구를 실행할 수 있어 사용하지 않는다. 별도 빌드 단계에서 신뢰한 SDK의 표준 라이브러리만 export 자료로 준비하고, 분석 시에는 `go/importer`의 `gc` importer와 제한된 archive lookup을 사용한다. 사용자 저장소 모듈의 빌드·다운로드·cgo·go generate·init/main은 실행하지 않는다.

JSON 입력32 MiB, 파일500개·각2 MiB·합계16 MiB, AST/타입/출력 작업5만, 출력16 MiB. Go 메모리 제한512 MiB는 soft limit이다. 향후 앱 연결에서는 기존 GraphWorker의 Job Object·시간/출력 제한을 반드시 적용해야 한다. 이 자체 실행 파일은 OS 샌드박스가 아니다. 표준 export 디렉터리는 신뢰한 빌드 산출물이며 저장소 파일에서 덮어쓰게 하면 안 된다.

## 승인 후 수행할 검증

공식 SDK를 SMSR 전용 위치에 설치하기 전 사용자 결정을 받는다. 시스템 PATH나 전역 Go/Git 설정을 변경하지 않는다. 승인된 SDK 경로로 다음을 실행한다.

```powershell
./scripts/build-go-analysis.ps1 -Go '<승인된 SDK의 절대 경로>/bin/go.exe'
```

스크립트는 표준 라이브러리 export 준비, 테스트, 분석기 빌드와 Go 라이선스 복사를 수행한다. 프로세스 환경은 실행 뒤 복원한다. `artifacts/go-analysis`는 생성 산출물이다. 실제 SDK가 확보되면 코드 포맷·컴파일 오류부터 수정하고 테스트 결과를 기록한 뒤 앱 저장/조회/UI를 연결한다.

정답집: 파일 간 선언/호출, 이름 가림, 제네릭·직접/인터페이스/함수값 호출, 표준 라이브러리, 인자 모드, 경로 경계, UTF-16·CRLF·line 지시문, 비밀 리터럴/태그/오류 제외. 아직 **실행되지 않았다**.

공식 근거: [go/parser](https://pkg.go.dev/go/parser), [go/types](https://pkg.go.dev/go/types), [go/importer](https://pkg.go.dev/go/importer), [Go source importer 구현](https://go.dev/src/go/internal/srcimporter/srcimporter.go).
