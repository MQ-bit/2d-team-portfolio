# 원본·개선 버전 비교 구조

## 구현 범위

기존 게임 동작을 기준으로 두 실행 경로를 유지한다. 원본 알고리즘을 개선 코드로 덮어쓰지 않는다. 개선 구현은 `Assets/02.Scripts/Optimization/`에 있으며, 기존 게임 코드에는 구현 선택을 위한 연결과 양쪽에 동일한 계측만 추가한다.

기존 데모 시나리오는 기본적으로 `Original`을 사용한다. 비교 시나리오만 특정 개선 또는 `Improved`를 선택한다. 일반 게임은 `GameDefault`(`SharedMask | FastMask`)를 사용한다. 마스크 출력이 원본과 픽셀 단위로 같다는 검증(게임 시나리오 284개, 무작위 장판 52건)이 끝난 경로만 기본 적용했고, 상태이상 GC와 적 탐색은 데모 한정이라 적용하지 않았다.

| 대상 | 원본 | 개선 구현 | 변경 내용 |
|---|---|---|---|
| 상태 조회 | `Potion/Effects/PotionAreaEffectTracker.cs` | `Optimization/OptimizedPotionAreaEffectTracker.cs` | `HasStatus`와 `HasDamageElement`에서 효과 목록을 인덱스로 조회하여 phase iterator 생성 제거 |
| 장판 타임라인 | `Potion/Area/PotionAreaTimeline.cs` | `Optimization/OptimizedPotionAreaTimeline.cs` | 동기화에 사용하는 대상 집합·키 스냅샷 재사용. 중첩 호출은 독립 버퍼 사용 |
| 장판 마스크 | `Potion/Visual/PotionAreaMaskRenderer.cs` | `Optimization/OptimizedPotionAreaMaskRenderer.cs` | 같은 불변 footprint를 사용하는 렌더러가 참조 수 기반으로 텍스처 공유 |
| 원거리 적 탐색 | `Entity/Enemy/RangedEnemyController.FindPlayerInRange`의 기존 본문 | `Optimization/OptimizedEnemySearch.cs` | 단일 플레이어의 콜라이더 레이어를 데모 Setup에서 수집해 물리 조회 필터에 사용 |

앞의 세 원본 파일은 내용 그대로 보존한다. 적 탐색은 원본 메서드의 본문을 남기고, 개선 조건에서만 별도 구현으로 분기한다. 기존 수신자·장판·VFX 코드는 `Comparison...` 연결 클래스를 통해 원본 또는 개선 구현을 사용한다. 데이터 에셋, 셰이더 및 HLSL은 수정하지 않는다.

## 선택 단위

`PerformanceDemoScenario.Optimizations`의 선택지는 다음과 같다.

- `Original`: 원본 경로
- `StatusQuery`: 상태·피해 원소 조회 개선만 적용
- `Timeline`: 타임라인 버퍼 재사용만 적용
- `SharedMask`: 마스크 공유만 적용
- `EnemySearch`: 적 탐색 필터만 적용
- `Improved`: 위 개선 모두 적용

데모 Setup 전에 선택하며, 상태를 가진 비교 클래스는 생성 시 선택을 고정한다. 실행 도중 버전을 전환하지 않는다. 정상 종료 및 중단 시 전역 선택을 원본으로 복원한다.

이 방식은 비교 실험을 위한 것이다. 기존 컴포넌트 전체를 복제하면 효과 처리나 AI 수정 시 두 버전이 서로 달라질 수 있으므로, 개선 대상 알고리즘을 중심으로 두 버전을 구성했다. 변경한 단일 파일의 모든 코드가 개선되었다는 뜻은 아니다.

## 개선별 선택 이유와 한계

### 상태 조회

기존 조회는 장판 소스마다 `GetEffects(FloorTick)`와 `GetEffects(FloorExit)`를 호출한다. `GetEffects`는 `yield return` 기반이며, 표시 갱신에서 이 조회가 반복된다. 개선 버전은 동일 목록의 phase와 operation을 직접 검사한다. 조회 결과를 장기간 캐시하지 않으므로 상태 변경 알림 누락 문제를 도입하지 않는다.

이 변경은 iterator 생성이 확인되는 경로만 대상으로 한다. 주기 효과 적용의 `Pulse`, 효과 선택 및 종료 처리에 남은 할당을 모두 제거한 것은 아니다. 따라서 전체 PlayerLoop GC 0을 목표 달성 수치로 미리 제시하지 않는다.

### 타임라인

대상 집합과 기존 키 목록은 매 동기화마다 내용을 비운 뒤 재사용한다. 키 목록은 기존 코드와 마찬가지로 스냅샷으로 유지하여 콜백에서 상태가 변경되어도 Dictionary를 직접 순회하며 수정하지 않는다. 콜백 중 재진입하면 독립된 임시 버퍼를 사용하고, 종료 콜백용 목록도 분리한다. 버퍼는 `finally`에서 참조를 비운다.

대가로 인스턴스가 최대 사용량의 버퍼 용량을 유지한다. 인터페이스 기반 입력 순회와 중첩 호출 등 모든 할당까지 없어진 것은 아니다.

### 장판 마스크

표현을 낮추는 실험과 비용 제거를 구분하기 위해 해상도 256, 픽셀 판정식, 텍스처 형식과 샘플링 설정을 유지했다. 같은 footprint 객체를 사용하는 경우에만 생성 결과를 공유한다. 마지막 사용자가 해제하면 캐시 항목과 Texture2D를 해제한다. 별도 형상끼리 근삿값으로 묶거나 영구 캐싱하지 않는다.

이 개선은 중복 마스크 생성에만 효과가 있다. 최초 마스크의 픽셀 계산, 형상 생성, native 클리핑 및 VFX 생성 비용은 남는다. 벽에 잘리지 않은 장판은 원래 마스크를 만들지 않으므로 해당 조건에서 이득이 없을 수 있다. 여러 장판을 하나의 드로우콜로 배칭한 구현이 아니다.

### 적 탐색

레이어 이름만 가정하지 않고 실제 데모 플레이어의 자식 콜라이더 레이어를 포함한다. 트리거 포함 여부와 검색 주기는 원본과 유지한다. 대상 캐시나 탐색 주기 감소는 감지·재획득 동작을 바꿀 수 있어 이번 기본 개선에 포함하지 않았다.

이 구현은 플레이어가 하나이며 콜라이더 계층과 레이어가 실행 중 유지되는 격리 데모용이다. 컨텍스트가 없으면 기존 전체 필터로 조회한다. 다중 플레이어, 런타임 콜라이더 추가 또는 레이어 변경이 있는 실제 게임으로 확대할 때는 갱신 계약을 먼저 추가해야 한다.

## 재현 방법

Unity에서 기존 데모 씬을 열고 `Tools > Performance > Add Optimization Comparisons`를 실행한다. 기존 시나리오를 보존하면서 비교용 SO와 러너 참조를 추가한다. 기존 같은 이름의 비교 에셋은 덮어쓰지 않는다.

```powershell
python Tools/Profiling/Run-Optimization.py prepare
python Tools/Profiling/Run-Optimization.py validate
python Tools/Profiling/Run-Optimization.py capture --suite core --repetitions 3
```

실행 로그에 출력된 journal 경로로 오프라인 분석을 실행한다. 측정과 분석을 동시에 실행해 CPU 자원을 경쟁시키지 않는다.

```powershell
python Tools/Profiling/Run-Optimization.py analyze --journal Logs/Optimization/<journal>.json
python Tools/Profiling/Summarize-Optimization.py Logs/Optimization/<journal>.json
```

`gc`, `combine`, `enemy`, `regression`, `all` suite도 제공한다. 이전 journal에서 완료한 시나리오를 제외하려면 `--exclude-journal`을 사용한다. 실패 시도는 삭제하지 않으며, 실패 실행의 일부 정상 프레임도 성공 비교 그룹에 합치지 않는다.

Development Build 실행은 `Run-OptimizationBuild.py`를 사용한다. 재개할 때 실행 파일·관리 어셈블리 해시, 반복 횟수, 프레임 제한, 해상도와 VSync가 일치해야 완료 시나리오를 건너뛴다. 이전 journal에 빌드 해시가 없으면 동일 빌드라고 추정하지 않고 새로 측정한다. `Summarize-Optimization.py`도 서로 다른 빌드·실행 설정을 함께 요약하지 않는다.

Development Build에는 아래 인자로 저장된 시나리오를 실행하는 진입점을 제공한다. 실행 완료 시 빌드 폴더에 `optimization-last-run.json`을 기록하고 종료한다. 원본/개선은 동일 빌드 안에 포함된다.

```text
PotionOptimization.exe --optimization-scenarios S6_Chase_Moving_60
PotionOptimization.exe --optimization-scenarios S6_Chase_Moving_60_EnemySearch
```

## 검증 근거

`OptimizationValidation.Run()`은 실제 원본과 개선 구현을 비교한다.

- phase·operation·상태 조합 및 소스 제거 후 조회 결과
- 타임라인의 진입·중복 접촉·주기·이탈·만료 이벤트 순서
- 콜백 중 종료 및 재진입 시 이벤트 순서
- 원본/공유 마스크의 GPU 읽기 결과 픽셀 일치, 중복 연결, 참조 해제
- 혼합 레이어·트리거·범위 안팎·대상 비활성화 시 탐색 결과

현재 Unity Mono 환경에서는 `GC.GetAllocatedBytesForCurrentThread()`가 명시적인 배열 할당에도 0을 반환했다. 해당 카운터는 사용 불가로 기록하며, 테스트 결과의 0을 무할당 근거로 사용하지 않는다. GC 성능은 binary Profiler 캡처에서 확인한다.

로컬 원본 메타데이터, 실패 시도와 분석 결과는 `Logs/Optimization/` 및 `Logs/ProfilerReports/`에 보관한다. Logs는 Git 제외 대상이므로 공유용 성능 수치와 한계는 별도의 최종 결과 문서에 남긴다.

2026-10-19 재개 측정의 Development Build 결과, 반복별 작업량과 적용 한계는 [OptimizationResults.md](OptimizationResults.md)에 기록한다. 조합 순간 부하는 단일 함수 최대뿐 아니라 해당 조합 이벤트가 발생한 측정 프레임의 최대 시간과 예산 초과 횟수도 확인한다.
