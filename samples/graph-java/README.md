# Java 17 분석 예제

SMSR을 색인한 뒤 `/graph/advanced?projectId=SMSR`의 Java 파일 묶음에 다음 네 경로를 줄별로 입력한다.

```text
samples/graph-java/Lib.java
samples/graph-java/Entry.java
samples/graph-java/Switches.java
samples/graph-java/Expressions.java
```

`Java 분석·저장`에서 오버로드별 대상, 제네릭 추론 타입, 직접/가상 호출을 확인한다. `Java 저장 결과`는 동일 묶음의 SQLite 저장 결과와 최신 여부를 조회한다. 가상 호출은 정적 대상만 확인한 것으로 표시한다.

버전2의 `Java 메서드 내부 값·도달 정의 후보`에서 `pick`/`identity`의 입력→반환, `overwrite`의 입력 제거, `choose`의 분기 합류, `counted`의 continue 뒤 for 갱신식을 확인한다. 외부 호출 결과는 인수와 자동 연결하지 않는다. 생성자·try/finally·힙/배열 접근·람다 등 미지원 메서드는 빈 값 그래프와 사유를 표시한다. 버전1 저장 결과는 재분석해야 한다.

버전3의 `Java 호출별 본문·반환 데이터 요약`은 직접 호출의 입력/본문 반환 대응과 함수 간 데이터 의존을 표시한다. `relayed`는 영향 입력0·호출 간선1개, `erased`는 빈 영향 입력·간선0개다. 재귀/다단계 호출은 고정점으로 계산한다. 가상/가변 호출·외부/미지원 본문은 미해석 반환으로 유지한다. 제어 의존·예외·힙 효과·종료는 포함하지 않으므로 빈 결과도 안전성 판정이 아니다. 버전2 결과는 재분석한다.

## 빌드

최신 버전6는 `switch` 식의 직접 결과와 블록/콜론형 `yield`를 제어·값·호출 요약에 반영한다. `Expressions.pick`의 영향 입력은1/2, `nested`는 안쪽 yield 후 덮어쓴 입력2만 남는다. `relayed`는 입력1/2·호출 간선2개, `erased`는 빈 영향이다. 안쪽 yield가 바깥 식을 종료하지 않으며 yield 시점의 지역 상태를 다음 문장에 합류한다. `YIELD`/`SWITCH_ARM_VALUE`→`SWITCH_RESULT`와 후속 반환을 화면에서 확인한다. 버전5 이하 저장 결과는 재분석한다.

버전5는 `switch` 문을 제어·값·호출별 반환 요약에 반영했다. `Switches.pick`은 CASE_MATCH/CASE_NO_MATCH에 따라 입력1/2를 반환하고, `fall`은 뒤쪽 default 대입으로 입력 영향을 제거한다. `rule`의 화살표형은 다음 본문으로 이어 실행하지 않는다. `relayed`의 반환 영향은 입력1/2·호출 간선2개, `erased`는 빈 영향·간선0개다.

선택식은 한 번 평가하며 case 상수는 실행식으로 읽지 않는다. CASE_TEST는 상수 선택을 정규화한 근거이지 JVM 비교 순서나 조건 만족성 증명이 아니다. 문자열/enum·다중 라벨·중간 default·라벨 없는 break/continue를 지원한다. 패턴·try/finally·null/언박싱 예외는 미지원이며, 아래 버전2~4 설명은 이전 이력이다. 기존 문96경로와 신규 식90경로·중첩 yield 소유권·상태 합류·호출 반환·비밀 제외·컴파일 오류를 검증한다. 명시적 throw는 정상 CFG의 종료로 표시하지만 throw가 있는 메서드의 값 분석은 아직 미지원이다. 일치 없는 enum 식 경로도 정상 결과를 만들지 않고 종료로 표시한다.

버전4의 `Java 정상 경로 · 조건별 제어 의존`에서 `decide`의 TRUE/FALSE 반환, `selected`의 단락·삼항 경로, `counted`의 continue→갱신→조건 전이를 확인한다. `endless`는 `NON_EXIT_REACHABLE_REGION`으로 제어 의존을 제공하지 않지만 정상 전이는 남긴다. 버전3 이하 결과는 재분석한다.

기존 JDK 17 이상이 PATH에 있어야 한다. 설치하지 않으며 JRE만으로는 동작하지 않는다.

```powershell
./scripts/build-java-analysis.ps1
dotnet publish src/SMSR.App -c Release --no-restore -o artifacts/live-p2-java
```

첫 명령은 SMSR 자체 분석기만 컴파일한다. 앱 빌드가 Java 컴파일을 자동 실행하지 않으므로 Java 분석기 변경 뒤 먼저 실행한다. 앱은 생성된 `artifacts/java-analysis`의 클래스만 포함하고 JDK는 포함하지 않는다.

## 범위

명시한 현재 색인 파일만 메모리에서 파싱·타입 검사한다. 대상 코드 실행, 클래스 생성, annotation processor, 빌드 스크립트, 외부 의존성 자동 탐색은 하지 않는다. Java 17 표준 API 이외의 누락 의존성은 컴파일 오류로 표시한다. 원문·리터럴·annotation 값·진단 메시지는 저장하지 않는다. 심벌명과 경로는 저장되므로 민감 경로는 색인에서 제외한다.

파일 최대 500개, 파일당 2 MiB, 전체 원문 16 MiB, 출력 16 MiB, JVM 힙 512 MiB, JVM 실행 100초·전체 워커2분 한도다. 일부 정상 CFG는 제공하지만 전체 PDG·taint, 프로젝트 맥락, 실제 런타임 호출 대상은 아직 제공하지 않는다.

값 분석은 정상 흐름 MAY 후보다. 심벌별 대입/분기/반복 정의를 추적하지만 실제 조건 만족성·예외·힙/별칭·변환/호출 부수효과·함수 간 요약은 확정하지 않는다. 제어 간선은 구문상 조건이며 완전한 제어 의존/PDG가 아니다. 값 계산25만 단계와 전체 출력2만 항목 한도를 적용한다. 전체16언어 정밀 분석 완료를 뜻하지 않는다.

버전4의 제어 표는 위 구문상 값 간선과 독립적이다. javac AST에서 if·단락/삼항·while/for/do·라벨 없는 break/continue·반환·명시적 throw를 연결하고 유한 종료 경로 후지배를 계산한다. 도달 불가 노드는 제외한다. 예외/호출 내부 효과와 실제 조건 만족성(불리언 리터럴 제외)은 모델링하지 않는다. try·switch·확장 for·라벨·동기화·익명 클래스 초기화 등 미지원은 사유를 표시한다. 람다의 생성과 본문 실행은 구분하고 본문을 호출한 것으로 연결하지 않는다. CFG 출력은 기존 Java 계산25만/출력2만 한도를 공유하며 후지배 계산은 추가25만/2만 한도로 실패 시 부분 성공을 반환하지 않는다.
