# 성능 최적화 영향 요소 분석 및 자동 측정 데모 계획

- 작성일: 2026-10-14
- 분석 환경: Unity 6000.3.22f1 / Universal Render Pipeline 17.3.0 / 2D 프로젝트
- 현재 단계: 소스 코드 기반 후보 분석. 실행 중 Profiler 측정 및 최적화 구현은 아직 수행하지 않음.
- 목적: 성능에 큰 영향을 줄 수 있는 요소를 식별하고, 동일 조건에서 반복 측정할 자동 데모 씬의 범위를 정한다.

## 1. 판단 요약

우선 조사할 축은 **장판 렌더링, 장판 생성·클리핑·조합, 리퍼 스킬 연출, 적 밀집 이동·물리**다. 포션 장판과 리퍼를 우선 후보로 잡는 것은 타당하지만, 실제 우선순위는 CPU/GPU 프레임 시간과 순간 스파이크를 측정한 뒤 결정해야 한다.

특히 장판은 다음 두 문제를 구분해야 한다.

- 포션이 착지하거나 조합되는 순간 끊김: 생성, 벽 클리핑, 메시·콜라이더·마스크 생성, 물리 동기화, 메모리 할당을 조사한다.
- 장판이 많이 남아 있는 동안 지속적으로 느림: 투명 렌더링, 셰이더, 지속 효과, 접촉 대상 수를 조사한다.

아래 우선순위는 **측정 순서**이며, 비용이 확정된 순위가 아니다. 코드가 복잡하거나 특정 API가 있다는 사실만으로 병목을 단정하지 않는다.

## 2. 후보별 영향 요소

| 조사 순서 | 후보 | 주요 비용 영역 | 부하를 키우는 변수 | 예상 증상 |
| --- | --- | --- | --- | --- |
| 1 | 장판 셰이더·투명 VFX | GPU, 렌더 제출 CPU | 화면 점유 면적, 겹침, 장판 종류, 렌더러·패스 수 | 장판이 보이는 동안 프레임 시간 증가 |
| 2 | 장판 생성·클리핑·마스크·조합 | CPU, 할당, GPU 업로드 | 동시 생성 수, 벽 폴리곤 복잡도, 마스크 크기 | 착지·조합 프레임의 스파이크 |
| 3 | 리퍼 스킬·경고 표시 | CPU, 렌더링, 할당 | 동시 리퍼 수, 스킬 반복 빈도, 메시 갱신량 | 스킬 시작 또는 충전 구간의 비용 증가 |
| 4 | 적 이동·길찾기·물리 | CPU, Physics 2D | 적 수, 밀집도, 맵 크기, 장애물, 갱신 빈도 | 적이 몰릴 때 지속 부하 또는 주기적 스파이크 |
| 5 | 장판 효과·상태이상·피해 피드백 | CPU, 물리, VFX | 접촉 대상 수, 효과 중첩, 틱 빈도, 동시 타격 수 | 장판 안에 적이 많을 때 비용 증가 |
| 6 | 전투 오브젝트 수명·자원 유지 | CPU, GC, 메모리 | 생성·해제 빈도, 반복 전투 횟수 | 간헐적 끊김 또는 자원 수의 지속 증가 |

### 2.1 장판 셰이더와 투명 렌더링

**코드에서 확인한 사실**

- [PotionAreaVisual.shader](../Assets/09.Resources/Resources/PotionAreaVisual.shader)는 알파 블렌딩과 여러 노이즈 계산을 사용한다. 스타일별 경로가 있으므로 파일 전체의 연산이 모든 픽셀에서 실행된다고 해석하면 안 된다.
- [PotionHealingSpring.shader](../Assets/09.Resources/Resources/PotionHealingSpring.shader)와 [PotionPurifyingLife.shader](../Assets/09.Resources/Resources/PotionPurifyingLife.shader)에는 반복 계산이 있다.
- [PotionFireVortexRenderer.cs](../Assets/02.Scripts/Potion/Visual/PotionFireVortexRenderer.cs)는 별도 메시·렌더러를 구성한다. 공유 머티리얼을 사용하는 코드도 있으므로 장판마다 모든 자원이 독립 생성된다고 가정하지 않는다.

**영향 가설**

장판 개수뿐 아니라 화면에서 차지하는 픽셀 수와 투명 효과의 겹침이 중요하다. 같은 개수여도 크게 확대되거나 겹쳐 보이는 상황에서 GPU 비용이 커질 수 있다. 렌더러와 드로우콜 증가로 CPU 렌더 제출 비용도 증가할 수 있다.

**검증 방법**

- 효과 종류별로 단일 장판부터 시작하고 개수·면적·겹침을 각각 독립적으로 변경한다.
- 시각 효과만 비활성화한 비교군에서도 장판 판정과 효과 적용은 유지한다.
- GPU 시간, Rendering 통계, Frame Debugger의 실제 패스·드로우콜을 확인한다.
- Frame Debugger는 그리기 구조를 추적하는 도구이며, 셰이더별 GPU 시간의 직접 측정을 대신하지 않는다.

**측정 후 검토할 개선**: 노이즈 계산 단순화, 텍스처 활용, 보이지 않는 VFX 갱신 제한, 렌더러·패스 축소. 각 개선은 장판 경계와 시각적 가독성이 유지되는지 확인한다.

### 2.2 장판 생성, 벽 클리핑, 마스크, 조합

**코드에서 확인한 사실**

- [PotionAreaClipper.cs](../Assets/02.Scripts/Potion/Cliping/PotionAreaClipper.cs)의 `Subtract`는 임시 GameObject, Rigidbody2D, CompositeCollider2D, PolygonCollider2D를 만들고 `GenerateGeometry()`와 `CreateMesh()`를 호출한 뒤 정리한다. 벽이 없는 경로는 클리핑을 건너뛴다.
- [PotionAreaMaskRenderer.cs](../Assets/02.Scripts/Potion/Visual/PotionAreaMaskRenderer.cs)의 `SetFootprint`는 잘린 장판에서 최대 축 256 해상도의 마스크를 생성한다. 픽셀별 폴리곤 내부 판정을 하고 `SetPixels32`, `Apply`로 텍스처를 업로드한다. 호출 시의 비용이며 매 프레임 생성이라고 단정하지 않는다.
- [PotionCombinedAreaShape.cs](../Assets/02.Scripts/Potion/Combination/PotionCombinedAreaShape.cs)는 조합 시 벽 폴리곤 조회, 클리핑, 콜라이더 적용, 조합 시각 오브젝트 생성과 footprint 전달을 수행한다.
- [PotionAreaEffect.cs](../Assets/02.Scripts/Potion/Area/PotionAreaEffect.cs)의 `ApplySingleImpact`는 `Physics2D.SyncTransforms`, 겹침 조회, List·HashSet 생성을 수행한다.

**영향 가설**

여러 포션이 같은 프레임에 착지하거나 복잡한 벽 근처에서 조합되면 단발 작업이 겹쳐 CPU 스파이크와 할당이 증가할 수 있다. 장판 유지 중 GPU 비용과 별도로 조사해야 한다.

**검증 방법**

- 벽 없는 곳 / 단순 벽 / 복잡한 모서리에서 같은 포션을 같은 빈도로 투척한다.
- 단일 생성과 동시 다수 생성, 일반 착지와 조합 착지를 분리한다.
- 생성 프레임의 CPU 시간·GC Alloc·작업 호출 횟수를 확인한다.
- 마스크 생성과 클리핑에 별도 CPU 마커를 두어 비용을 구분한다.

**측정 후 검토할 개선**: 임시 계산 자원 재사용, 반복 형상의 캐시, 마스크 해상도 조절, 불필요한 재생성 방지. 벽을 넘는 판정, 연결 영역, 조합 전파 동작의 정확성을 보존해야 한다.

### 2.3 리퍼 스킬과 경고 표시

**코드에서 확인한 사실**

- [ReaperController.cs](../Assets/02.Scripts/Entity/Enemy/ReaperController.cs)는 스킬 과정에서 포션 낙하, 탈취 효과, 돌진 경고 오브젝트를 생성·해제한다.
- [ReaperAttackIndicator.cs](../Assets/02.Scripts/Entity/Enemy/ReaperAttackIndicator.cs)는 메시·LineRenderer·머티리얼을 만들고, 형태 구성에서 List와 `ToArray`를 사용한다.
- `SetProgress`의 모드별 경로에서 메시 정점·색상 갱신과 `RecalculateBounds()`를 수행한다. 재사용 배열이 있으므로 모든 갱신이 새 배열 할당을 일으킨다고 가정하지 않는다.
- [ReaperPotionSteal.cs](../Assets/02.Scripts/Entity/Enemy/ReaperPotionSteal.cs)는 경고, 트레일, 획득 효과를 생성하고 이동 연출을 갱신한다.

**영향 가설**

스킬 시작 시 생성·형상 구성 비용과 충전 중 반복 메시 갱신 비용이 서로 다르다. 리퍼 한 마리에서는 작아도 동시 발동 시 누적될 수 있다.

**검증 방법**

- 돌진 / 탈취 / 포션 낙하를 각각 실행한다.
- 스킬 시작, 충전, 발동, 종료 구간을 구분하고 동시 리퍼 수를 늘린다.
- 연출만 비활성화한 비교군에서도 같은 타이밍의 판정과 스킬 상태 전이는 유지한다.

**측정 후 검토할 개선**: 경고 형상 캐시, 오브젝트·메시 재사용, 고정 Bounds 적용 가능성, 갱신 빈도 조절. 풀링 시 모드·색상·진행도·참조 초기화와 자원 수명을 함께 설계해야 한다.

### 2.4 다수 적의 이동, 길찾기, 물리

**코드에서 확인한 사실**

- [EnemyMovementModule.cs](../Assets/02.Scripts/Entity/Enemy/EnemyMovementModule.cs)는 추적 시 `GetSeparation`에서 주변 적을 `Physics2D.OverlapCircle`로 조회한다. 결과 컨테이너를 재사용하며, 경로 재탐색에는 간격 제한이 있다.
- [EnemyPathfinder.cs](../Assets/02.Scripts/Entity/Enemy/EnemyPathfinder.cs)는 장애물 격자와 크기별 거리장을 사용한다. 목표 셀과 시간 조건에 따라 재계산을 제한하고, 기본 거리장 갱신 간격은 0.1초다.
- 격자 재구축은 셀·크기 레이어별 물리 조회를 수행한다. 이를 일반 추적 구간과 분리해 측정해야 한다.

**영향 가설**

적 수와 밀집도가 증가하면 주변 조회·결과 순회·접촉 처리 비용이 커질 수 있다. 거리장은 공유·갱신 제한이 있으므로 적마다 전체 길찾기를 수행한다고 해석하면 안 된다. 맵 전환의 격자 재구축은 별도의 로딩 스파이크 후보다.

**검증 방법**: 같은 수의 적을 분산 배치한 경우와 밀집 배치한 경우를 비교한다. 장애물 없는 추적과 우회 추적, 최초 격자 생성과 정상 유지 구간도 구분한다. Scripts와 Physics 2D 시간을 함께 확인한다.

**측정 후 검토할 개선**: 주변 조회 주기·분산, 공간 분할, 길찾기 갱신 분산. 이동 반응성과 적 겹침 방지 품질을 검증한다.

### 2.5 장판 효과, 상태이상, 피해 피드백

**코드에서 확인한 사실**

- [PotionAreaContacts.cs](../Assets/02.Scripts/Potion/Area/PotionAreaContacts.cs)는 접촉 대상과 콜라이더를 캐시하고 다중 콜라이더의 중복 진입을 처리한다.
- [PotionAreaEffect.cs](../Assets/02.Scripts/Potion/Area/PotionAreaEffect.cs)는 장판 타임라인, 조합 전파, 접촉 대상과 효과 적용을 관리한다.
- [PotionEffectReceiver.cs](../Assets/02.Scripts/Potion/Effects/PotionEffectReceiver.cs)는 지속 효과와 상태이상을 처리하고 상태 시각 컴포넌트를 연결한다.
- [DamageNumberPresenter.cs](../Assets/02.Scripts/UI/IngameUI/DamageNumberPresenter.cs)에는 풀 설정과 갱신 경로가 있다. 데미지 숫자에 무조건 새 풀을 추가하는 접근보다 현재 동작과 포화 시 비용부터 확인한다.

**영향 가설 및 검증**

장판 개수가 같아도 접촉 적 수, 틱 빈도, 상태 중첩에 따라 비용이 달라질 수 있다. 대상 없는 장판과 대상이 들어간 장판을 비교하고, 피해 숫자·피격 VFX만 끈 비교군으로 피드백 비용을 분리한다. 실제 피해량·틱 주기·중복 적용 규칙은 동일하게 유지한다.

### 2.6 생성·해제와 메모리 유지

`Instantiate/Destroy`의 존재 자체가 풀링 필요성을 입증하지 않는다. 생성 빈도, 생성 프레임의 CPU 비용, 관리 힙 할당, 유지 자원 수를 먼저 확인한다. Texture·Mesh·Material 같은 Unity 자원은 GC Alloc만으로 상태를 판단할 수 없다.

같은 전투 사이클을 반복한 뒤 정리 완료·동일 대기 조건의 메모리 스냅샷을 비교한다. 최초 캐시 형성과 지속 증가를 구분하고, 남은 참조와 자원 수명을 확인한 뒤 누수 여부를 판단한다.

## 3. 측정 지표와 도구

| 도구·지표 | 확인할 내용 | 해석 주의점 |
| --- | --- | --- |
| Unity Profiler CPU Usage | Main Thread·Render Thread, 작업 시간·호출 수, GC Alloc | 전체 샘플 시간과 Self 시간을 구분하고 중첩 시간을 중복 합산하지 않는다 |
| GPU Usage | GPU 프레임 시간, 렌더링 구간 | 플랫폼·Graphics API 지원 여부를 확인한다. 미지원은 비용이 0이라는 뜻이 아니다 |
| Rendering / Frame Debugger | Batches, SetPass, 렌더러·패스·그리기 순서 | 드로우콜 수만으로 픽셀 처리 비용을 판단하지 않는다 |
| Physics 2D | 물리 처리, 접촉·쿼리 관련 비용 | 스크립트에서 수행한 쿼리의 CPU 호출 경로도 확인한다 |
| Profile Analyzer | 여러 프레임과 변경 전후 CPU 캡처 비교 | 같은 조건과 구간끼리 비교한다 |
| Memory Profiler | 정리 후 오브젝트·텍스처·메시·머티리얼과 참조 | 캐시·워밍업 증가와 반복 누수를 구분한다 |

프레임 시간의 중앙값, p95·p99, 최대값과 스파이크 발생 횟수를 기록한다. 백분위는 해당 구간의 원본 프레임 데이터에서 산출하고 짧은 구간의 표본 수를 함께 남긴다. 생성 이벤트는 이벤트 프레임의 비용도 별도로 비교한다. 평균 FPS만으로 순간 끊김을 판단하지 않는다.

목표가 60FPS라면 한 프레임 예산은 약 16.7ms, 120FPS라면 약 8.3ms다. 목표 플랫폼·해상도·FPS는 구현 전에 확정한다. CPU와 GPU는 병렬로 동작하므로 두 시간을 단순 합산해 프레임 시간으로 사용하지 않는다. VSync·프레임 제한에 따른 대기 샘플을 실제 작업 병목으로 오인하지 않는다.

## 4. 자동 측정 데모 씬 구성안

전용 씬 하나와 시나리오 실행 컨트롤러를 만든다. 실제 프리팹·포션 데이터·전투 로직을 사용하며, 자동 실행은 입력과 배치·타이밍을 제어한다. 첫 버전에는 복잡한 자동 최적화나 합격 판정 시스템을 포함하지 않는다.

### 시나리오

아래 수량과 시간은 초기 실험값이며 실제 게임 최대 동시 수와 목표 기기에 맞춰 조정한다.

| ID | 시나리오 | 변경 변수 | 비교군 |
| --- | --- | --- | --- |
| S0 | 기준 맵·카메라 | 전투 오브젝트 없음 | 공통 기준 |
| S1 | 장판 유지 | 종류별 1 / 5 / 10 / 20개 | 시각 효과만 끄기 |
| S2 | 장판 렌더 부하 | 고정 개수에서 면적·겹침을 각각 변경 | 분산 / 밀집 배치 |
| S3 | 포션 착지·벽 클리핑 | 벽 없음 / 단순 벽 / 복잡한 모서리, 동시 착지 수 | 같은 포션·투척 빈도 |
| S4 | 장판 조합·전파 | 조합 종류, 생성 빈도, 접촉 대상 수 | 일반 장판과 별도 기록 |
| S5 | 리퍼 스킬 | 스킬별 1 / 3 / 5마리, 동시·분산 발동 | 경고·VFX만 끄기 |
| S6 | 적 추적·밀집 | 10 / 30 / 60마리, 장애물·밀집도 | 동일 수의 분산 배치 |
| S7 | 지속 효과·피해 피드백 | 고정 장판 수에서 대상 수·효과 종류 변경 | 피해 숫자·피격 VFX만 끄기 |
| S8 | 복합 전투 | 대표 전투 및 예상 최대 동시 부하 | 분리 측정 결과와 대조 |
| S9 | 반복 수명 | 동일 생성·전투·정리 사이클 반복 | 정리 후 자원·메모리 비교 |

### 재현 조건

- 카메라, 해상도, 품질, 렌더 파이프라인, 난수 시드, 투척 위치, 실행 순서와 타이밍을 고정한다.
- 실제 적용 중인 프리팹·데이터 값을 기록한다. 설정 수량과 실제 활성 수량이 일치하는지 확인한다.
- 초기안은 워밍업 5초, 유지 구간 20초, 동일 조건 3회 반복이다. 최초 생성·첫 스킬 비용은 별도 캡처한다.
- 장판 만료·조합·적 사망으로 부하가 달라지면 이를 기록한다. 유지 구간과 재생성 구간을 분리한다.
- 자동 스폰·일괄 생성·정리 자체의 비용을 구분하고, 정상 유지 구간 통계에 섞지 않는다.
- 시나리오 사이에 장판, 적, 이펙트와 이벤트 구독을 정리하고 정리 완료를 검증한다.
- 시나리오 표시 UI는 작게 유지한다. 매 프레임 로그나 파일 저장이 측정 부하를 만들지 않도록 한다.
- 대표 시나리오는 실제 인게임 전투에서 다시 검증한다. 데모 결과만으로 전체 게임 성능을 확정하지 않는다.

### 계측 설계

추가 예정인 CPU `ProfilerMarker` 이름은 다음과 같다. 기존 코드에 이미 추가된 마커를 뜻하지 않는다.

- `PerfDemo.Setup`, `PerfDemo.Cleanup`: 측정 도구 자체의 작업.
- `Potion.Clip`, `Potion.BuildMask`, `Potion.Combine`, `Potion.Impact`: 장판 생성·판정 작업.
- `Reaper.BuildIndicator`, `Reaper.UpdateIndicator`: 리퍼 경고 생성·갱신 작업.
- `Enemy.Separation`, `Enemy.DistanceField`, `Enemy.RebuildGrid`: 적 이동·길찾기 작업.

마커는 동기 작업 범위를 감싼다. 코루틴의 대기 시간을 포함해 프레임을 넘어 열린 상태로 유지하지 않는다. CPU 마커는 GPU 셰이더 실행 시간을 직접 측정하지 않는다. 시나리오 ID·시작/종료 프레임은 별도 기록해 캡처 구간을 식별한다.

## 5. 구현 순서 및 완료 기준

1. 목표 플랫폼·FPS·해상도와 실제 게임의 대표·최대 동시 부하를 정한다.
2. 기존 `EnemyAIScene`, `VFXTestScene`, `PotionEffectTest`의 구성과 실제 프리팹 의존성을 확인한다. 기존 `EnemyAITestSceneBuilder`의 씬 생성 패턴을 참고한다.
3. 전용 씬과 컨트롤러를 작성한다. 첫 구현은 S0, S1, S3, S5로 시작한다.
4. 최소 CPU 마커와 시나리오 구간 기록을 추가하고, 실제 장판·스킬·판정이 실행되는지 확인한다.
5. 에디터에서 동작을 점검한 뒤 목표 기기의 Development Build를 Profiler에 연결해 기준 캡처를 저장한다. 일반 측정은 Deep Profile을 끈 상태로 하고, 필요할 때 좁은 구간의 진단에 사용한다.
6. S4, S6, S7, S8, S9와 전후 비교를 확장한다. GPU 데이터가 지원되지 않으면 목표 플랫폼에 맞는 GPU 도구를 선택한다.
7. 가장 큰 비용 또는 프레임 예산 초과를 만드는 후보부터 하나씩 개선하고 동일 조건으로 재측정한다.

완료 기준은 자동 실행의 재현성, 유효한 구간 캡처, 최초·유지·정리 비용의 구분, 비교군의 판정 조건 유지, 시나리오 사이 자원 정리다. 성능 개선의 수치 목표는 기준 측정 후 결정한다.

현재 [FireOptimizationValidation.cs](../Assets/02.Scripts/Potion/Tests~/FireOptimizationValidation.cs)는 렌더 결과와 일부 실행 시간을 확인하는 코드지만, `Tests~` 경로 및 Editor 전용 코드이므로 일반 플레이용 성능 데모를 대신하지 않는다. Reflection·Stopwatch를 포함한 시간도 목표 기기의 GPU 프레임 시간으로 해석하지 않는다.

## 6. 결과 기록 양식

| 항목 | 기록 내용 |
| --- | --- |
| 실행 환경 | 코드 리비전, 기기·CPU·GPU, Graphics API, Unity 버전, 빌드 설정 |
| 화면·시간 설정 | 해상도, 품질, VSync, 목표 FPS, Fixed Timestep |
| 시나리오 | ID, seed, 포션·조합·스킬 종류, 활성 개수, 배치, 효과 토글 |
| 구간 | 최초 / 워밍업 / 생성 / 유지 / 종료 / 정리, 시작·종료 프레임, 표본 수 |
| 시간 | 프레임 시간 분포, CPU 작업·GPU 시간, 이벤트 스파이크 |
| 기타 비용 | GC Alloc, 렌더 통계, 물리 비용, 정리 후 자원 수 |
| 비교 | 변경 전후 동일 조건의 차이, 반복별 편차, 시각·판정 검증 결과 |

## 7. 참고 문서

- [Unity 6.3 Profiler](https://docs.unity3d.com/6000.3/Documentation/Manual/Profiler.html)
- [목표 기기에서 프로파일링](https://docs.unity3d.com/6000.3/Documentation/Manual/profiling-target-device.html)
- [Frame Debugger](https://docs.unity3d.com/6000.3/Documentation/Manual/FrameDebugger.html)
- [Profile Analyzer](https://docs.unity3d.com/Packages/com.unity.performance.profile-analyzer@1.2/manual/index.html)

이 문서는 측정과 데모 구현의 출발점이다. 실측 결과가 나오면 후보 순서, 수량, 시간과 개선 방향을 갱신한다.
