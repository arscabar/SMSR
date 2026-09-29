# JavaScript·TypeScript·TSX 컴파일러 예제

저장소를 색인한 뒤 로컬 검색·코드 분석 화면의 TS/JS 묶음에 다음 경로를 입력합니다.

```text
samples/graph-typescript/math.ts
samples/graph-typescript/entry.ts
samples/graph-typescript/helper.js
samples/graph-typescript/view.tsx
```

- `identity(42)`: 다른 파일의 제네릭 선언, NUMBER 반환 타입.
- `select('example')`: 문자열 오버로드의 선언 선택. 문자열 값은 결과에 저장하지 않음.
- `double(3)`: JSDoc의 NUMBER 매개변수와 호출 인수 대응.
- `<View>`: TSX 정적 시그니처. JSX props의 인수 전개는 미지원으로 명시.
- `overwrite(5)`: 입력을 덮어쓰므로 입력→반환 데이터 경로가 남지 않는다.
- `choose(5, true)`: 두 분기의 result 정의가 반환 읽기에 합쳐진다.
- `repeat(5, true)`: 0회/반복 후 정의가 합쳐진다. 실제 인수 값을 대입해 조건을 풀지는 않는다.
- `counted(5)`: continue 뒤에도 for 갱신식의 result 대입이 실행되는 후보 경로를 확인한다.
- `relayed`: identity를 거쳐 입력0이 반환에 영향을 주는 요약과 호출 내부 간선1개를 확인한다.
- `erased`: overwrite가 입력을 덮어쓰므로 반환 영향 입력과 호출 요약 간선이 비어 있다. 이것은 안전성 판정이 아니다.
- `decide`: TRUE/FALSE마다 다른 반환이 선택되는 제어 의존.
- `nullSet`: NULLISH일 때만 대입 실행. 0/false/빈 문자열과 null/undefined를 같은 것으로 취급하지 않는다.
- `selected`: 첫 조건이 거짓이면 두 번째 조건을 평가하지 않는 정상 경로.
- `endless`: 종료 도달 불가 사유를 표시하고 제어 의존은 비운다.
- `switchPick`: 중간 default보다 뒤 case를 먼저 비교하며 CASE_MATCH/CASE_NO_MATCH별 반환이 다르다.
- `switchFall`: 첫 case 일치 후 다음 case 조건식을 평가하지 않고 본문을 이어 실행한다.
- `switchRelayed`: switchPick을 호출한 결과에 입력1·2의 데이터 영향과 호출 간선2개가 표시된다. 선택 조건 입력0은 데이터 의존에 섞지 않는다.
- `switchKilled`·`switchErased`: 모든 case/default가 값을 덮어쓰면 반환 영향 입력과 호출 요약 간선이 비어 있다. 안전성 판정은 아니다.

버전8의 **JS/TS 정상 경로 · 조건별 제어 의존**은 기존 구문상 값 간선과 별도이다. if·switch·단락/삼항·논리 대입·nullish·while/for/do·라벨 없는 break/continue·반환/명시 throw를 정상 CFG로 연결한다. TRUE/FALSE는 진릿값 검사이며 NULLISH/NON_NULLISH는 null/undefined 여부다. 유한 종료 경로 후지배로 계산하므로 실제 종료·경로 조건 만족·암묵적 예외·getter/호출 내부 효과·함수 간 제어/taint를 증명하지 않는다. try/for-in/of/라벨/옵셔널 체인/구조분해/기본·rest 인자/비동기·생성기/직접 eval·import 등 미지원은 사유와 빈 CFG다. 람다/함수 표현식의 생성과 지연 본문을 구분한다. JSX 확장은 미지원이며 TSX의 일반 함수 부분에만 같은 분석이 적용된다. 버전7 이하 결과는 재분석한다.

`SWITCH_VALUE`는 최초 선택식의 1회 평가 위치, `CASE_TEST`는 case 비교 위치다. CASE_MATCH/CASE_NO_MATCH는 그 값과 엄격하게 일치/불일치하는 정적 경로이며 실제 값 판정은 하지 않는다. 모든 case 불일치 후 default로 진입하고, 선택 후에는 본문만 fall-through한다. switch의 break는 switch 밖으로, continue는 바깥 반복문의 갱신/조건으로 연결된다. 제어 표가 있어도 완전한 PDG/taint가 아니다.

버전8은 switch 값·도달 정의와 함수 간 반환 데이터 요약도 제공한다. 각 case 선택 시점의 상태를 보관해 뒤쪽 조건식의 대입이 앞선 선택 경로에 섞이지 않게 한다. default는 마지막 검색 상태로 시작하며 각 본문 진입/이전 본문 fall-through·break·continue 상태를 합친다. 경로별 조건을 풀지 않는 MAY 후보이므로 실제 실행 가능한 값만 남긴 결과는 아니다. 미지원 본문·외부 호출·예외/힙 효과와 완전한 taint 보장은 계속 제외한다.

버전5의 **호출별 반환 데이터 의존 요약**은 본문의 값 경로로 확인한 매개변수 의존성을 각 호출의 인수→결과에 대응시킨다. 다단계/재귀 호출은 고정점으로 계산하고 호출 사이의 값을 섞지 않는다. 외부 호출·미지원 본문·외부 읽기의 미해석 값 ID도 반환까지 전파한다. 제어 의존·실제 호출 대상·종료·예외·힙/별칭은 포함하지 않는다. 미해석 ID가 없거나 영향 입력이 비어 있어도 완전한 분석/안전성을 뜻하지 않는다.

버전4의 값 분석은 for·do/while, 가장 가까운 반복문의 break/continue, 전위/후위 증감 및 복합 대입을 추가했다. break는 갱신식을 건너뛰며 do는 최소 한 번 실행한다. switch 값 분석은 버전8에 추가했고 라벨·for-in/of·예외 흐름은 아직 지원하지 않는다. 상수 조건의 참/거짓도 풀지 않으므로 실제 도달 가능성을 확정하지 않는다.

버전3의 **함수 내부 값·도달 정의 후보**에서 함수별 노드/간선을 펼칠 수 있다. 대입은 이전 정의를 제거하며 if·단락·while은 정의 집합을 합친다. 제어 간선은 구문상 조건 근거이고 완전한 PDG가 아니다. 호출 인수 노드·본문 매개변수 노드·반환 노드 ID는 기존 호출별 연결에 기록하지만, 함수 간 값 요약을 자동 확정하지 않는다. 미지원 구문이 있는 함수는 빈 값 그래프와 사유를 표시한다.

버전2의 **호출별 본문 연결 후보**에서는 `identity`, `select`, `double`의 각 호출이 구현 본문의 매개변수·명시 반환 위치와 연결된다. 오버로드의 선택 선언과 구현 본문 위치가 다른 점도 확인할 수 있다. `<View>`는 본문을 찾더라도 일반 함수 호출과 다른 프로토콜이므로 입출력 연결을 만들지 않는다.

연결 상세는 호출별로 분리된다. 중첩 함수의 반환은 바깥 함수에 섞이지 않는다. 기본값은 인수가 undefined인 조건을 표시하며, async/generator·rest/spread·구조분해 매개변수는 불가 사유를 표시한다. 반환은 구문상 위치이며 도달 가능성·finally 효과·암묵 반환이나 실제 실행 대상을 확정하지 않는다. 함수 내부의 인수→반환 의존성을 추측해 연결하지 않는다.

설치된 Node 및 VS Code TypeScript 6.0 컴파일러가 필요합니다. 대상 코드·프로젝트 설정·패키지·빌드를 실행하거나 자동 설치하지 않습니다. 이 예제의 성공은 전체16언어의 정밀 분석이나 전체 프로그램 PDG/taint 완료를 뜻하지 않습니다.
