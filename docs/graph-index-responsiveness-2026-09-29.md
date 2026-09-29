# 실제 5,000파일 색인 중 응답성 — 2026-09-29

결론: **새 임시 저장소의 Markdown 5,000개를 색인하는 동안 HTTP health와 WPF Input 큐 응답 기준을 게시본 3회 모두 충족했다.** 실제 창 렌더링·사용자 입력이나 전체16언어 정밀 분석의 완료 판정은 아니다.

## 변경 이유와 검사 범위

기존 `--graph-responsiveness-self-test`의 1,000문서·서버 health 검사를 확장했다. SQL 파일 기록만 넣는 [대형 조회 검사](graph-large-performance-2026-09-29.md)와 달리 실제 파일 생성→공개 색인 API→파싱→관계 해소→SQLite 저장 경로를 실행한다. Ponytail/Karpathy 원칙에 따라 기존 서버·색인 API·WPF Dispatcher를 재사용했으며 새 의존성은 없다.

- 파일마다 파일 노드1개와 제목9개, CONTAINS9개와 LINKS_TO31개: 총50,000노드·200,000관계다. 문서 링크는 다음31개 파일로 순환한다.
- 첫 색인: ChangedFiles=5000, RemovedFiles=0, Unchanged=false, Revision=1. 변경 없는 두 번째 색인: ChangedFiles=0, RemovedFiles=0, Unchanged=true, Revision=1을 요구한다.
- health의 전체 개수와 dangling/중복/self-loop/issue=0을 확인한다. 경계 파일0/4999의 들어오는31개 소유 경로, 나가는9개 제목과 총18제목의 줄 번호·정확한 링크 대상·해소 상태를 대조한다. 모든 파일의 모든 관계를 독립 대조한 것은 아니다.
- 매 반복 새 임시 Git 저장소·DB·프로세스·임의 loopback 포트를 사용한다. 운영 DB/실행 앱은 교체하지 않았다. 검증 임시 폴더만 종료 후 삭제하며 남은 폴더가 없음을 확인했다.

## 측정·판정 방법

색인 HTTP 요청 시작부터 응답 JSON 해석까지가 IndexMs다. 파일 생성, 이후 health 및 관계 대조 시간은 제외한다. 첫 색인 직후 같은 프로세스에서 변경 없는 재검사를 측정한다.

독립 작업 두 개가 각각 HTTP `/api/health`와 WPF `DispatcherPriority.Input`의 빈 작업 완료 시간을 잰다. 각 응답 뒤25ms 쉬며, 색인 완료를 관찰하면 끝낸다. 마지막으로 시작한 응답은 색인 종료 직후에 끝날 수 있다. WPF 주 스레드의 Dispatcher인지 검사하며 모든 색인/측정 작업이 종료된 뒤 정리한다.

- 각각 최소5표본, 유한한 비음수 값, HTTP 최대≤2000ms, WPF 큐 최대≤200ms를 요구한다. 큐 대기5초 초과도 실패한다.
- p95는 정렬 표본의 `ceil(표본수*0.95)`번째 값이다. 지연/표본수/NaN 오류의 음성 검사와 p95 양성 검사를 포함한다.
- WPF200ms는 이번 회귀 검사의 보호 기준이다. 사용자와 합의한 전체 화면 UX 기준의 대체물이 아니다. 창·렌더링·실제 키보드/마우스 입력은 실행하지 않는다.

## 게시본 측정 결과

시각은 KST, 지연 단위는 ms다. 표본 수는 HTTP/WPF 순서다.

| 완료 시각 | 단계 | 색인 시간 | 표본 수 | HTTP p95 / 최대 | WPF 큐 p95 / 최대 |
|---|---|---:|---:|---:|---:|
| 07:04:45 | 첫 색인 | 12090.51 | 392 / 397 | 0.7760 / 134.3159 | 0.5432 / 1.3563 |
| 07:04:45 | 변경 없음 | 1719.64 | 57 / 57 | 0.5983 / 1.7330 | 0.6651 / 0.8617 |
| 07:05:09 | 첫 색인 | 12904.36 | 418 / 421 | 0.7683 / 132.0740 | 0.4929 / 16.8113 |
| 07:05:09 | 변경 없음 | 1733.31 | 57 / 57 | 0.4869 / 0.5141 | 0.5243 / 0.6635 |
| 07:05:29 | 첫 색인 | 8881.65 | 286 / 291 | 0.8738 / 130.4272 | 0.5870 / 1.9133 |
| 07:05:29 | 변경 없음 | 1696.41 | 56 / 56 | 0.5178 / 0.6166 | 0.4848 / 0.5687 |

각 실행 DB 본체145,391,616바이트. WAL/SHM·프로세스 메모리는 합산하지 않았다. Windows11 Pro10.0.26200, i5-12400F, 논리12CPU, RAM17,026,113,536바이트에서 측정했다. OS 캐시는 지우지 않았고 다른 사용자 앱의 부하는 통제하지 않았다. 이 작업의 다른 무거운 회귀 검사는 3회 측정 후 실행했다.

게시 경로 `artifacts/verify-p2-index-responsive`, 관리 코드 `SMSR.App.dll` SHA-256: `362098B1B9011DE9506E9776C428A31A65BE5743AE0A62B9759E1CB83744EB3B`.

Debug 탐색 측정은 첫 색인9122.63ms/재검사1871.03ms였고 최종 관계 표본 검사를 추가하기 전의 결과다. 게시본 결과와 합산하지 않는다. 최종 게시본의 기존 그래프 자체검사와 대형 HTTP 정답/지연 검사도 통과했다(경로p95 88.38ms, 영향p95 90.00ms).

## 재현

```powershell
dotnet publish src/SMSR.App/SMSR.App.csproj -c Release --no-restore -o artifacts/verify-p2-index-responsive
$app=(Resolve-Path artifacts/verify-p2-index-responsive/SMSR.App.exe).Path
$result=Start-Process -FilePath $app -ArgumentList '--graph-responsiveness-self-test' -WindowStyle Hidden -Wait -PassThru
if($result.ExitCode -ne 0){throw 'Inspect smsr-graph-responsiveness-error.txt in TEMP'}
Get-Content "$env:TEMP/smsr-p2-index-responsiveness.json"
```

설치된 Git이 필요하다. 먼저 종료코드0을 확인하고 보고서 version=2와 실행 시각을 대조한다. 중간 실패 시 이전 JSON이 남을 수 있어 파일 존재만으로 성공 판정하지 않는다. 새 빌드의 DLL 해시는 달라질 수 있다.

## 남은 범위

실제 사용자 대형 저장소·언어 컴파일러 혼합 부하, 파일 변경/삭제 증분 부하, 색인 중 경로/영향 질의·화면 렌더링, 취소/장시간·메모리·고차수 허브·냉캐시는 별도 검증이 필요하다. 이번 결과는 빈 WPF 입력 큐 처리와 health 응답을 검증하며 실제 화면 프레임이나 사용자 조작을 증명하지 않는다. SQLite 교체 근거는 없으며 전용 DB는 실제 요구 부하에서 같은 정답과 조건으로 비교 후 결정한다. 전체16언어 정밀 의미 분석·완전한 PDG/taint·P2 최종 수용은 계속 진행 중이다.
