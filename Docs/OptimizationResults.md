# 최적화 비교 결과

측정일: 2026-10-19. 중단 전에 준비된 Build-v2의 원본·개선 경로를 같은 실행 파일에서 각 3회 측정했다. core 20개 시나리오, 60개 캡처가 실행 상태와 측정 마커 검증을 통과했다. 기존 Build 및 에디터 수치는 이번 결과에 합치지 않았다.

지속 접촉 30명 조건의 PlayerLoop 할당은 83.3%, 동시 조합 10쌍의 최대 단일 호출 시간은 43.5%, 정지 적 60명의 탐색 호출당 시간은 69.7% 감소했다. 동시 조합 이벤트 프레임의 최대 시간은 실행별 중앙값 185.158 → 127.430ms로 줄었지만 60FPS 예산을 크게 넘는다. 비용 감소는 확인했으며, 순간 끊김 해소와 일반 게임 적용은 남은 과제다.

## 환경과 근거

- Unity 6000.3.22f1, Windows Development Player, Mono, batchmode + graphics, Direct3D12, 품질 Ultra.
- GPU: NVIDIA GeForce RTX 4060 Laptop GPU. 해상도 1280×720, VSync 0, 목표 120FPS, Fixed Timestep 약 0.02초.
- Deep Profile OFF, Demo UI OFF. Setup·워밍업·Cleanup 및 측정 경계 프레임은 성능 통계에서 제외했다. 캡처를 모두 마친 뒤 별도 Unity 프로세스에서 분석했다.
- 원본 알고리즘 기준 커밋: `7c1507759af50b18f25fc970a40bd6e00225cc7c`. 재개 시 저장소 HEAD: `f1c8178fe39b044553d66f3c1dcd62152f74ffa3`.
- 재사용한 빌드는 2026-10-16에 생성됐다. 위 HEAD와 바이너리의 정확한 소스 일치는 별도 빌드 메타데이터로 증명하지 않았으며, 실행한 바이너리는 아래 해시로 식별한다.
- 실행 파일 SHA-256: `4e015f1b6628d598b2983a999f1004634c40da3a9454de9fa2f9542f0fa1318f`.
- Assembly-CSharp.dll SHA-256: `f45a38b8441554e62fbef7b3f0e96c02b58deaede8960d0e49a6df9cda7a820f`.
- 실행 journal: `Logs/Optimization/player-v2-core-20261019.json`.
- 분석: `Logs/Optimization/player-v2-analysis-20261019/summary.json`, 요약: `Logs/Optimization/player-v2-results.json`.
- raw 캡처는 각 journal 항목의 `runDirectory`, 실행별 manifest 사본은 같은 이름의 `_runs/*_run.json`에 있다. Logs와 로컬 raw는 Git 제외 대상이다. 이 문서는 반복별 핵심 수치와 manifest 해시를 보존한다.

## 주요 비교

각 값은 3회 실행별 지표의 중앙값이다. 감소율은 반올림 전 값으로 계산했다. inclusive 구간은 겹치므로 합산하지 않는다. GC는 할당량이며 수집 시간이나 잔존 메모리량이 아니다.

| 조건 | 지표 | 원본 | 개선 | 감소율 |
|---|---|---:|---:|---:|
| 지속 접촉 30명 / 상태 조회만 | HasStatus bytes/call | 211.986 | 0.000 | 100.0% |
| 지속 접촉 30명 / 타임라인만 | Synchronize inclusive bytes/call | 2262.573 | 376.463 | 83.4% |
| 지속 접촉 30명 / 전체 개선 | PlayerLoop KiB/frame | 26.334 | 4.402 | 83.3% |
| 동시 조합 10쌍 | 마스크 inclusive ms/실행 | 221.505 | 137.053 | 38.1% |
| 동시 조합 10쌍 | 조합 최대 단일 호출 ms | 24.325 | 13.742 | 43.5% |
| 동시 조합 10쌍 | 조합 이벤트 프레임 최대 ms | 185.158 | 127.430 | 31.2% |
| 단순 벽 조합 | 조합 최대 단일 호출 ms | 15.299 | 8.321 | 45.6% |
| 복잡 모서리 조합 | 조합 최대 단일 호출 ms | 20.034 | 10.168 | 49.2% |
| 이동 적 60명 | 탐색 µs/call | 12.347 | 6.593 | 46.6% |
| 정지 적 60명 | 탐색 µs/call | 15.094 | 4.569 | 69.7% |
| 최소 전투 / 전체 개선 | PlayerLoop KiB/frame | 1.637 | 0.793 | 51.5% |

## 해석과 적용 범위

- 상태 조회의 iterator 할당 제거와 타임라인의 버퍼 재사용을 별도로 비교했다. 전체 개선은 적 탐색과 마스크 개선도 포함하므로 전체 CPU 변화를 상태 조회 하나의 성과로 귀속하지 않는다.
- 마스크 해상도 256과 픽셀 판정식을 유지한 동일 footprint 공유 실험이다. 최초 마스크 생성·native 클리핑·형상·VFX 비용은 남으며 GPU 시간이나 배칭 개선을 입증하지 않는다.
- 적 탐색은 단일 플레이어의 콜라이더 레이어가 고정된 데모 조건이다. 탐색 주기를 줄이지 않았다. 아래 호출 수와 처리량도 함께 확인해야 하며, 다중 플레이어·동적 레이어 변경에 대한 일반 적용은 보류한다.
- 120FPS 제한으로 프레임 시간에는 약 8.33ms의 표시 대기가 포함된다. 함수 비용 감소를 FPS 상승률로 바꾸어 표현하지 않는다. 조합 순간 끊김은 p95만으로 드러나지 않을 수 있어 최대·p99·예산 초과 프레임과 함께 본다.
- 동일 시드라도 시간 기반 실행은 완전히 결정적이지 않다. 측정 종료 경계의 피해 틱 수, 이동 거리와 공격 횟수가 달라질 수 있다. 기능 유효 판정은 모든 피해·공격 결과의 완전 일치를 뜻하지 않는다. 아래 반복별 작업량을 보존하며 전체 프레임·프레임당 지표는 그 한계 안에서 해석한다.
- 2026-10-16 기능 비교 검증 기록은 53,484 assertions, Passed다. 이번 재개에서는 해당 단위 검증을 다시 실행하지 않았으며, 새 빌드 실행의 기능 판정과 캡처 유효성은 새로 확인했다. 할당 카운터가 지원되지 않아 단위 검증의 0 bytes는 성능 근거로 쓰지 않는다.
- 검증 범위는 core 시나리오다. 대표·최대 전투, 전체 적 수 조합과 장기 생명주기 suite는 이번 빌드에서 측정하지 않았다. 공유 텍스처·버퍼의 장시간 상주 메모리 검증과 실제 게임 기본 적용은 별도 과제다.
- 일반 게임의 기본 선택은 Original이며 두 버전은 보존했다. 이번 변경은 측정·분석 도구와 결과 기록에 한정한다.

## 반복별 성능과 작업량

각 배열은 반복 1·2·3 순서다. `gcB/f`는 bytes/frame, `gcB/call`은 inclusive bytes/call이다. 없는 마커는 미측정으로 표시한다. 시간 단위는 별도 표시가 없으면 ms다.

### S7_Area_ContactEffects_30_Sustained_Original

| 지표 | 반복 1 / 2 / 3 |
|---|---|
| 프레임 수 | 839 / 839 / 839 |
| 전체 프레임 p95 | 8.363 / 8.367 / 8.369 |
| 전체 프레임 p99 | 8.480 / 8.553 / 8.449 |
| 전체 프레임 최대 | 8.701 / 9.255 / 8.613 |
| 16.67ms 초과 프레임 | 0 / 0 / 0 |
| 33.33ms 초과 프레임 | 0 / 0 / 0 |
| PlayerLoop gcB/f | 26993.354 / 26948.300 / 26965.569 |
| HasStatus gcB/call | 212.253 / 211.719 / 211.986 |
| 타임라인 gcB/call | 2264.261 / 2262.573 / 2262.208 |
| 마스크 inclusive ms/실행 | 0.001 / 0.001 / 0.001 |
| 마스크 GC bytes/실행 | 0 / 0 / 0 |
| 조합 최대 단일 호출 | 미측정 |
| 조합 이벤트 프레임 최대 | 미측정 |
| 조합 이벤트 프레임 수 | 미측정 |
| 조합 이벤트 16.67ms 초과 | 미측정 |
| 조합 이벤트 33.33ms 초과 | 미측정 |
| 탐색 µs/call | 8.649 / 11.114 / 8.916 |
| 탐색 calls/frame | 9.690 / 9.654 / 9.511 |
| FixedUpdate µs/call | 13.274 / 15.983 / 13.474 |
| FixedUpdate ms/frame | 0.129 / 0.154 / 0.128 |
| effectiveSeed | 1014 / 1015 / 1016 |
| enemiesSpawned | 30 / 30 / 30 |
| enemyDeaths | 0 / 0 / 0 |
| combinations | 0 / 0 / 0 |
| potionDamageEvents | 330 / 330 / 330 |
| enemyDamageEvents | 330 / 330 / 330 |
| playerDamageEvents | 112 / 112 / 112 |
| meleeAttacks | 0 / 0 / 0 |
| playerThrows | 0 / 0 / 0 |
| playerDistance | 0.000 / 0.000 / 0.000 |
| enemyDistance | 0.000 / 0.000 / 0.000 |
| cycles | 1 / 1 / 1 |
| outcome | IntervalCompleted / IntervalCompleted / IntervalCompleted |
| functionalValid | True / True / True |

실행 ID: 20261019_001658_825_d60e0268.

Manifest SHA-256: `4161d7e0ce806f4b7bf665ff8ff4d18811df3ab50987354ae70a47bd2dcc7792`.

원본 raw: `S7_Area_ContactEffects_30_Sustained_Original_repeat01.raw`, `S7_Area_ContactEffects_30_Sustained_Original_repeat02.raw`, `S7_Area_ContactEffects_30_Sustained_Original_repeat03.raw`.

### S7_Area_ContactEffects_30_Sustained_StatusQuery

| 지표 | 반복 1 / 2 / 3 |
|---|---|
| 프레임 수 | 839 / 839 / 839 |
| 전체 프레임 p95 | 8.372 / 8.383 / 8.358 |
| 전체 프레임 p99 | 8.462 / 8.468 / 8.454 |
| 전체 프레임 최대 | 8.626 / 8.703 / 8.975 |
| 16.67ms 초과 프레임 | 0 / 0 / 0 |
| 33.33ms 초과 프레임 | 0 / 0 / 0 |
| PlayerLoop gcB/f | 6377.337 / 6412.455 / 6394.238 |
| HasStatus gcB/call | 0.000 / 0.000 / 0.000 |
| 타임라인 gcB/call | 2227.541 / 2257.848 / 2256.232 |
| 마스크 inclusive ms/실행 | 0.001 / 0.001 / 0.001 |
| 마스크 GC bytes/실행 | 0 / 0 / 0 |
| 조합 최대 단일 호출 | 미측정 |
| 조합 이벤트 프레임 최대 | 미측정 |
| 조합 이벤트 프레임 수 | 미측정 |
| 조합 이벤트 16.67ms 초과 | 미측정 |
| 조합 이벤트 33.33ms 초과 | 미측정 |
| 탐색 µs/call | 8.103 / 10.266 / 9.411 |
| 탐색 calls/frame | 9.476 / 9.511 / 9.654 |
| FixedUpdate µs/call | 12.516 / 14.804 / 14.277 |
| FixedUpdate ms/frame | 0.119 / 0.141 / 0.138 |
| effectiveSeed | 1014 / 1015 / 1016 |
| enemiesSpawned | 30 / 30 / 30 |
| enemyDeaths | 0 / 0 / 0 |
| combinations | 0 / 0 / 0 |
| potionDamageEvents | 300 / 330 / 330 |
| enemyDamageEvents | 300 / 330 / 330 |
| playerDamageEvents | 111 / 112 / 112 |
| meleeAttacks | 0 / 0 / 0 |
| playerThrows | 0 / 0 / 0 |
| playerDistance | 0.000 / 0.000 / 0.000 |
| enemyDistance | 0.000 / 0.000 / 0.000 |
| cycles | 1 / 1 / 1 |
| outcome | IntervalCompleted / IntervalCompleted / IntervalCompleted |
| functionalValid | True / True / True |

실행 ID: 20261019_001729_899_33b6b941.

Manifest SHA-256: `8fb437daa8fc3b3b7a296815cddb434397c76485941ab2c73b504beda9ed1aba`.

원본 raw: `S7_Area_ContactEffects_30_Sustained_StatusQuery_repeat01.raw`, `S7_Area_ContactEffects_30_Sustained_StatusQuery_repeat02.raw`, `S7_Area_ContactEffects_30_Sustained_StatusQuery_repeat03.raw`.

### S7_Area_ContactEffects_30_Sustained_Timeline

| 지표 | 반복 1 / 2 / 3 |
|---|---|
| 프레임 수 | 839 / 839 / 839 |
| 전체 프레임 p95 | 8.410 / 8.426 / 8.387 |
| 전체 프레임 p99 | 8.559 / 8.615 / 8.577 |
| 전체 프레임 최대 | 9.119 / 8.788 / 8.701 |
| 16.67ms 초과 프레임 | 0 / 0 / 0 |
| 33.33ms 초과 프레임 | 0 / 0 / 0 |
| PlayerLoop gcB/f | 25142.198 / 25104.791 / 25141.964 |
| HasStatus gcB/call | 212.253 / 212.253 / 212.253 |
| 타임라인 gcB/call | 376.463 / 350.266 / 377.137 |
| 마스크 inclusive ms/실행 | 0.001 / 0.001 / 0.001 |
| 마스크 GC bytes/실행 | 0 / 0 / 0 |
| 조합 최대 단일 호출 | 미측정 |
| 조합 이벤트 프레임 최대 | 미측정 |
| 조합 이벤트 프레임 수 | 미측정 |
| 조합 이벤트 16.67ms 초과 | 미측정 |
| 조합 이벤트 33.33ms 초과 | 미측정 |
| 탐색 µs/call | 8.945 / 10.511 / 8.108 |
| 탐색 calls/frame | 9.583 / 9.476 / 9.690 |
| FixedUpdate µs/call | 13.560 / 14.974 / 12.534 |
| FixedUpdate ms/frame | 0.130 / 0.142 / 0.121 |
| effectiveSeed | 1014 / 1015 / 1016 |
| enemiesSpawned | 30 / 30 / 30 |
| enemyDeaths | 0 / 0 / 0 |
| combinations | 0 / 0 / 0 |
| potionDamageEvents | 330 / 300 / 330 |
| enemyDamageEvents | 330 / 300 / 330 |
| playerDamageEvents | 112 / 111 / 112 |
| meleeAttacks | 0 / 0 / 0 |
| playerThrows | 0 / 0 / 0 |
| playerDistance | 0.000 / 0.000 / 0.000 |
| enemyDistance | 0.000 / 0.000 / 0.000 |
| cycles | 1 / 1 / 1 |
| outcome | IntervalCompleted / IntervalCompleted / IntervalCompleted |
| functionalValid | True / True / True |

실행 ID: 20261019_001759_709_316c971a.

Manifest SHA-256: `d65a39a8d8dc9a1ea2dca3dced4a303cc9c0ac1899660c51d0ae5213de5d68f7`.

원본 raw: `S7_Area_ContactEffects_30_Sustained_Timeline_repeat01.raw`, `S7_Area_ContactEffects_30_Sustained_Timeline_repeat02.raw`, `S7_Area_ContactEffects_30_Sustained_Timeline_repeat03.raw`.

### S7_Area_ContactEffects_30_Sustained_Improved

| 지표 | 반복 1 / 2 / 3 |
|---|---|
| 프레임 수 | 839 / 839 / 839 |
| 전체 프레임 p95 | 8.375 / 8.377 / 8.365 |
| 전체 프레임 p99 | 8.542 / 8.566 / 8.498 |
| 전체 프레임 최대 | 9.104 / 9.312 / 8.618 |
| 16.67ms 초과 프레임 | 0 / 0 / 0 |
| 33.33ms 초과 프레임 | 0 / 0 / 0 |
| PlayerLoop gcB/f | 4481.707 / 4510.107 / 4507.547 |
| HasStatus gcB/call | 0.000 / 0.000 / 0.000 |
| 타임라인 gcB/call | 341.879 / 367.947 / 368.483 |
| 마스크 inclusive ms/실행 | 0.000 / 0.001 / 0.000 |
| 마스크 GC bytes/실행 | 0 / 0 / 0 |
| 조합 최대 단일 호출 | 미측정 |
| 조합 이벤트 프레임 최대 | 미측정 |
| 조합 이벤트 프레임 수 | 미측정 |
| 조합 이벤트 16.67ms 초과 | 미측정 |
| 조합 이벤트 33.33ms 초과 | 미측정 |
| 탐색 µs/call | 3.551 / 3.766 / 3.848 |
| 탐색 calls/frame | 9.511 / 9.690 / 9.690 |
| FixedUpdate µs/call | 7.912 / 8.149 / 8.436 |
| FixedUpdate ms/frame | 0.075 / 0.079 / 0.082 |
| effectiveSeed | 1014 / 1015 / 1016 |
| enemiesSpawned | 30 / 30 / 30 |
| enemyDeaths | 0 / 0 / 0 |
| combinations | 0 / 0 / 0 |
| potionDamageEvents | 330 / 330 / 330 |
| enemyDamageEvents | 330 / 330 / 330 |
| playerDamageEvents | 112 / 112 / 112 |
| meleeAttacks | 0 / 0 / 0 |
| playerThrows | 0 / 0 / 0 |
| playerDistance | 0.000 / 0.000 / 0.000 |
| enemyDistance | 0.000 / 0.000 / 0.000 |
| cycles | 1 / 1 / 1 |
| outcome | IntervalCompleted / IntervalCompleted / IntervalCompleted |
| functionalValid | True / True / True |

실행 ID: 20261019_001829_731_537ca523.

Manifest SHA-256: `fe2f7746ddb1f4e15be3d14889535352650dbe3335db5895bdb0ad93396f7c5d`.

원본 raw: `S7_Area_ContactEffects_30_Sustained_Improved_repeat01.raw`, `S7_Area_ContactEffects_30_Sustained_Improved_repeat02.raw`, `S7_Area_ContactEffects_30_Sustained_Improved_repeat03.raw`.

### S7_Area_ContactEffects_30

| 지표 | 반복 1 / 2 / 3 |
|---|---|
| 프레임 수 | 836 / 838 / 838 |
| 전체 프레임 p95 | 8.498 / 8.411 / 8.394 |
| 전체 프레임 p99 | 8.613 / 8.570 / 8.516 |
| 전체 프레임 최대 | 18.930 / 13.910 / 14.141 |
| 16.67ms 초과 프레임 | 1 / 0 / 0 |
| 33.33ms 초과 프레임 | 0 / 0 / 0 |
| PlayerLoop gcB/f | 17660.684 / 17709.000 / 17365.573 |
| HasStatus gcB/call | 205.398 / 206.643 / 205.145 |
| 타임라인 gcB/call | 2068.422 / 2067.981 / 2048.363 |
| 마스크 inclusive ms/실행 | 0.000 / 0.000 / 0.001 |
| 마스크 GC bytes/실행 | 0 / 0 / 0 |
| 조합 최대 단일 호출 | 미측정 |
| 조합 이벤트 프레임 최대 | 미측정 |
| 조합 이벤트 프레임 수 | 미측정 |
| 조합 이벤트 16.67ms 초과 | 미측정 |
| 조합 이벤트 33.33ms 초과 | 미측정 |
| 탐색 µs/call | 11.169 / 10.643 / 7.830 |
| 탐색 calls/frame | 5.957 / 5.943 / 5.943 |
| FixedUpdate µs/call | 16.312 / 15.026 / 12.533 |
| FixedUpdate ms/frame | 0.097 / 0.089 / 0.074 |
| effectiveSeed | 1014 / 1015 / 1016 |
| enemiesSpawned | 30 / 30 / 30 |
| enemyDeaths | 30 / 30 / 30 |
| combinations | 0 / 0 / 0 |
| potionDamageEvents | 180 / 180 / 180 |
| enemyDamageEvents | 210 / 210 / 210 |
| playerDamageEvents | 78 / 78 / 78 |
| meleeAttacks | 0 / 0 / 0 |
| playerThrows | 0 / 0 / 0 |
| playerDistance | 0.000 / 0.000 / 0.000 |
| enemyDistance | 0.000 / 0.000 / 0.000 |
| cycles | 1 / 1 / 1 |
| outcome | IntervalCompleted / IntervalCompleted / IntervalCompleted |
| functionalValid | True / True / True |

실행 ID: 20261019_001859_809_037b713a.

Manifest SHA-256: `689574959d10dbf5e0d820f7088887504bb8de8e47d13f4752f6d6f32f17344c`.

원본 raw: `S7_Area_ContactEffects_30_repeat01.raw`, `S7_Area_ContactEffects_30_repeat02.raw`, `S7_Area_ContactEffects_30_repeat03.raw`.

### S7_Area_ContactEffects_30_Improved

| 지표 | 반복 1 / 2 / 3 |
|---|---|
| 프레임 수 | 838 / 839 / 839 |
| 전체 프레임 p95 | 8.367 / 8.373 / 8.403 |
| 전체 프레임 p99 | 8.473 / 8.491 / 8.600 |
| 전체 프레임 최대 | 20.512 / 14.375 / 12.700 |
| 16.67ms 초과 프레임 | 1 / 0 / 0 |
| 33.33ms 초과 프레임 | 0 / 0 / 0 |
| PlayerLoop gcB/f | 3425.692 / 3482.565 / 3474.117 |
| HasStatus gcB/call | 0.000 / 0.000 / 0.000 |
| 타임라인 gcB/call | 806.873 / 810.839 / 806.244 |
| 마스크 inclusive ms/실행 | 0.000 / 0.000 / 0.000 |
| 마스크 GC bytes/실행 | 0 / 0 / 0 |
| 조합 최대 단일 호출 | 미측정 |
| 조합 이벤트 프레임 최대 | 미측정 |
| 조합 이벤트 프레임 수 | 미측정 |
| 조합 이벤트 16.67ms 초과 | 미측정 |
| 조합 이벤트 33.33ms 초과 | 미측정 |
| 탐색 µs/call | 4.445 / 4.085 / 3.674 |
| 탐색 calls/frame | 5.979 / 5.936 / 5.936 |
| FixedUpdate µs/call | 9.694 / 8.587 / 8.440 |
| FixedUpdate ms/frame | 0.058 / 0.051 / 0.050 |
| effectiveSeed | 1014 / 1015 / 1016 |
| enemiesSpawned | 30 / 30 / 30 |
| enemyDeaths | 30 / 30 / 30 |
| combinations | 0 / 0 / 0 |
| potionDamageEvents | 180 / 180 / 180 |
| enemyDamageEvents | 210 / 210 / 210 |
| playerDamageEvents | 78 / 78 / 78 |
| meleeAttacks | 0 / 0 / 0 |
| playerThrows | 0 / 0 / 0 |
| playerDistance | 0.000 / 0.000 / 0.000 |
| enemyDistance | 0.000 / 0.000 / 0.000 |
| cycles | 1 / 1 / 1 |
| outcome | IntervalCompleted / IntervalCompleted / IntervalCompleted |
| functionalValid | True / True / True |

실행 ID: 20261019_001929_507_9305c168.

Manifest SHA-256: `4c2beaece5602b0e46a9fcd5799ff273d863afd3636354be11b202d7a61f01ec`.

원본 raw: `S7_Area_ContactEffects_30_Improved_repeat01.raw`, `S7_Area_ContactEffects_30_Improved_repeat02.raw`, `S7_Area_ContactEffects_30_Improved_repeat03.raw`.

### S7_Combine_ContactEffects_10

| 지표 | 반복 1 / 2 / 3 |
|---|---|
| 프레임 수 | 838 / 839 / 839 |
| 전체 프레임 p95 | 8.392 / 8.378 / 8.394 |
| 전체 프레임 p99 | 8.542 / 8.511 / 8.483 |
| 전체 프레임 최대 | 18.399 / 8.752 / 8.728 |
| 16.67ms 초과 프레임 | 1 / 0 / 0 |
| 33.33ms 초과 프레임 | 0 / 0 / 0 |
| PlayerLoop gcB/f | 3007.418 / 2763.342 / 2753.297 |
| HasStatus gcB/call | 96.912 / 97.082 / 97.346 |
| 타임라인 gcB/call | 655.941 / 656.583 / 653.422 |
| 마스크 inclusive ms/실행 | 0.002 / 0.002 / 0.003 |
| 마스크 GC bytes/실행 | 0 / 0 / 0 |
| 조합 최대 단일 호출 | 1.629 / 1.712 / 3.208 |
| 조합 이벤트 프레임 최대 | 8.336 / 8.331 / 8.262 |
| 조합 이벤트 프레임 수 | 1 / 1 / 1 |
| 조합 이벤트 16.67ms 초과 | 0 / 0 / 0 |
| 조합 이벤트 33.33ms 초과 | 0 / 0 / 0 |
| 탐색 µs/call | 6.398 / 5.147 / 7.056 |
| 탐색 calls/frame | 1.253 / 1.263 / 1.251 |
| FixedUpdate µs/call | 10.054 / 8.635 / 11.106 |
| FixedUpdate ms/frame | 0.013 / 0.011 / 0.014 |
| effectiveSeed | 1014 / 1015 / 1016 |
| enemiesSpawned | 10 / 10 / 10 |
| enemyDeaths | 10 / 10 / 10 |
| combinations | 1 / 1 / 1 |
| potionDamageEvents | 40 / 40 / 40 |
| enemyDamageEvents | 50 / 50 / 50 |
| playerDamageEvents | 35 / 35 / 35 |
| meleeAttacks | 0 / 0 / 0 |
| playerThrows | 0 / 0 / 0 |
| playerDistance | 0.000 / 0.000 / 0.000 |
| enemyDistance | 0.000 / 0.000 / 0.000 |
| cycles | 1 / 1 / 1 |
| outcome | IntervalCompleted / IntervalCompleted / IntervalCompleted |
| functionalValid | True / True / True |

실행 ID: 20261019_001959_725_38ad6f5a.

Manifest SHA-256: `b7bd73c2ff7db12f76e19c78fcc415fce11e61b6bee486e40556b63bb6203497`.

원본 raw: `S7_Combine_ContactEffects_10_repeat01.raw`, `S7_Combine_ContactEffects_10_repeat02.raw`, `S7_Combine_ContactEffects_10_repeat03.raw`.

### S7_Combine_ContactEffects_10_Improved

| 지표 | 반복 1 / 2 / 3 |
|---|---|
| 프레임 수 | 839 / 839 / 839 |
| 전체 프레임 p95 | 8.381 / 8.397 / 8.404 |
| 전체 프레임 p99 | 8.524 / 8.503 / 8.589 |
| 전체 프레임 최대 | 13.276 / 9.844 / 8.901 |
| 16.67ms 초과 프레임 | 0 / 0 / 0 |
| 33.33ms 초과 프레임 | 0 / 0 / 0 |
| PlayerLoop gcB/f | 1324.684 / 1085.771 / 1069.294 |
| HasStatus gcB/call | 0.000 / 0.000 / 0.000 |
| 타임라인 gcB/call | 273.712 / 279.298 / 275.783 |
| 마스크 inclusive ms/실행 | 0.001 / 0.001 / 0.001 |
| 마스크 GC bytes/실행 | 0 / 0 / 0 |
| 조합 최대 단일 호출 | 1.579 / 1.462 / 1.466 |
| 조합 이벤트 프레임 최대 | 8.335 / 8.359 / 8.328 |
| 조합 이벤트 프레임 수 | 1 / 1 / 1 |
| 조합 이벤트 16.67ms 초과 | 0 / 0 / 0 |
| 조합 이벤트 33.33ms 초과 | 0 / 0 / 0 |
| 탐색 µs/call | 2.797 / 2.846 / 2.935 |
| 탐색 calls/frame | 1.251 / 1.263 / 1.251 |
| FixedUpdate µs/call | 5.994 / 6.359 / 6.149 |
| FixedUpdate ms/frame | 0.008 / 0.008 / 0.008 |
| effectiveSeed | 1014 / 1015 / 1016 |
| enemiesSpawned | 10 / 10 / 10 |
| enemyDeaths | 10 / 10 / 10 |
| combinations | 1 / 1 / 1 |
| potionDamageEvents | 40 / 40 / 40 |
| enemyDamageEvents | 50 / 50 / 50 |
| playerDamageEvents | 35 / 35 / 35 |
| meleeAttacks | 0 / 0 / 0 |
| playerThrows | 0 / 0 / 0 |
| playerDistance | 0.000 / 0.000 / 0.000 |
| enemyDistance | 0.000 / 0.000 / 0.000 |
| cycles | 1 / 1 / 1 |
| outcome | IntervalCompleted / IntervalCompleted / IntervalCompleted |
| functionalValid | True / True / True |

실행 ID: 20261019_002029_934_a6a407af.

Manifest SHA-256: `e06934425a4d5ff0e964cb4de2f28a26fd723f7019dc50620741fe85b2c03d72`.

원본 raw: `S7_Combine_ContactEffects_10_Improved_repeat01.raw`, `S7_Combine_ContactEffects_10_Improved_repeat02.raw`, `S7_Combine_ContactEffects_10_Improved_repeat03.raw`.

### S4_Combine_Burst_10

| 지표 | 반복 1 / 2 / 3 |
|---|---|
| 프레임 수 | 808 / 807 / 808 |
| 전체 프레임 p95 | 8.422 / 8.429 / 8.380 |
| 전체 프레임 p99 | 8.674 / 8.585 / 8.561 |
| 전체 프레임 최대 | 185.158 / 185.421 / 184.126 |
| 16.67ms 초과 프레임 | 3 / 3 / 3 |
| 33.33ms 초과 프레임 | 3 / 3 / 3 |
| PlayerLoop gcB/f | 11776.879 / 11789.807 / 11777.354 |
| HasStatus gcB/call | 0.000 / 0.000 / 0.000 |
| 타임라인 gcB/call | 144.096 / 144.096 / 144.096 |
| 마스크 inclusive ms/실행 | 218.870 / 225.146 / 221.505 |
| 마스크 GC bytes/실행 | 5386112 / 5386112 / 5386112 |
| 조합 최대 단일 호출 | 23.843 / 24.663 / 24.325 |
| 조합 이벤트 프레임 최대 | 185.158 / 185.421 / 184.126 |
| 조합 이벤트 프레임 수 | 2 / 2 / 2 |
| 조합 이벤트 16.67ms 초과 | 2 / 2 / 2 |
| 조합 이벤트 33.33ms 초과 | 2 / 2 / 2 |
| 탐색 µs/call | 미측정 |
| 탐색 calls/frame | 미측정 |
| FixedUpdate µs/call | 미측정 |
| FixedUpdate ms/frame | 미측정 |
| effectiveSeed | 1014 / 1015 / 1016 |
| enemiesSpawned | 0 / 0 / 0 |
| enemyDeaths | 0 / 0 / 0 |
| combinations | 10 / 10 / 10 |
| potionDamageEvents | 0 / 0 / 0 |
| enemyDamageEvents | 0 / 0 / 0 |
| playerDamageEvents | 0 / 0 / 0 |
| meleeAttacks | 0 / 0 / 0 |
| playerThrows | 0 / 0 / 0 |
| playerDistance | 0.000 / 0.000 / 0.000 |
| enemyDistance | 0.000 / 0.000 / 0.000 |
| cycles | 1 / 1 / 1 |
| outcome | IntervalCompleted / IntervalCompleted / IntervalCompleted |
| functionalValid | True / True / True |

실행 ID: 20261019_002059_676_f0aebd08.

Manifest SHA-256: `a1f7ac4a1c09d09cbdd5c74c17b8c2bfd4bfba88a4f92f6b1b52d0b9d633ad85`.

원본 raw: `S4_Combine_Burst_10_repeat01.raw`, `S4_Combine_Burst_10_repeat02.raw`, `S4_Combine_Burst_10_repeat03.raw`.

### S4_Combine_Burst_10_SharedMask

| 지표 | 반복 1 / 2 / 3 |
|---|---|
| 프레임 수 | 819 / 818 / 819 |
| 전체 프레임 p95 | 8.430 / 8.395 / 8.376 |
| 전체 프레임 p99 | 8.657 / 8.560 / 8.487 |
| 전체 프레임 최대 | 127.223 / 131.259 / 127.430 |
| 16.67ms 초과 프레임 | 3 / 3 / 3 |
| 33.33ms 초과 프레임 | 2 / 2 / 2 |
| PlayerLoop gcB/f | 9126.381 / 9135.895 / 9126.342 |
| HasStatus gcB/call | 0.000 / 0.000 / 0.000 |
| 타임라인 gcB/call | 144.094 / 144.094 / 144.094 |
| 마스크 inclusive ms/실행 | 133.460 / 137.053 / 139.983 |
| 마스크 GC bytes/실행 | 3284240 / 3284240 / 3284240 |
| 조합 최대 단일 호출 | 13.742 / 13.640 / 14.099 |
| 조합 이벤트 프레임 최대 | 127.223 / 131.259 / 127.430 |
| 조합 이벤트 프레임 수 | 2 / 2 / 2 |
| 조합 이벤트 16.67ms 초과 | 2 / 2 / 2 |
| 조합 이벤트 33.33ms 초과 | 2 / 2 / 2 |
| 탐색 µs/call | 미측정 |
| 탐색 calls/frame | 미측정 |
| FixedUpdate µs/call | 미측정 |
| FixedUpdate ms/frame | 미측정 |
| effectiveSeed | 1014 / 1015 / 1016 |
| enemiesSpawned | 0 / 0 / 0 |
| enemyDeaths | 0 / 0 / 0 |
| combinations | 10 / 10 / 10 |
| potionDamageEvents | 0 / 0 / 0 |
| enemyDamageEvents | 0 / 0 / 0 |
| playerDamageEvents | 0 / 0 / 0 |
| meleeAttacks | 0 / 0 / 0 |
| playerThrows | 0 / 0 / 0 |
| playerDistance | 0.000 / 0.000 / 0.000 |
| enemyDistance | 0.000 / 0.000 / 0.000 |
| cycles | 1 / 1 / 1 |
| outcome | IntervalCompleted / IntervalCompleted / IntervalCompleted |
| functionalValid | True / True / True |

실행 ID: 20261019_002129_743_a8b97d10.

Manifest SHA-256: `0fcaf45aa8d77e006a9c7c3efff549cd8375bd990e58707851bee457b8a4ab16`.

원본 raw: `S4_Combine_Burst_10_SharedMask_repeat01.raw`, `S4_Combine_Burst_10_SharedMask_repeat02.raw`, `S4_Combine_Burst_10_SharedMask_repeat03.raw`.

### S4_Combine_SimpleWall

| 지표 | 반복 1 / 2 / 3 |
|---|---|
| 프레임 수 | 836 / 836 / 835 |
| 전체 프레임 p95 | 8.366 / 8.382 / 8.384 |
| 전체 프레임 p99 | 8.451 / 8.594 / 8.473 |
| 전체 프레임 최대 | 42.182 / 40.012 / 41.173 |
| 16.67ms 초과 프레임 | 1 / 1 / 1 |
| 33.33ms 초과 프레임 | 1 / 1 / 1 |
| PlayerLoop gcB/f | 1939.695 / 1686.996 / 1688.519 |
| HasStatus gcB/call | 0.000 / 0.000 / 0.000 |
| 타임라인 gcB/call | 144.090 / 144.090 / 144.090 |
| 마스크 inclusive ms/실행 | 31.518 / 31.389 / 32.391 |
| 마스크 GC bytes/실행 | 793216 / 793216 / 793216 |
| 조합 최대 단일 호출 | 15.299 / 15.014 / 15.405 |
| 조합 이벤트 프레임 최대 | 42.182 / 40.012 / 41.173 |
| 조합 이벤트 프레임 수 | 1 / 1 / 1 |
| 조합 이벤트 16.67ms 초과 | 1 / 1 / 1 |
| 조합 이벤트 33.33ms 초과 | 1 / 1 / 1 |
| 탐색 µs/call | 미측정 |
| 탐색 calls/frame | 미측정 |
| FixedUpdate µs/call | 미측정 |
| FixedUpdate ms/frame | 미측정 |
| effectiveSeed | 1014 / 1015 / 1016 |
| enemiesSpawned | 0 / 0 / 0 |
| enemyDeaths | 0 / 0 / 0 |
| combinations | 1 / 1 / 1 |
| potionDamageEvents | 0 / 0 / 0 |
| enemyDamageEvents | 0 / 0 / 0 |
| playerDamageEvents | 0 / 0 / 0 |
| meleeAttacks | 0 / 0 / 0 |
| playerThrows | 0 / 0 / 0 |
| playerDistance | 0.000 / 0.000 / 0.000 |
| enemyDistance | 0.000 / 0.000 / 0.000 |
| cycles | 1 / 1 / 1 |
| outcome | IntervalCompleted / IntervalCompleted / IntervalCompleted |
| functionalValid | True / True / True |

실행 ID: 20261019_002159_824_f5c00bbe.

Manifest SHA-256: `92888f5e63662e1dfe8dccd1bd9dc1e041ecb170f1942d89145f27f9cf873a93`.

원본 raw: `S4_Combine_SimpleWall_repeat01.raw`, `S4_Combine_SimpleWall_repeat02.raw`, `S4_Combine_SimpleWall_repeat03.raw`.

### S4_Combine_SimpleWall_SharedMask

| 지표 | 반복 1 / 2 / 3 |
|---|---|
| 프레임 수 | 837 / 837 / 837 |
| 전체 프레임 p95 | 8.386 / 8.396 / 8.400 |
| 전체 프레임 p99 | 8.550 / 8.596 / 8.574 |
| 전체 프레임 최대 | 30.796 / 27.513 / 27.628 |
| 16.67ms 초과 프레임 | 1 / 1 / 1 |
| 33.33ms 초과 프레임 | 0 / 0 / 0 |
| PlayerLoop gcB/f | 1555.578 / 1301.901 / 1301.901 |
| HasStatus gcB/call | 0.000 / 0.000 / 0.000 |
| 타임라인 gcB/call | 144.090 / 144.090 / 144.090 |
| 마스크 inclusive ms/실행 | 20.050 / 17.167 / 18.985 |
| 마스크 GC bytes/실행 | 471568 / 471568 / 471568 |
| 조합 최대 단일 호출 | 9.829 / 8.008 / 8.321 |
| 조합 이벤트 프레임 최대 | 30.796 / 27.513 / 27.628 |
| 조합 이벤트 프레임 수 | 1 / 1 / 1 |
| 조합 이벤트 16.67ms 초과 | 1 / 1 / 1 |
| 조합 이벤트 33.33ms 초과 | 0 / 0 / 0 |
| 탐색 µs/call | 미측정 |
| 탐색 calls/frame | 미측정 |
| FixedUpdate µs/call | 미측정 |
| FixedUpdate ms/frame | 미측정 |
| effectiveSeed | 1014 / 1015 / 1016 |
| enemiesSpawned | 0 / 0 / 0 |
| enemyDeaths | 0 / 0 / 0 |
| combinations | 1 / 1 / 1 |
| potionDamageEvents | 0 / 0 / 0 |
| enemyDamageEvents | 0 / 0 / 0 |
| playerDamageEvents | 0 / 0 / 0 |
| meleeAttacks | 0 / 0 / 0 |
| playerThrows | 0 / 0 / 0 |
| playerDistance | 0.000 / 0.000 / 0.000 |
| enemyDistance | 0.000 / 0.000 / 0.000 |
| cycles | 1 / 1 / 1 |
| outcome | IntervalCompleted / IntervalCompleted / IntervalCompleted |
| functionalValid | True / True / True |

실행 ID: 20261019_002229_857_53052183.

Manifest SHA-256: `1492abd2fc748bacaa3dfb8d06e10b2fcee0b6ef80e4f37289c32d3c15c56578`.

원본 raw: `S4_Combine_SimpleWall_SharedMask_repeat01.raw`, `S4_Combine_SimpleWall_SharedMask_repeat02.raw`, `S4_Combine_SimpleWall_SharedMask_repeat03.raw`.

### S4_Combine_ComplexCorner

| 지표 | 반복 1 / 2 / 3 |
|---|---|
| 프레임 수 | 834 / 835 / 835 |
| 전체 프레임 p95 | 8.387 / 8.380 / 8.398 |
| 전체 프레임 p99 | 8.542 / 8.542 / 8.518 |
| 전체 프레임 최대 | 52.371 / 49.180 / 44.660 |
| 16.67ms 초과 프레임 | 1 / 1 / 1 |
| 33.33ms 초과 프레임 | 1 / 1 / 1 |
| PlayerLoop gcB/f | 1960.219 / 1705.024 / 1705.024 |
| HasStatus gcB/call | 0.000 / 0.000 / 0.000 |
| 타임라인 gcB/call | 144.091 / 144.090 / 144.090 |
| 마스크 inclusive ms/실행 | 37.946 / 38.287 / 33.957 |
| 마스크 GC bytes/실행 | 793216 / 793216 / 793216 |
| 조합 최대 단일 호출 | 20.034 / 20.213 / 17.812 |
| 조합 이벤트 프레임 최대 | 52.371 / 49.180 / 44.660 |
| 조합 이벤트 프레임 수 | 1 / 1 / 1 |
| 조합 이벤트 16.67ms 초과 | 1 / 1 / 1 |
| 조합 이벤트 33.33ms 초과 | 1 / 1 / 1 |
| 탐색 µs/call | 미측정 |
| 탐색 calls/frame | 미측정 |
| FixedUpdate µs/call | 미측정 |
| FixedUpdate ms/frame | 미측정 |
| effectiveSeed | 1014 / 1015 / 1016 |
| enemiesSpawned | 0 / 0 / 0 |
| enemyDeaths | 0 / 0 / 0 |
| combinations | 1 / 1 / 1 |
| potionDamageEvents | 0 / 0 / 0 |
| enemyDamageEvents | 0 / 0 / 0 |
| playerDamageEvents | 0 / 0 / 0 |
| meleeAttacks | 0 / 0 / 0 |
| playerThrows | 0 / 0 / 0 |
| playerDistance | 0.000 / 0.000 / 0.000 |
| enemyDistance | 0.000 / 0.000 / 0.000 |
| cycles | 1 / 1 / 1 |
| outcome | IntervalCompleted / IntervalCompleted / IntervalCompleted |
| functionalValid | True / True / True |

실행 ID: 20261019_002259_841_18ad2eeb.

Manifest SHA-256: `ba96bf20d9c3111be32fefd9c76ac0b15650328cf9f64fc9e275da530c404306`.

원본 raw: `S4_Combine_ComplexCorner_repeat01.raw`, `S4_Combine_ComplexCorner_repeat02.raw`, `S4_Combine_ComplexCorner_repeat03.raw`.

### S4_Combine_ComplexCorner_SharedMask

| 지표 | 반복 1 / 2 / 3 |
|---|---|
| 프레임 수 | 837 / 837 / 837 |
| 전체 프레임 p95 | 8.371 / 8.383 / 8.418 |
| 전체 프레임 p99 | 8.525 / 8.505 / 8.583 |
| 전체 프레임 최대 | 30.593 / 30.690 / 28.516 |
| 16.67ms 초과 프레임 | 1 / 1 / 1 |
| 33.33ms 초과 프레임 | 0 / 0 / 0 |
| PlayerLoop gcB/f | 1572.809 / 1319.476 / 1319.303 |
| HasStatus gcB/call | 0.000 / 0.000 / 0.000 |
| 타임라인 gcB/call | 144.090 / 144.090 / 144.090 |
| 마스크 inclusive ms/실행 | 19.637 / 20.957 / 19.020 |
| 마스크 GC bytes/실행 | 471568 / 471568 / 471568 |
| 조합 최대 단일 호출 | 10.168 / 10.964 / 9.604 |
| 조합 이벤트 프레임 최대 | 30.593 / 30.690 / 28.516 |
| 조합 이벤트 프레임 수 | 1 / 1 / 1 |
| 조합 이벤트 16.67ms 초과 | 1 / 1 / 1 |
| 조합 이벤트 33.33ms 초과 | 0 / 0 / 0 |
| 탐색 µs/call | 미측정 |
| 탐색 calls/frame | 미측정 |
| FixedUpdate µs/call | 미측정 |
| FixedUpdate ms/frame | 미측정 |
| effectiveSeed | 1014 / 1015 / 1016 |
| enemiesSpawned | 0 / 0 / 0 |
| enemyDeaths | 0 / 0 / 0 |
| combinations | 1 / 1 / 1 |
| potionDamageEvents | 0 / 0 / 0 |
| enemyDamageEvents | 0 / 0 / 0 |
| playerDamageEvents | 0 / 0 / 0 |
| meleeAttacks | 0 / 0 / 0 |
| playerThrows | 0 / 0 / 0 |
| playerDistance | 0.000 / 0.000 / 0.000 |
| enemyDistance | 0.000 / 0.000 / 0.000 |
| cycles | 1 / 1 / 1 |
| outcome | IntervalCompleted / IntervalCompleted / IntervalCompleted |
| functionalValid | True / True / True |

실행 ID: 20261019_002330_039_1b9f7759.

Manifest SHA-256: `9df79acdd705f406575e6384b0875b0bcdce6b243a13b9e1065bf7c289c878a0`.

원본 raw: `S4_Combine_ComplexCorner_SharedMask_repeat01.raw`, `S4_Combine_ComplexCorner_SharedMask_repeat02.raw`, `S4_Combine_ComplexCorner_SharedMask_repeat03.raw`.

### S6_Chase_Moving_60

| 지표 | 반복 1 / 2 / 3 |
|---|---|
| 프레임 수 | 1798 / 1798 / 1798 |
| 전체 프레임 p95 | 8.401 / 8.410 / 8.427 |
| 전체 프레임 p99 | 8.548 / 8.581 / 8.568 |
| 전체 프레임 최대 | 9.152 / 8.988 / 8.835 |
| 16.67ms 초과 프레임 | 0 / 0 / 0 |
| 33.33ms 초과 프레임 | 0 / 0 / 0 |
| PlayerLoop gcB/f | 8406.736 / 8398.974 / 8407.321 |
| HasStatus gcB/call | 0.000 / 0.000 / 0.000 |
| 타임라인 gcB/call | 미측정 |
| 마스크 inclusive ms/실행 | 미측정 |
| 마스크 GC bytes/실행 | 미측정 |
| 조합 최대 단일 호출 | 미측정 |
| 조합 이벤트 프레임 최대 | 미측정 |
| 조합 이벤트 프레임 수 | 미측정 |
| 조합 이벤트 16.67ms 초과 | 미측정 |
| 조합 이벤트 33.33ms 초과 | 미측정 |
| 탐색 µs/call | 12.347 / 12.144 / 13.964 |
| 탐색 calls/frame | 19.555 / 19.655 / 19.789 |
| FixedUpdate µs/call | 17.204 / 17.110 / 19.040 |
| FixedUpdate ms/frame | 0.336 / 0.336 / 0.377 |
| effectiveSeed | 1014 / 1015 / 1016 |
| enemiesSpawned | 60 / 60 / 60 |
| enemyDeaths | 0 / 0 / 0 |
| combinations | 0 / 0 / 0 |
| potionDamageEvents | 0 / 0 / 0 |
| enemyDamageEvents | 0 / 0 / 0 |
| playerDamageEvents | 265 / 266 / 274 |
| meleeAttacks | 0 / 0 / 0 |
| playerThrows | 0 / 0 / 0 |
| playerDistance | 11.748 / 11.779 / 11.879 |
| enemyDistance | 100.268 / 100.686 / 101.306 |
| cycles | 1 / 1 / 1 |
| outcome | IntervalCompleted / IntervalCompleted / IntervalCompleted |
| functionalValid | True / True / True |

실행 ID: 20261019_002359_854_b709675e.

Manifest SHA-256: `af1710aca975aa53309f6922209791e5b72e7017fe73b74b6546e329ff9ba2e2`.

원본 raw: `S6_Chase_Moving_60_repeat01.raw`, `S6_Chase_Moving_60_repeat02.raw`, `S6_Chase_Moving_60_repeat03.raw`.

### S6_Chase_Moving_60_EnemySearch

| 지표 | 반복 1 / 2 / 3 |
|---|---|
| 프레임 수 | 1798 / 1797 / 1798 |
| 전체 프레임 p95 | 8.375 / 8.441 / 8.416 |
| 전체 프레임 p99 | 8.503 / 8.580 / 8.560 |
| 전체 프레임 최대 | 14.438 / 8.824 / 8.755 |
| 16.67ms 초과 프레임 | 0 / 0 / 0 |
| 33.33ms 초과 프레임 | 0 / 0 / 0 |
| PlayerLoop gcB/f | 8196.823 / 8176.592 / 8194.445 |
| HasStatus gcB/call | 0.000 / 0.000 / 0.000 |
| 타임라인 gcB/call | 미측정 |
| 마스크 inclusive ms/실행 | 미측정 |
| 마스크 GC bytes/실행 | 미측정 |
| 조합 최대 단일 호출 | 미측정 |
| 조합 이벤트 프레임 최대 | 미측정 |
| 조합 이벤트 프레임 수 | 미측정 |
| 조합 이벤트 16.67ms 초과 | 미측정 |
| 조합 이벤트 33.33ms 초과 | 미측정 |
| 탐색 µs/call | 6.687 / 6.388 / 6.593 |
| 탐색 calls/frame | 19.889 / 19.833 / 19.722 |
| FixedUpdate µs/call | 12.646 / 11.569 / 11.870 |
| FixedUpdate ms/frame | 0.252 / 0.229 / 0.234 |
| effectiveSeed | 1014 / 1015 / 1016 |
| enemiesSpawned | 60 / 60 / 60 |
| enemyDeaths | 0 / 0 / 0 |
| combinations | 0 / 0 / 0 |
| potionDamageEvents | 0 / 0 / 0 |
| enemyDamageEvents | 0 / 0 / 0 |
| playerDamageEvents | 281 / 276 / 273 |
| meleeAttacks | 0 / 0 / 0 |
| playerThrows | 0 / 0 / 0 |
| playerDistance | 11.936 / 11.903 / 11.846 |
| enemyDistance | 100.501 / 98.398 / 98.418 |
| cycles | 1 / 1 / 1 |
| outcome | IntervalCompleted / IntervalCompleted / IntervalCompleted |
| functionalValid | True / True / True |

실행 ID: 20261019_002453_870_3f162f4b.

Manifest SHA-256: `ad33a5b5a582d7af3294419629fbe0537fc82997fa32c056d5a3799e2163b5d7`.

원본 raw: `S6_Chase_Moving_60_EnemySearch_repeat01.raw`, `S6_Chase_Moving_60_EnemySearch_repeat02.raw`, `S6_Chase_Moving_60_EnemySearch_repeat03.raw`.

### S6_Chase_Static_60

| 지표 | 반복 1 / 2 / 3 |
|---|---|
| 프레임 수 | 1797 / 1796 / 1794 |
| 전체 프레임 p95 | 8.446 / 8.493 / 8.511 |
| 전체 프레임 p99 | 8.634 / 8.641 / 8.613 |
| 전체 프레임 최대 | 8.984 / 11.167 / 10.522 |
| 16.67ms 초과 프레임 | 0 / 0 / 0 |
| 33.33ms 초과 프레임 | 0 / 0 / 0 |
| PlayerLoop gcB/f | 8147.976 / 8148.165 / 8148.194 |
| HasStatus gcB/call | 0.000 / 0.000 / 0.000 |
| 타임라인 gcB/call | 미측정 |
| 마스크 inclusive ms/실행 | 미측정 |
| 마스크 GC bytes/실행 | 미측정 |
| 조합 최대 단일 호출 | 미측정 |
| 조합 이벤트 프레임 최대 | 미측정 |
| 조합 이벤트 프레임 수 | 미측정 |
| 조합 이벤트 16.67ms 초과 | 미측정 |
| 조합 이벤트 33.33ms 초과 | 미측정 |
| 탐색 µs/call | 14.872 / 15.094 / 16.349 |
| 탐색 calls/frame | 19.533 / 19.543 / 19.465 |
| FixedUpdate µs/call | 18.863 / 19.109 / 20.618 |
| FixedUpdate ms/frame | 0.368 / 0.373 / 0.401 |
| effectiveSeed | 1014 / 1015 / 1016 |
| enemiesSpawned | 60 / 60 / 60 |
| enemyDeaths | 0 / 0 / 0 |
| combinations | 0 / 0 / 0 |
| potionDamageEvents | 0 / 0 / 0 |
| enemyDamageEvents | 0 / 0 / 0 |
| playerDamageEvents | 312 / 312 / 310 |
| meleeAttacks | 0 / 0 / 0 |
| playerThrows | 0 / 0 / 0 |
| playerDistance | 0.000 / 0.000 / 0.000 |
| enemyDistance | 62.806 / 62.816 / 62.833 |
| cycles | 1 / 1 / 1 |
| outcome | IntervalCompleted / IntervalCompleted / IntervalCompleted |
| functionalValid | True / True / True |

실행 ID: 20261019_002547_850_25b65ace.

Manifest SHA-256: `f49440f36a6eaa6d1363f09f3a51cbe4e7ffa28ef23e59d6322a4b8545f69f50`.

원본 raw: `S6_Chase_Static_60_repeat01.raw`, `S6_Chase_Static_60_repeat02.raw`, `S6_Chase_Static_60_repeat03.raw`.

### S6_Chase_Static_60_EnemySearch

| 지표 | 반복 1 / 2 / 3 |
|---|---|
| 프레임 수 | 1798 / 1797 / 1799 |
| 전체 프레임 p95 | 8.411 / 8.464 / 8.379 |
| 전체 프레임 p99 | 8.549 / 8.608 / 8.498 |
| 전체 프레임 최대 | 8.834 / 8.868 / 8.727 |
| 16.67ms 초과 프레임 | 0 / 0 / 0 |
| 33.33ms 초과 프레임 | 0 / 0 / 0 |
| PlayerLoop gcB/f | 8039.415 / 8039.544 / 8039.635 |
| HasStatus gcB/call | 0.000 / 0.000 / 0.000 |
| 타임라인 gcB/call | 미측정 |
| 마스크 inclusive ms/실행 | 미측정 |
| 마스크 GC bytes/실행 | 미측정 |
| 조합 최대 단일 호출 | 미측정 |
| 조합 이벤트 프레임 최대 | 미측정 |
| 조합 이벤트 프레임 수 | 미측정 |
| 조합 이벤트 16.67ms 초과 | 미측정 |
| 조합 이벤트 33.33ms 초과 | 미측정 |
| 탐색 µs/call | 4.569 / 4.480 / 4.990 |
| 탐색 calls/frame | 19.488 / 19.499 / 19.511 |
| FixedUpdate µs/call | 8.724 / 8.645 / 9.447 |
| FixedUpdate ms/frame | 0.170 / 0.169 / 0.184 |
| effectiveSeed | 1014 / 1015 / 1016 |
| enemiesSpawned | 60 / 60 / 60 |
| enemyDeaths | 0 / 0 / 0 |
| combinations | 0 / 0 / 0 |
| potionDamageEvents | 0 / 0 / 0 |
| enemyDamageEvents | 0 / 0 / 0 |
| playerDamageEvents | 310 / 310 / 312 |
| meleeAttacks | 0 / 0 / 0 |
| playerThrows | 0 / 0 / 0 |
| playerDistance | 0.000 / 0.000 / 0.000 |
| enemyDistance | 62.810 / 62.807 / 62.800 |
| cycles | 1 / 1 / 1 |
| outcome | IntervalCompleted / IntervalCompleted / IntervalCompleted |
| functionalValid | True / True / True |

실행 ID: 20261019_002641_955_f7213e30.

Manifest SHA-256: `66d17bc57be02f4c3d54120ca5df06e3ffd00e7328b46a31f6d745dabe97e74a`.

원본 raw: `S6_Chase_Static_60_EnemySearch_repeat01.raw`, `S6_Chase_Static_60_EnemySearch_repeat02.raw`, `S6_Chase_Static_60_EnemySearch_repeat03.raw`.

### S8_Combat_Minimal

| 지표 | 반복 1 / 2 / 3 |
|---|---|
| 프레임 수 | 1025 / 1014 / 1007 |
| 전체 프레임 p95 | 8.385 / 8.424 / 8.492 |
| 전체 프레임 p99 | 8.504 / 8.571 / 8.656 |
| 전체 프레임 최대 | 11.582 / 8.976 / 9.065 |
| 16.67ms 초과 프레임 | 0 / 0 / 0 |
| 33.33ms 초과 프레임 | 0 / 0 / 0 |
| PlayerLoop gcB/f | 1824.312 / 1676.172 / 1647.919 |
| HasStatus gcB/call | 43.710 / 61.973 / 52.033 |
| 타임라인 gcB/call | 262.091 / 281.196 / 277.706 |
| 마스크 inclusive ms/실행 | 0.006 / 0.007 / 0.004 |
| 마스크 GC bytes/실행 | 0 / 0 / 0 |
| 조합 최대 단일 호출 | 3.583 / 3.036 / 1.892 |
| 조합 이벤트 프레임 최대 | 8.326 / 8.656 / 8.455 |
| 조합 이벤트 프레임 수 | 2 / 2 / 2 |
| 조합 이벤트 16.67ms 초과 | 0 / 0 / 0 |
| 조합 이벤트 33.33ms 초과 | 0 / 0 / 0 |
| 탐색 µs/call | 8.334 / 9.105 / 8.781 |
| 탐색 calls/frame | 0.730 / 0.536 / 0.665 |
| FixedUpdate µs/call | 14.784 / 15.674 / 15.307 |
| FixedUpdate ms/frame | 0.011 / 0.008 / 0.010 |
| effectiveSeed | 1014 / 1015 / 1016 |
| enemiesSpawned | 6 / 6 / 6 |
| enemyDeaths | 6 / 6 / 6 |
| combinations | 2 / 2 / 2 |
| potionDamageEvents | 15 / 17 / 19 |
| enemyDamageEvents | 28 / 29 / 29 |
| playerDamageEvents | 17 / 16 / 13 |
| meleeAttacks | 7 / 7 / 7 |
| playerThrows | 4 / 4 / 4 |
| playerDistance | 24.905 / 24.794 / 24.632 |
| enemyDistance | 14.036 / 11.549 / 16.848 |
| cycles | 1 / 1 / 1 |
| outcome | CombatCompleted / CombatCompleted / CombatCompleted |
| functionalValid | True / True / True |

실행 ID: 20261019_002736_102_259872d9.

Manifest SHA-256: `153884ebfef5eb13349bb3a5aa19412f8cf748aee71fd7583c7828e3e8917ccd`.

원본 raw: `S8_Combat_Minimal_repeat01.raw`, `S8_Combat_Minimal_repeat02.raw`, `S8_Combat_Minimal_repeat03.raw`.

### S8_Combat_Minimal_Improved

| 지표 | 반복 1 / 2 / 3 |
|---|---|
| 프레임 수 | 810 / 1014 / 1019 |
| 전체 프레임 p95 | 8.381 / 8.363 / 8.388 |
| 전체 프레임 p99 | 8.542 / 8.431 / 8.480 |
| 전체 프레임 최대 | 10.315 / 8.818 / 10.020 |
| 16.67ms 초과 프레임 | 0 / 0 / 0 |
| 33.33ms 초과 프레임 | 0 / 0 / 0 |
| PlayerLoop gcB/f | 1226.965 / 783.112 / 812.357 |
| HasStatus gcB/call | 0.000 / 0.000 / 0.000 |
| 타임라인 gcB/call | 146.232 / 114.243 / 104.628 |
| 마스크 inclusive ms/실행 | 0.004 / 0.003 / 0.003 |
| 마스크 GC bytes/실행 | 0 / 0 / 0 |
| 조합 최대 단일 호출 | 3.893 / 2.147 / 1.492 |
| 조합 이벤트 프레임 최대 | 8.329 / 8.336 / 8.372 |
| 조합 이벤트 프레임 수 | 2 / 2 / 2 |
| 조합 이벤트 16.67ms 초과 | 0 / 0 / 0 |
| 조합 이벤트 33.33ms 초과 | 0 / 0 / 0 |
| 탐색 µs/call | 6.453 / 8.345 / 6.714 |
| 탐색 calls/frame | 0.753 / 0.534 / 0.658 |
| FixedUpdate µs/call | 14.068 / 15.820 / 14.069 |
| FixedUpdate ms/frame | 0.011 / 0.008 / 0.009 |
| effectiveSeed | 1014 / 1015 / 1016 |
| enemiesSpawned | 6 / 6 / 6 |
| enemyDeaths | 6 / 6 / 6 |
| combinations | 2 / 2 / 2 |
| potionDamageEvents | 23 / 17 / 19 |
| enemyDamageEvents | 30 / 29 / 29 |
| playerDamageEvents | 14 / 15 / 15 |
| meleeAttacks | 5 / 7 / 7 |
| playerThrows | 4 / 4 / 4 |
| playerDistance | 24.666 / 24.834 / 24.820 |
| enemyDistance | 7.121 / 10.646 / 17.083 |
| cycles | 1 / 1 / 1 |
| outcome | CombatCompleted / CombatCompleted / CombatCompleted |
| functionalValid | True / True / True |

실행 ID: 20261019_002810_051_a791dc80.

Manifest SHA-256: `0c2189993b45b8718c644fc249df80f56f20f20407c85d4aab59fbaa5c176a92`.

원본 raw: `S8_Combat_Minimal_Improved_repeat01.raw`, `S8_Combat_Minimal_Improved_repeat02.raw`, `S8_Combat_Minimal_Improved_repeat03.raw`.

## 재현과 도구 검증

```powershell
python Tools/Profiling/Run-OptimizationBuild.py --exe Logs/Optimization/Build-v2/PotionOptimization.exe --suite core --repetitions 3 --journal Logs/Optimization/player-v2-core-20261019.json
python Tools/Profiling/Analyze-OptimizationBatch.py Logs/Optimization/player-v2-core-20261019.json --output Logs/Optimization/player-v2-analysis-20261019
python Tools/Profiling/Summarize-Optimization.py Logs/Optimization/player-v2-core-20261019.json --output Logs/Optimization/player-v2-results.json
python -m unittest discover -s Tools/Profiling -p test_optimization_tools.py -v
```

새 측정에는 새 journal과 분석 출력 경로를 사용한다. 같은 빌드·실행 조건의 완료 시나리오를 건너뛸 때만 `--exclude-journal`을 지정한다. 빌드 해시·프레임 제한·해상도·반복 수 등이 다르거나 근거가 없으면 재개를 거부한다. 요약에서도 서로 다른 빌드·설정을 합치지 않는다.

재개·혼합 방지 및 조합 이벤트 프레임 집계 회귀 테스트 6개를 통과했다. 이벤트 프레임은 선택된 측정 프레임만 사용하고 같은 프레임의 여러 조합은 한 번만 센다. 오프라인 분석기는 새 manifest의 시드·포션 피해·이동량 등도 보존한다. 이전 manifest에 없는 필드는 역직렬화 시 0이므로 실제 0으로 해석하기 전에 원문을 확인한다.
