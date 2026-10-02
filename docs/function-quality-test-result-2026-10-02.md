# 전체 기능·실제 결과 품질 검사 — 2026-10-02

## 결론

승인 범위의 검사를 마쳤다. 자동 검사 실행 단위 46개 중 43개 통과·3개 실패다. 실제 코드 4개의 설명은 최초 3개 성공, 실패한 1개만 한 번 재시도한 뒤 4개 모두 저장됐다.

**생성·저장·검색·보관함 보호 기능은 동작하지만, 사람이 코드 역할과 관계를 정확하고 쉽게 이해하는 최종 품질에는 아직 미달이다.** 전체 기능 정상 또는 Graphify와 동등한 품질로 승인하지 않는다. 기능 수정·운영 재배포는 이번 승인 범위에 포함하지 않았다.

[생성 결과 4개 직접 읽기](function-quality-result-2026-10-02.html) · [독립 의미 검토](function-quality-semantic-review-2026-10-02.md) · [승인 계획](function-quality-test-plan-2026-10-02.md)

## 검사 기준·보호

- 운영 배포 DLL SHA-256: `5A8432AF1AAF995FBF1D4F66A25178EEF838F7A81FB20A130A336495C684C4F7`. 검사 전후 동일했다.
- .NET 검사는 기존 배포 실행본, JS·Python 검사는 현재 작업 트리의 파일을 사용했다. 실제 설명·웹 검사는 기능 코드를 고치지 않고 검사 전용 시작 옵션만 추가한 별도 Release 실행본이다. 빌드 경고·오류 0.
- 임시 DB·loopback 서버·전용 시험 보관함으로 분리했다. 현재 앱은 종료·재시작하지 않았고 운영 색인·개인 보관함·전역 훅·Git 게시를 변경하지 않았다.
- SQLite 온라인 백업과 복원: `LocalAppData/SMSR/verification-backups/20261002-065348-675055/`. 전체 테이블 건수·무결성·외래키 검사 통과.
- 운영 그래프 10개 테이블의 모든 행 지문이 전후 동일했다. 파일 4,667 · 노드 41,095 · 관계 89,907 및 과거 리비전·문제·hyperedge·형식 데이터 포함. `quick_check` 정상. 작업 추적 이벤트는 정상적으로 추가되므로 비교 대상에서 제외했다.
- 종료 코드 0은 해당 검사 경로의 통과다. 코드 커버리지나 모든 조합의 실사용 보장이 아니다.

## 자동 검사 결과

| 구분 | 실행 | 통과 | 실패 |
|---|---:|---:|---:|
| JS 모의 DOM·상호작용 회귀 | 21 | 21 | 0 |
| .NET 격리 자체 검사 | 20 | 17 | 3 |
| Python·Graphify 오프라인 회귀 | 5 | 5 | 0 |
| 합계 | 46 | 43 | 3 |

### .NET 개별 결과

| 실행 옵션 | 결과 | 소요 |
|---|---|---:|
| --self-test | PASS | 11.60초 |
| --tracking-self-test | PASS | 2.36초 |
| --oauth-self-test | PASS | 2.38초 |
| --codex-config-self-test | PASS | 2.37초 |
| --graph-self-test | PASS | 24.77초 |
| --graph-static-self-test | PASS | 575.34초 |
| --graph-lsp-self-test | PASS | 6.42초 |
| --graph-knowledge-self-test | PASS | 4.32초 |
| --graph-role-self-test | PASS | 8.32초 |
| --graph-deep-self-test | PASS | 8.47초 |
| --graph-advanced-self-test | PASS | 54.09초 |
| --graph-document-self-test | PASS | 65.67초 |
| --summary-preview-self-test | PASS | 11.79초 |
| --tistory-self-test | PASS | 79.31초 |
| --tistory-discovery-self-test | PASS | 46.54초 |
| --tistory-session-self-test | PASS | 41.62초 |
| --graph-responsiveness-self-test | PASS | 36.91초 |
| --graph-jdt-product-self-test | FAIL | 41.06초 |
| --graph-benchmark | FAIL | 0.44초 |
| --graph-overview-large-self-test | FAIL | 481.16초 |

실패는 재실행으로 숨기지 않았다.

1. Java JDT 경계 검사: `GraphJdtBoundarySelfCheck.cs:7`의 `Secret.java`가 파일명 비밀 보호 규칙에 먼저 차단된다. 실제 보안 차단은 동작하지만 검사에서 기대한 내용 기반 예외와 달라 실패했다. 이후 취소 검사는 실행되지 않았다.
2. .NET 벤치마크: `GraphBenchmarkFixture.cs:19`가 열 이름 없이 8개 값을 INSERT하지만 현재 `graph_nodes`는 9개 열이다. 측정용 fixture 스키마 불일치다. Python의 별도 oracle 벤치마크 통과와 구별한다.
3. 대형 구조 요약: 5만 노드·20만 관계에서 GraphWorker의 8분 제한을 넘었다. 계속 CPU를 사용했으며 약 465MB working set을 관찰했다. 성능 한계이며 교착 상태로 단정하지 않는다.

응답성 검사에서는 5천 파일·5만 노드·20만 관계 색인 17.31초, health p95 0.768ms/max 216.79ms, WPF 입력 큐 p95 0.331ms/max 2.370ms였다. 실제 그래프 렌더링·마우스 입력의 성능 측정은 아니다.

### JS 개별 검사 — 모두 PASS

`scripts/`의 다음 21개를 Node로 실행했다.

- test_app_deploy_startup.mjs
- test_dashboard_disclosures.mjs
- test_dashboard_selection.mjs
- test_dashboard_timeline.mjs
- test_find_simple_preview.mjs
- test_graph_edge_pick.mjs
- test_graph_explorer_deep.mjs
- test_graph_explorer_drag.mjs
- test_graph_explorer_expand.mjs
- test_graph_explorer_labels.mjs
- test_graph_explorer_network.mjs
- test_graph_explorer_placement.mjs
- test_graph_explorer_relations.mjs
- test_graph_explorer.mjs
- test_graph_export.mjs
- test_graph_find_hierarchy.mjs
- test_graph_knowledge_sync.mjs
- test_graph_roles.mjs
- test_graph_visual.mjs
- test_graphify_roadmap.mjs
- test_web_loading_orb.mjs

`test_graph_export.mjs`는 HTML 인자 없이 실행했다. UI 계약은 통과했지만 출력 문구에 포함된 실제 다운로드·CSP 검증은 이번 실행에서 수행하지 않았다.

### Python 개별 검사 — 모두 PASS

앱 runtime `C:/Users/surromind/AppData/Local/SMSR/graph-runtime/Scripts/python.exe`의 `-B`와 오프라인 설정을 사용했다. 외부 전송·모델 다운로드는 없었다.

- test_graphify_baseline.py — 고정 버전·원본 58모듈·라이선스·fixture 분류.
- test_graphify_support.py — scanner/dispatcher 일치. `.dm/.dme`는 tree-sitter-dm 부재로 FILE_ONLY다. EXTRACTOR_AVAILABLE은 정확도 보증이 아니다.
- test_graphify_reference_quality.py — XAML 속성/명령 근거와 잘못된 연결 거부.
- test_graph_benchmark.py — 경로·영향 oracle, 임시 SQLite snapshot.
- test_graph_media.py — 이미지·해시 불일치·무음·영상 프레임·시간 경계.

## 실제 코드 설명과 노트

원본 코드 4개를 바이트 그대로 복사해 별도 색인했다. 전체 프로젝트 관계나 16개 언어의 설명 품질 검사는 아니다. 4개 원본 지문 및 27개 설명 인용은 일치하며 선택 원문 절단은 없었다. 외부 helper를 포함한 전체 분석이라는 뜻은 아니다.

| 대상 | 최종 저장 | 핵심 판정 |
|---|---|---|
| GraphVaultSync.cs | CURRENT · 7문장 | 동작은 맞지만 제외 노트 보존 누락·잘못된 호출 후보 |
| GraphRoleBatchService.cs | CURRENT · 7문장 | 내부 호출은 맞지만 역할 요약이 JSON 저장에 치우침·사용량 동의 누락 |
| graph-explorer-related.js | CURRENT · 5문장 | 기본 기능 설명 유용·사용자 행동보다 구현 세부가 앞섬 |
| graphify_cache_io.py | CURRENT · 8문장 | 캐시 동작은 맞음·길고 helper 없는 보안 전체 설명 불가 |

최초 VaultSync의 인용·계약 실패는 한 번의 명시적 재시도로 복구됐다. 실패 출력은 버려져 최초 위반 항목을 정확히 확정할 수 없다. 실패 분류가 너무 포괄적인 것도 진단 한계다. 실제 모델 이름은 수집되지 않았다.

### 확인한 품질 결함

| 우선순위 | 결함 | 근거·범위 |
|---|---|---|
| 높음 | namespace 선언을 다른 파일 포함 관계처럼 표기 | VaultProjection이 공유 namespace 대표 파일 경로로 CONTAINS를 투영. 클래스 충돌이나 LLM 환각과 별개 |
| 높음 | 다른 클래스의 동명 메서드 후보가 역할 설명에 섞임 | VaultSync의 StatusAsync/SaveAsync → BatchService 후보 2개. INFERRED 경고는 있으나 사용자에게 잡음. helper 없는 제한 fixture 결과를 전체 운영 그래프 오류로 확대하지 않음 |
| 높음 | 인용 위치가 정확하지 않음 | 27개 인용 모두 실제 인용 시작행 대신 근거 구간 시작행 표기. 위조 인용은 아니지만 정확한 위치 안내가 아님 |
| 중간 | 임시 서버 노트 원문 링크가 고정 포트로 향함 | 검사 서버 54815인데 노트는 49783. 기본 포트 운영 실패를 의미하지는 않음 |
| 중간 | 핵심 역할·목적·보호 설명 누락, 긴 인용·enum 반복 | 역할→주요 동작→실제 관계→한계 순의 짧은 요약과 접힌 근거가 필요 |
| 중간 | 최초 생성 실패 진단 정보 부족 | 인용·계약 실패로만 표시되어 구체적인 위반 규칙을 확정하지 못함 |

Graphify의 추출 구조 자체를 가져왔어도 사용자에게 보여주는 파일 관계의 의미가 자동으로 정확해지는 것은 아니다. 수정 후 일반 혼합 저장소·실제 helper 포함·긴 파일로 다시 검증해야 한다.

## 실제 화면·데이터 보호

- 파일 중심 4개 검색 결과 → 검색어 cache로 1개 → 문서 필터 0개 → 코드 필터 1개 확인.
- 파일 선택 시 역할·동작, 원문 버튼, 구성요소 접힘, 관련 항목, Obsidian 링크 확인. Python 원문 41줄 실제 페이지 확인.
- 설명 현황 최신 4/4, 미분석·오래됨·대기·분석 중·실패 0 표시 확인.
- 일괄 생성의 동의 없는 시작 차단, 4개 대상 미리보기, 일시정지·재개·실패만 재시도와 최종 진행 표시 확인. 최신 재사용 4·새 분석 0 미리보기 확인. 재사용 재시작은 실제 LLM을 추가 호출하지 않기 위해 실행하지 않았다.
- 전용 보관함 연결·동기화에서 파일 노트 4개와 index 1개 생성. 위키 링크 10개 대상이 모두 존재했다.
- 재동기화: written 0 / unchanged 5 / conflicts 0. 5개 노트 해시·수정 시간이 같았다.
- 생성 노트 1개를 직접 수정한 후: written 0 / unchanged 4 / conflicts 1. 수정한 노트와 생성 영역 밖 개인 노트의 해시가 보존됐고 웹에 충돌이 표시됐다.
- 오래됨·변경된 지문의 갱신 대기와 잘못된 인용 거부는 역할 자체 검사에서 확인. 실제 모델을 다시 호출하는 원문 변경 후 재생성은 이번 실사용 검사에서 추가 실행하지 않았다.
- 운영 웹의 대시보드 → 찾기 → 추가 기능 → 작업 현황 복귀에서 workflowId 유지·실제 화면 복귀 확인.
- 운영 대시보드에서 수동 canonical 에이전트 기록과 훅 UUID 기록이 함께 표시되는 현상을 관찰했다. 중복·부모/담당 매핑은 별도 검증 필요다. 다른 에이전트의 모델·추론 강도는 추측하지 않았다.
- WPF 요약 미리보기의 한글·표·코드·버튼은 정상. 악성 HTML 요청 차단은 통과했지만 빈 iframe/이미지 자리표시자는 시각적으로 어색했다.
- 실제 웹 화면을 캡처해 시각 확인했으나 브라우저 도구가 반환한 이미지는 별도 파일 경로 없이 표시된다. 아래 자료는 저장된 WPF 검사 이미지다.

![WPF 요약 미리보기](C:/Users/surromind/AppData/Local/Temp/smsr-qa-dotnet-8b64853958b64d2aa17f03e526df2e24/summary-preview-self-test/summary-view.png)

## 실제 자동 생성 역할 문장

아래는 생성된 ROLE 문장을 수정하지 않고 옮긴 것이다. 검토자의 개선 문장이 아니다. 전체 27개 문장은 별도 HTML·JSON에 보존했다.

### GraphVaultSync.cs

제공된 구현 범위에서 SyncAsync는 프로젝트 노트의 보관함 반영을 처리하고, 기록·변경 없음·충돌·색인 제외 항목의 집계 결과를 반환하는 역할을 한다. 실제 파일 기록 방식은 GraphVaultWrite.Note 구현이 없어 확인할 수 없다.

### GraphRoleBatchService.cs

제공된 구현 범위에서 GraphRoleBatchService는 프로젝트별 일괄 작업 데이터를 JSON으로 저장하고 읽는 역할을 맡는다.

### graph-explorer-related.js

제공된 코드 범위에서 relatedItems는 전달받은 노드의 관련 항목을 조회하고, 선택 버튼과 관계 정보가 있는 화면 영역을 box에 추가하는 역할을 한다.

### graphify_cache_io.py

제공된 코드에서 callbacks()는 기존 캐시 읽기·저장 함수를 보관하고, 파일 캐시 처리와 통계를 덧붙이는 load·save 콜백 및 활성 캐시 이름 집합을 반환하는 구성 함수로 해석된다. 콜백이 실제로 등록되거나 실행되는 외부 코드는 제시되지 않았다.

## 제외·미검증

- 실제 Obsidian 앱에서 보관함 등록·노트 열기·그래프 렌더링: 미검증. URI 등록은 존재하지만 native 앱 조작을 지원하지 않는 도구 환경이다. 설치되지 않았다고 단정하지 않는다.
- 작성한 HTML 결과 보고서의 실제 렌더링: 브라우저가 file 프로토콜을 차단하여 확인하지 못했다. 차단을 우회하지 않고 Markdown 문서로 결과를 제공한다. 앞서 검사한 HTTP 웹 화면과 구별한다.
- 전체 프로젝트 LLM 분석, 개인 보관함 연결/변경, 전역 훅 설정, 커밋·푸시·릴리즈, 외부 사이트 실제 게시/로그인, 공개 업데이트 다운로드/설치는 제외.
- reference/quality export JS 검사 2개는 별도 SMSR·AO3 고정 export fixture와 과거 revision/count가 필요하므로 제외.
- test_media_content.py는 기존 산출물 3개를 덮어써 제외. test_codex_notify_hook.py는 실제 Codex·전역 훅 검사라 제외.
- Tistory CDP/manual은 실제 Chrome 창을 열므로 제외. headless fixture 3개만 검사했고 실제 게시가 아니다.
- 모든 언어의 의미 정확도, 대형 실제 그래프 가독성, 전체 변경→모델 갱신→Obsidian 앱의 실사용 연계는 이번 통과 범위가 아니다.

## 후속 작업 순서

1. namespace/선언/포함과 파일 의존 관계를 구분하고, 소유 클래스가 다른 후보를 기본 역할 설명에서 배제한다.
2. 인용의 실제 행과 현재 서버 주소를 연결하고, 핵심 역할 요약·helper 근거 수집·진단 분류를 보강한다.
3. JDT fixture와 벤치마크 스키마를 고치고, 대형 요약 시간 제한 원인을 측정·최적화한 뒤 실패 3개 재검증.
4. 일반 저장소·긴 파일의 설명 품질 및 에이전트 ID 매핑을 재검증한다.
5. 그다음 사용자와 실제 Obsidian 보관함 등록→노트 열기→위키 이동→변경분 갱신을 확인한다.

현재는 ‘기능 검사와 품질 판정 완료’이지 ‘결함 수정·최종 품질 인수 완료’가 아니다.

## 보존 자료

- `artifacts/quality-acceptance-20261002-1601/`: 실제 원본 복사본·격리 DB·최초/최종 JSON·사용자 수정 보호 상태의 테스트 보관함.
- `artifacts/quality-test-build/`: 검사 전용 시작 옵션 포함 후보. 운영에 배포하지 않았다.
- .NET 자료: `C:/Users/surromind/AppData/Local/Temp/smsr-qa-dotnet-8b64853958b64d2aa17f03e526df2e24/`.
- 실행: 기존 `node scripts/<검사명>.mjs`, 앱 runtime `python.exe -B scripts/<검사명>.py`, 배포 앱의 위 CLI 옵션, `dotnet build src/SMSR.App/SMSR.App.csproj -c Release -o artifacts/quality-test-build --no-restore`, 숨김 `--graph-quality-preview <host.json>`, `python scripts/verify-graph-backup.py`, `python scripts/verify_graph_preservation.py <snapshot.db>`, 임시 localhost 동일 출처 보관함 API 및 실제 브라우저 조작.

