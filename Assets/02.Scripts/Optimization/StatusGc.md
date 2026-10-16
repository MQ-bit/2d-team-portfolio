# 상태이상 GC: 반복 조회와 타임라인 버퍼

## 문제와 선택

상태 표시가 활성 상태를 확인할 때 장판 효과 목록을 phase별 iterator로 반복 조회했다. 타임라인은 접촉 대상 집합과 기존 대상의 키 목록을 매 동기화마다 생성했다.

| 고민 | 검토한 방법 | 선택과 이유 |
|---|---|---|
| 상태 표시를 위해 반복 조회할 때 생기는 할당을 어떻게 줄일까? | A. 상태 변경 이벤트를 받아 결과 캐시 / B. 기존 조회 시점을 유지하고 목록 직접 순회 | B. 갱신 알림 누락 위험을 추가하지 않고 iterator 생성을 제거하기 위해서 |
| 타임라인 임시 컬렉션을 어떻게 관리할까? | A. 매번 생성 / B. 인스턴스 버퍼 재사용 | B. 키 스냅샷을 유지하면서 반복 할당을 줄이기 위해서. 재진입 시에는 별도 버퍼를 사용 |

## 두 버전

- 원본: `../Potion/Effects/PotionAreaEffectTracker.cs`, `../Potion/Area/PotionAreaTimeline.cs`
- 개선: `OptimizedPotionAreaEffectTracker.cs`, `OptimizedPotionAreaTimeline.cs`
- 연결: `ComparisonAreaEffectTracker.cs`, `ComparisonAreaTimeline.cs`
- 선택: `StatusQuery`, `Timeline`, `Improved`

상태·피해 원소 조회는 `Effects`를 인덱스로 순회하고 phase와 operation을 검사한다. 효과 적용 주기와 표시 갱신 시점은 그대로 둔다. 타임라인은 콜백 중 종료·재진입을 고려하여 재사용 버퍼, 종료용 버퍼, 중첩 호출용 버퍼를 구분한다.

## 확인된 에디터 결과

Unity 6000.3.22f1, Deep Profile OFF, Profile Editor OFF, Demo UI OFF. 적 30명 고체력 지속 접촉 조건, 각 3회 실행 중앙값이다. 원래 체력으로 사망하는 기존 S7 조건과 구분한다.

| 지표 | 원본 | 상태 조회만 개선 | 타임라인만 개선 | 전체 개선 |
|---|---:|---:|---:|---:|
| PlayerLoop GC KiB/frame | 27.129 | 7.165 | 25.283 | 5.297 |
| 상태 표시 LateUpdate inclusive GC KiB/frame | 19.216 | 0.125 | 19.221 | 0.119 |
| HasStatus 계측 구간 GC bytes/frame | 20,389.456 | 0 | 20,395.184 | 0 |
| 타임라인 동기화 inclusive GC bytes/frame | 2,305.822 | 2,281.787 | 444.970 | 415.555 |

각 실행의 적 생성 수 30, 사망 수 0, 포션 피해 이벤트 330회를 확인했다. 전체 개선은 적 탐색 개선도 포함하므로 전체 CPU 변화는 상태 조회 하나의 성과로 귀속하지 않는다. 타임라인만 개선한 조건에서는 상태 표시의 할당이 유지되고 타임라인 할당만 줄어드는 것을 확인했다.

`HasStatus` 마커는 표시 외 호출도 포함할 수 있으므로 LateUpdate와 더하거나 두 값의 비율로 할당 비중을 계산하지 않는다. 모든 GC 수치는 할당량이며 잔존 메모리나 GC 수집 시간은 아니다.

## 검증과 한계

- phase·operation·상태 조합, null 효과 항목, 여러 소스 중 하나 제거 후 조회 결과를 원본과 비교했다.
- 타임라인의 중복 접촉·주기·만료·이탈과 콜백 중 종료·재진입 이벤트 순서를 비교했다.
- 반복 버퍼의 용량은 유지되므로 상주 메모리와 초기 할당의 절충이 있다.
- `Pulse`, 효과 선택, 잔류 상태 등 다른 경로의 할당은 남아 있다.
- .NET 할당 카운터는 현재 Unity Mono에서 사용 불가였다. 위 숫자는 binary Profiler에서 확인했다.

로컬 근거: `Logs/Optimization/editor-core.json`, `editor-results.json`과 각 분석 보고서의 `hotspots.csv`, `allocation-sites.csv`. Development Build 최종 결과 및 전체 실행별 값은 `Docs/OptimizationResults.md`에 정리한다.
