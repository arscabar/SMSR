# Java 언어 서버 정의 조회 예제

기본 사용자 VS Code 경로의 Red Hat Java1.56.0 win32-x64 설치가 필요하다. SMSR은 설치하지 않는다. 기존 JDK17 묶음 분석과 별개로 JDT의 정의 위치를 확인하며 결과는 후보로 저장한다.

SMSR 저장소를 색인한 뒤 **로컬 검색·코드 분석 → Java 언어 서버 정의 조회**에서 입력한다.

- Java 상대 경로: `samples/graph-jdt/src/sample/Choices.java`, `samples/graph-jdt/src/sample/Usage.java` (각각 한 줄).
- 소스 루트: `samples/graph-jdt/src`.
- 조회 파일: `samples/graph-jdt/src/sample/Usage.java`.
- 줄4, UTF-16 열51: 정수 오버로드. 결과 `Choices.java` 줄4의 `pick`.
- 줄5, UTF-16 열55: 문자열 오버로드. 결과 `Choices.java` 줄5의 `pick`.

화면은1부터, API/MCP는0부터 시작하는 UTF-16 좌표다. 선택 이름 안의 열을 지정한다. 결과 링크에서 원문을 확인하고 같은 입력의 저장 결과를 다시 열 수 있다. 소스/색인 변경 후에는 갱신 필요로 표시한다.

원본 프로젝트 설정은 복사하지 않는다. 명시 Java 파일만 임시 소스 루트에 복사하고 의존성 가져오기/자동 빌드를 끈다. 정의가 없어도 안전성이나 정상 컴파일을 뜻하지 않으며 실제 Maven/Gradle 프로젝트 설정·외부 라이브러리·실제 디스패치·전체 PDG/taint는 이 기능의 결과로 확정하지 않는다. 알려진 비밀 패턴은 거부하지만 모든 비밀을 판별하지는 못한다.
