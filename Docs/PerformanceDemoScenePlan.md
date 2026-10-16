# 자동 성능 측정 데모 씬 설계 및 구현 계획

- 작성일: 2026-10-14
- 기반 문서: [성능 최적화 영향 요소 분석](PerformanceOptimizationAnalysis.md)
- 상태: `PerformanceDemoScene` 및 S0·S1·S3·S5의 첫 실행 버전 구현. 구현 상세와 남은 검증은 아래 12절을 따른다.
- 목적: 포션 장판과 리퍼를 우선 측정하고, 이후 적 이동·상태 효과·복합 전투로 확장할 수 있는 재현 가능한 테스트 환경을 만든다.
- 후속 확장 계획: [실제 전투 루프 시나리오 계획](PerformanceDemoGameLoopScenarioPlan.md). 다중 장판 조합·다수 적 추적·복합 전투·스테이지 진행·반복 수명의 구현 및 검증 순서를 다룬다.
- 게임 루프 시나리오 구현: [추가된 24개 시나리오 사용법과 범위](PerformanceDemoGameLoopImplementation.md). 고정 공간의 전투를 확장하며 방/스테이지 진행은 후속 범위다.

## 1. 결정할 형태

**전용 씬 하나 + 자동 실행 컨트롤러 + 시나리오 설정 데이터 + 에디터 씬 생성 도구**로 구성한다.

씬은 `PerformanceDemoScene`으로 명명한다. 화면에는 실제 게임의 바닥·벽·장판·리퍼와 고정 카메라가 있고, 작은 패널에서 시나리오를 선택해 실행한다. 전체 실행을 누르면 정해진 순서로 측정과 정리를 반복한다. Development Build에서도 같은 자동 실행이 가능해야 한다.

최초 구현은 최적화 전후를 비교할 기준 환경을 만드는 데 집중한다. 측정용으로 원본 셰이더나 전투 로직을 먼저 최적화하지 않는다. 데모의 자동 배치·실행 비용과 게임 코드의 비용을 구분한다.

## 2. 화면과 공간 구성

### 화면 구성

- 중앙: 측정 대상이 보이는 고정된 2D 전투 공간.
- 모서리 패널: 시나리오 ID, 반복 회차, 현재 단계, 설정 수량과 실제 활성 수량.
- 실행 전 조작: 시나리오 선택, 개별 실행, 전체 실행, 중단·정리.
- 비교 설정: 원본 연출 / 연출 비활성화. 동일 게임 조건의 비교군을 실행한다.
- 측정 중에는 UI 값을 낮은 빈도로 갱신하고, 필요하면 패널 렌더링을 숨긴다. UI 사용 여부도 실행 설정에 기록한다.

첫 버전의 결과 화면은 완료·실패 상태와 기록 파일 위치만 표시한다. 게임 화면에 복잡한 그래프나 매 프레임 로그를 넣지 않는다.

### 테스트 공간

| 공간 | 구성 | 목적 |
| --- | --- | --- |
| OpenFloor | 벽이 없는 바닥과 고정 배치 지점 | 장판 유지·렌더링 기준 |
| SimpleWall | 짧은 벽과 단순 모서리 | 벽 근처 포션 착지 |
| ComplexCorner | 오목한 모서리와 실제 레이어의 벽 콜라이더 | 클리핑·조합 비용 |
| ReaperArena | 리퍼·플레이어 위치 지점과 충분한 돌진 공간 | 리퍼 스킬 재현 |
| EnemyArena, 후속 | 분산·밀집 배치와 우회 경로 | 이동·물리 부하 |

한 번에 한 공간만 활성화한다. 비활성 공간의 콜라이더·AI·VFX가 측정에 참여하지 않게 한다. 공간은 동일 카메라 기준 위치에 교체 배치하고, 시나리오 실행 중 카메라를 이동하지 않는다.

기존 맵 전체를 복제하기보다 실제 바닥·벽의 렌더 설정과 물리 레이어를 이용해 작은 공간을 만든다. 맵 프리팹 경로와 사용 타일은 구현 단계에서 AssetDatabase로 확인한다. 기존 씬 생성 도구의 하드코딩 경로가 현재 유효하다고 가정하지 않는다.

## 3. 예정 씬 계층

```text
PerformanceDemoScene
├─ DemoSystem
│  ├─ PerformanceDemoRunner
│  ├─ PerformanceDemoRecorder
│  └─ ScenarioSettings
├─ EnvironmentRoot
│  └─ CurrentArena
├─ ServicesRoot
│  └─ 시나리오에 필요한 공통 서비스
├─ CameraRoot
│  └─ 고정 Camera + 실제 URP 렌더 설정
├─ RuntimeRoot
│  ├─ Player
│  ├─ Enemies
│  ├─ Projectiles
│  └─ AreasAndEffects
└─ DemoUI
```

실제 게임 코드는 자식 이펙트를 데모 루트 밖에 생성할 수 있다. 따라서 `RuntimeRoot` 삭제만으로 정리를 완료했다고 판단하지 않는다. 스폰 소유권, 별도 생성 오브젝트, 풀 반환과 정적 참조를 확인한다.

## 4. 파일과 책임

다음은 구현 예정 경로이며, 구현 전에는 파일 링크로 연결하지 않는다.

| 예정 경로 | 책임 |
| --- | --- |
| `Assets/01.Scenes/PerformanceDemoScene.unity` | 직렬화된 실행 환경 |
| `Assets/02.Scripts/PerformanceDemo/PerformanceDemoRunner.cs` | 실행 상태, 단계 전환, 중단과 정리 |
| `Assets/02.Scripts/PerformanceDemo/PerformanceDemoScenario.cs` | 시나리오·수량·시간·seed·비교 설정 데이터 |
| `Assets/02.Scripts/PerformanceDemo/PerformanceDemoPotionDriver.cs` | 실제 초기화·투척 경로를 이용한 장판 실행 |
| `Assets/02.Scripts/PerformanceDemo/PerformanceDemoReaperDriver.cs` | AI 스킬 조건 구성과 실행 횟수 검증 |
| `Assets/02.Scripts/PerformanceDemo/PerformanceDemoRecorder.cs` | 환경·구간·이벤트 기록, 완료 후 저장 |
| `Assets/02.Scripts/PerformanceDemo/PerformanceDemoView.cs` | 실행 조작과 간단한 상태 표시 |
| `Assets/02.Scripts/Editor/PerformanceDemoSceneBuilder.cs` | 프리팹·데이터 검증, 씬과 설정 생성 |
| `Assets/PerformanceDemo/` | 시나리오 설정 및 필요한 전용 데이터 사본 |

첫 구현에서는 작은 클래스를 불필요하게 더 분할하지 않는다. 에디터 생성 도구와 런타임 코드의 의존성은 분리한다. 빌드에서 AssetDatabase·UnityEditor·Reflection으로 비공개 필드를 조작하지 않는다.

생성 메뉴는 `Tools/Performance/Create Demo Scene`으로 제안한다. 저장되지 않은 현재 씬을 보호하고, 기존 데모 씬의 사용자 수정 사항을 무조건 덮어쓰지 않도록 한다. 일반 인게임 빌드 목록은 임의 변경하지 않고 데모 빌드를 별도로 구성한다.

## 5. 실제 게임 코드 연결

### 포션과 장판

현재 확인된 에셋은 [Potion.prefab](../Assets/03.Prefabs/Potion.prefab), [PotionGroundArea.prefab](../Assets/03.Prefabs/PotionGroundArea.prefab), [Player.prefab](../Assets/03.Prefabs/Player/Player.prefab)이다.

- 유지 부하 측정: [PotionAreaController](../Assets/02.Scripts/Potion/Area/PotionAreaController.cs)의 공개 `Initialize`, 소유자·카탈로그 설정 경로를 사용한다. 실제 활성화 순서와 추가 호출 필요성은 구현 시 전체 초기화 경로를 확인한다.
- 착지 측정: [PotionProjectile](../Assets/02.Scripts/Potion/Projectile/PotionProjectile.cs)의 `Initiate`와 `Launch`로 실제 투척·착지·벽 충돌 경로를 실행한다.
- 포션 설정: 실제 PotionData와 BottleSprite로 PotionItem을 구성한다. 조합 데이터베이스·카탈로그 등 프리팹 참조가 유효한지 검사한다.
- 직접 장판 생성과 투척 생성은 서로 다른 측정으로 표시한다. 유지 시나리오에 투사체·파괴 연출 비용을 섞지 않는다.
- 장판 수가 줄어드는 원인은 만료, 조합, 기화와 생성 실패로 나누어 기록한다.

장판 수명보다 긴 유지 구간에는 두 방식을 구분한다. **원본 수명 시나리오**는 실제 수명 내의 유효 구간만 측정한다. **장기 유지 시나리오**는 전용 데이터 사본에서 수명만 늘리고 변경값을 명시한다. 측정 중 몰래 재생성해 유지 비용으로 계산하지 않는다.

### 리퍼

현재 확인된 프리팹은 [ReaperNormal](../Assets/03.Prefabs/Enemy/ReaperNormal.prefab), [ReaperElite](../Assets/03.Prefabs/Enemy/ReaperElite.prefab), [ReaperRanged](../Assets/03.Prefabs/Enemy/ReaperRanged.prefab), [ReaperAttackIndicator](../Assets/03.Prefabs/Enemy/ReaperAttackIndicator.prefab)다.

[ReaperController](../Assets/02.Scripts/Entity/Enemy/ReaperController.cs)는 거리, 시야, 활성 스킬, 쿨타임, 보유 포션과 플레이어 인벤토리에 따라 스킬을 선택한다. 공개 스킬 강제 실행 API가 있다고 가정하지 않는다.

- 실제 AI 측정: 실제 프리팹과 정상 상태 전이를 사용한다. 프리팹별 허용 스킬·거리와 데이터 참조를 확인하고, 플레이어 위치·인벤토리를 준비한다.
- 스킬 분리: 필요하면 에디터에서 전용 ReaperData·프리팹 사본을 만들어 대상 스킬만 허용한다. 기본 시간·피해·연출 값은 유지하고 변경 항목을 기록한다. 원본 에셋은 수정하지 않는다.
- 탈취 성공 / 빈 인벤토리 / 탈취 실패는 별도 케이스로 표시한다. 성공 케이스에는 실제 포션 이동 연출이 발생했는지 확인한다.
- 경고 단독 측정: 실제 Indicator의 공개 설정·진행도 API를 반복한다. 이 결과는 경고 연출 비용이며 리퍼 전체 스킬 비용으로 보고하지 않는다.
- 동시 발동과 분산 발동은 구분한다. 의도한 시간에 실제 스킬이 시작했는지 기록하고, 실패하면 해당 구간을 무효 처리한다.

### 서비스와 플레이어

SoundManager, 플레이어 탐색 조건, 입력, 인벤토리, 상태·피해 피드백, EnemyPathfinder의 초기화와 해제를 조사한다. 필요한 서비스만 실제 초기화 경로로 구성한다. 누락된 서비스 때문에 효과가 생략되거나 에러가 발생한 실행은 유효한 기준 측정으로 사용하지 않는다.

플레이어가 사망하거나 포션이 고갈돼 부하가 바뀌지 않도록 각 반복 전에 정상 게임 API로 상태를 준비한다. 측정 중 생존 보정이 필요하면 방식과 변경 조건을 기록한다. 지속적으로 체력을 갱신하는 추가 부하를 숨기지 않는다.

## 6. 자동 실행 상태와 시간

```text
Idle → Validate → Setup → Warmup → Measure → Cleanup → Settle
                                            ↑             │
                                            └─ 다음 반복은 Setup부터
전체 완료 → Save → Completed
오류 또는 중단 → Cleanup → Settle → Save → Failed 또는 Stopped
```

- `Validate`: 참조, 레이어, 데이터, 목표 개수와 공간 배치를 확인한다.
- `Setup`: 공간·서비스·플레이어·대상 생성. 준비 비용은 별도 표시한다.
- `Warmup`: 초기 연출·캐시·자원을 준비한다. 첫 사용 비용의 별도 실행과 혼동하지 않는다.
- `Measure`: 유지 구간 또는 투척·스킬 이벤트 시퀀스를 수행한다.
- `Cleanup`: 대상 제거·풀 반환·이벤트 해제·임시 데이터 해제.
- `Settle`: 지연 파괴와 물리 갱신 후 잔존 대상을 확인한다. 고정 대기 시간만으로 정리 성공을 판단하지 않는다.
- `Save`: 측정 구간 밖에서 결과를 일괄 저장한다.

자동 실행 관리와 제한 시간은 unscaled time을 사용하고, 게임 스킬·투사체·효과는 원래 scaled time을 유지한다. 포션 명중의 히트스톱과 카메라 흔들림 때문에 두 시간과 화면 부하가 달라질 수 있다. 원본 피드백 유지 실행과 피드백을 제어한 비교 실행을 구분한다. 러너가 매 프레임 `timeScale = 1`을 덮어쓰지 않는다.

고정 seed는 같은 입력·배치 생성에 사용한다. Unity 물리와 프레임 기반 AI가 기기마다 완전히 같은 결과를 보장하지 않으므로 실제 실행 횟수와 부하를 함께 기록한다. 공통 서비스의 캐시는 시나리오 간 유지될 수 있다. 첫 사용 비용은 새 프로세스의 별도 실행으로 확인한다.

## 7. 첫 구현 범위

분석 문서의 시나리오 ID를 그대로 사용한다. 대표 케이스를 먼저 구현하고 효과 종류·수량 조합의 전체 순회는 이후 확장한다.

| ID | 첫 버전 내용 | 유효성 조건 |
| --- | --- | --- |
| S0 | 실제 바닥·고정 카메라·공통 서비스 기준 | 전투 대상 없음, 다른 공간 비활성 |
| S1 | 대표 장판 유지, 1 / 5 / 10 / 20개 | 의도한 수량·수명·스타일 유지, 조합 방지 배치 |
| S3 | 동일 포션 투척, 벽 없음 / 단순 벽 / 복잡한 모서리 | 실제 착지·클리핑 성공, 이벤트 수 기록 |
| S5 | 리퍼 돌진·탈취·낙하 각각, 우선 1마리 | 의도한 스킬과 세부 결과가 실제 발생 |

기본 워밍업은 5초, 측정은 최대 20초, 반복은 3회다. 유지 시간은 원본 장판 수명에 맞춰 조정한다. 리퍼는 반복 시간뿐 아니라 성공한 스킬 실행 횟수를 기록한다. S0에도 비교 시나리오와 동일한 서비스 구성을 사용한다.

후속 순서는 S2 렌더 면적·겹침 → S4 조합 → S6 적 이동 → S7 효과·피드백 → S8 복합 전투 → S9 반복 수명이다.

## 8. 연출 비활성화 비교 방식

렌더러만 끄는 비교와 시각 갱신까지 끄는 비교는 비용이 다르다. 첫 버전은 **렌더러 비활성화 비교**를 기본으로 하고 정확히 표시한다. 이는 시각 갱신 CPU 비용이 남아 있을 수 있는 비교다.

- 콜라이더, 효과 적용, 스킬 상태 전이는 유지한다.
- 런타임에 생성되는 MeshRenderer·SpriteRenderer·LineRenderer·ParticleSystemRenderer도 처리한다.
- 전체 GameObject를 끄거나 스킬 컨트롤러를 끄는 방식은 사용하지 않는다.
- 첫 버전은 측정 구간에 새 대상이 생기는 경우 생성 이벤트·소유 대상 목록으로 처리한다. 매 프레임 씬 전체 검색을 하지 않는다.
- 나중에 시각 갱신 비활성화 모드를 추가하면, 게임 로직이 시각 컴포넌트와 분리돼 있는지 먼저 확인한다.

오디오 사용 여부도 양쪽에서 동일하게 유지하거나 별도 비교 조건으로 기록한다.

## 9. 계측과 결과 저장

Profiler의 CPU 마커는 분석 문서의 이름을 따른다. 실제 작업 범위를 감싸며 프레임을 넘어 열린 마커를 두지 않는다. 데모 Setup·Cleanup 마커도 추가한다.

첫 버전에서는 Profiler 캡처와 **실행 메타데이터·구간·이벤트 기록**을 우선한다. GPU 지원을 확인하지 않고 CPU 시간이나 프레임 시간을 GPU 시간으로 대체하지 않는다.

- 저장 정보: 실행 ID, 시나리오 설정, Unity·기기·Graphics API, 화면·품질·시간 설정, 단계별 시작/끝 프레임, 실제 대상 수, 이벤트 횟수, 실패 사유.
- 기록은 메모리에 모았다가 측정 종료 후 JSON·CSV로 저장한다. 저장 위치는 `Application.persistentDataPath/PerformanceDemo/<RunId>/`로 제안한다.
- 프레임 통계 자동 수집은 후속 기능이다. 적절한 ProfilerRecorder·프레임 타이밍 지원과 계측 부하를 확인한 뒤 추가한다.
- Profiler 바이너리 자동 저장은 후속으로 검토한다. 최초 버전은 사용자가 Profiler에서 캡처를 저장한다.
- Profiler 기록 버퍼가 전체 순회를 담지 못할 수 있으므로 개별 시나리오 실행과 구간별 캡처를 지원한다.

Development Build에서도 UI 개별 실행과 자동 시작 설정을 제공한다. 일반 측정은 Deep Profile을 끄고, 최적화 전후에 동일한 빌드·화면 설정을 사용한다.

## 10. 구현 단계와 검증

1. **의존성 조사**: 실제 프리팹·포션 데이터·조합 DB·서비스·레이어·플레이어 탐색 조건을 확인한다. 기본 플랫폼은 현재 프로젝트 대상 PC 설정을 따르고 해상도·품질·FPS를 명시한다.
2. **씬 뼈대**: 생성 도구, 고정 카메라, 테스트 공간, 런타임 루트와 S0를 만든다. 저장된 씬을 다시 열어 참조 유지와 빌드 실행을 확인한다.
3. **장판 데모**: S1·S3와 수량 검증, 수명 처리, 생성·유지 구간 마커를 구현한다.
4. **리퍼 데모**: 실제 AI 스킬 조건을 구성하고 S5를 구현한다. 경고 단독 실행은 별도 이름으로 둔다.
5. **반복 실행**: Warmup·Measure·Cleanup·Settle, 오류·중단 경로와 기록 저장을 구현한다.
6. **기준 캡처**: 개별 시나리오 3회와 전체 순회를 실행해 Console 오류, 부하 변동, 잔존 오브젝트를 확인한다. Development Build에서 캡처한다.
7. **확장**: 기준 데이터가 유효한 뒤 후속 시나리오와 자동 통계를 추가한다.

### 최초 버전 완료 기준

- 사람이 전투를 직접 조작하지 않아도 S0·S1·S3·S5를 개별 실행 및 순회할 수 있다.
- 실제 프리팹·실제 초기화 경로가 실행되고, 의도한 장판과 리퍼 스킬이 확인된다.
- 원본 에셋·게임플레이 값이 데모 때문에 수정되지 않는다. 전용 사본 사용 시 차이가 기록된다.
- 측정 구간과 준비·정리 구간을 캡처에서 식별할 수 있다.
- 기대 수량·스킬 횟수 불일치와 필수 참조 누락은 무효 결과로 표시한다.
- 중단·실패 후에도 정리되며, 다음 실행의 부하가 이전 실행에 의해 증가하지 않는다.
- 에디터 재생과 Development Build에서 실행 가능하고 기록을 저장한다.

## 11. 구현 전 확인할 항목

정확한 수량·시간과 씬 의존성은 조사 결과에 따라 조정할 수 있다. 다음 항목은 구현 초기에 확인하되, 가용 데이터로 먼저 기본 시나리오를 진행한다.

- 실제 플레이의 장판·리퍼·적 최대 동시 수와 대표 화면 점유 면적.
- 장판 실제 수명, 조합 자동 발생 조건, 벽 레이어 및 시각 스타일 데이터.
- 리퍼 프리팹별 허용 스킬과 데이터 사본으로 단일 스킬을 재현할 수 있는지.
- 탈취용 플레이어 인벤토리 준비·복구 API와 데모 플레이어 생존 방식.
- 사운드·카메라 피드백·전역 매니저의 초기화와 씬 종료 시 수명.
- GPU Profiler 지원과 목표 빌드의 저장 경로·Profiler 연결.

원래 분석 문서는 **무엇이 비용을 만들 수 있는지**, 이 문서는 **그 비용을 재현하고 비교하는 씬을 어떻게 만들지**를 담당한다. 구현 중 바뀐 구조와 확인된 제약은 이 문서에 반영한다.

## 12. 2026-10-15 구현 현황과 사용법

- 씬: `Assets/01.Scenes/Optimization/PerformanceDemoScene.unity`. 열고 Play 후 왼쪽 패널에서 `Run selected` / `Run all` / `Stop and clean up`을 사용한다.
- 생성 메뉴: `Tools/Performance/Create Demo Scene`. 씬이 이미 있으면 기존 씬을 열며 덮어쓰지 않는다. 일반 빌드 목록은 변경하지 않는다.
- 11개 설정: S0 기준, S1 화염 장판 1·5·10·20개, S3 벽 없음·단순 벽·복잡한 모서리, S5 돌진·포션 탈취 성공·포션 낙하. 설정은 `Assets/PerformanceDemo/`의 ScriptableObject에서 조절한다.
- 바닥과 벽은 현재 유효한 `NormalRoom_1.prefab`의 Tilemap 구성과 최다 사용 타일을 사용한다. 기존 문서의 `Level_1.prefab` 경로는 사용하지 않는다. 한 공간만 활성화한다.
- S1은 원본 화염 포션의 반경 4, 수명 6초를 유지한다. 간격은 지름의 1.1배이며, 측정은 5.5초다. 워밍업용 대상을 먼저 실행하고 정리한 뒤 새 대상으로 측정한다. 측정 도중 보충하지 않는다.
- S3은 실제 `Initiate` / `Launch` 경로를 사용한다. 예측한 벽 충돌 및 착지 위치, 실제 생성된 장판의 위치·콜라이더를 확인한다. 착지 관찰에는 0.1초 간격의 런타임 씬 루트 탐색 부하가 포함된다.
- S5는 원본 리퍼 프리팹의 전용 사본과 단일 스킬만 허용한 ReaperData 사본을 사용한다. 공개 `Action` / `RecoveryFrom` 변화로 시작·완료를 기록한다. 탈취 성공은 인벤토리 감소로 확인한다. 빈 인벤토리·회피 실패 케이스와 다중 리퍼는 후속 범위다.
- 데모 플레이어는 원본 프리팹 사본을 사용하고 입력을 잠근다. PlayerData 사본의 초기·최대 HP만 100000으로 변경한다. 측정 중 회복을 반복하지 않는다.
- 각 반복은 별도 가산 런타임 씬에서 실행된다. 실제 코드가 부모 없이 만든 장판·파괴 이펙트·리퍼 경고도 해당 씬이 소유하며, 정리 때 비활성화 후 씬 전체를 언로드한다. SoundManager·카탈로그·길찾기는 반복 간 유지한다.
- `PerfDemo.Setup`, `PerfDemo.Measure`, `PerfDemo.Cleanup` 마커는 동기 작업 범위만 감싼다. 구간 경계와 프레임·실제 시간·게임 시간·개수·상태 변화는 JSON/CSV로 별도 기록한다.
- 저장: `Application.persistentDataPath/PerformanceDemo/<RunId>/run.json`, `events.csv`, 시나리오·반복별 `.raw`. 러너의 `AutoStart`를 켜면 빌드에서도 자동 순회한다.
- 현재 비교 조건: 원본 렌더러·오디오 유지, 고정 카메라. 카메라 흔들림 컨트롤러와 전역 데미지 숫자 UI는 배치하지 않았다. 전체 인게임 피드백 비용으로 해석하지 않는다. 렌더러 비활성화 비교·GPU/프레임 통계 집계·데모 전용 빌드 메뉴는 아직 구현하지 않았다.
- 검증 도구: `PerformanceDemoSceneBuilder.CreateBatch`는 생성·재열기·참조를 확인한다. `PerformanceDemoSmokeCheck.RunBatch`는 설정을 메모리에 복사해 짧은 1회 순회를 실행한다. `-batchmode -executeMethod PerformanceDemoSmokeCheck.RunBatch`로 실행하며 `-quit`는 지정하지 않는다. 실제 성능 기준 캡처는 화면이 있는 Development Build에서 원래 시간·3회 반복 설정으로 별도 수행해야 한다.
- 검증 결과: Unity 6000.3.22f1에서 스크립트 컴파일, 저장 씬 재열기·참조 검증, 11개 단축 시나리오 순회, 중단 후 정리, 잘못된 설정 실패 처리, 실패 후 재실행을 통과했다. 루트 씬 개수가 실행 전으로 복구되는 것도 확인했다. `-nographics` 실행이므로 화면 품질·GPU 성능 및 Development Build 검증은 아직 수행하지 않았다. 로그는 `Logs/PerformanceDemoCreate.log`, `Logs/PerformanceDemoSmoke.log`에 남는다.

## 13. 측정 구간 자동 Profiler 캡처

- `DemoSystem`의 러너에서 `Capture Profiler`는 기본 활성화다. 에디터 또는 Development Build에서 `Run selected`를 누르면 워밍업과 측정 준비를 끝낸 후 Measure에 들어가면서 기록한다. Profiler 창을 직접 열거나 Record를 누를 필요가 없다.
- `S1_Areas_20_repeat01.raw`처럼 시나리오 이름과 반복 번호로 파일을 나눈다. 저장 폴더는 실행 시작 때 만들고, 실행 ID에는 충돌 방지용 접미사를 붙인다. 정리와 결과 JSON/CSV 쓰기 전에 바이너리 기록을 닫는다.
- S1 장판 생성은 준비 구간에 두고, S3 투척 시작은 Measure 안에서 수행한다. 리퍼는 준비 때 생성한 뒤 컨트롤러를 잠시 멈췄다가 Measure 진입 시 다시 활성화한다. 실제 스킬 로직과 데이터는 변경하지 않는다.
- `run.json`의 `captures`에 파일명, 시나리오, 반복, 시작·종료 프레임/실제 시간, 러너가 발행한 Measure 샘플 수, 파일 크기, `Completed` / `Stopped` / `Failed` 상태와 실패 사유를 저장한다. 에디터 실행 여부·Development Build 여부·씬 경로도 기록한다.
- `Completed`는 실행 중 검증을 통과한 캡처 후보라는 뜻이다. 파일 내부의 실제 마커·함수 비용 검증은 다음 CLI 분석 단계에서 수행하므로 `markerVerification`은 `PendingOfflineAnalysis`로 명시한다. 빈 파일이나 Measure 샘플 0개는 즉시 실패 처리한다.
- `Maximum Frame Gap Seconds` 기본값은 1초다. 이 값을 넘는 프레임/갱신 중단, 연속 `timeScale=0`, 목표 시간 초과를 감지하면 결과를 무효로 저장한다. Measure 중 애플리케이션 포커스 상실도 무효 처리한다(배치 모드 제외). 짧은 게임 히트스톱은 허용하며 timeScale을 매 프레임 강제 복구하지 않는다.
- 사용자 중단, 오류, 러너 비활성화·파괴·애플리케이션 종료 때에도 캡처를 닫고 결과를 저장한다. 프로세스 강제 종료나 운영체제 중단까지 저장을 보장하지는 않는다. 기존 바이너리 Profiler 기록이 있으면 이를 덮어쓰지 않고 실패 처리한다. 자체 캡처 후에는 기존 Profiler 활성 상태와 logFile 설정을 복구한다.
- 새 캡처 경로 검증: Unity 6000.3.22f1의 별도 임시 프로젝트에서 실제 러너·레코더·캡처 소스와 가짜 게임 서비스를 사용해 정상 2회, 측정 중 중단, 수량 불일치 실패, 컴포넌트 비활성화, 재실행, 캡처 끄기, 기존 기록 충돌을 검사했다. 저장된 6개 raw를 다시 열어 Measure 마커 존재 및 Setup/Cleanup 마커 부재를 확인했다. 짧은 히트스톱 허용과 긴 갭·정지·시간 초과 거부도 검사했다. 로그와 검증 캡처는 `Logs/ProfilerCaptureChecks/` 및 `Logs/ProfilerCaptureChecks.log`에 있다. 실제 전투 시나리오와 화면이 있는 Development Build의 성능 검증은 별도로 수행한다.

## 14. 저장 캡처 CLI 분석

`Tools/Profiling/Analyze-Profiler.ps1`에서 실행 결과 폴더나 단일 `.raw` / `.data`를 분석한다. 별도 Unity 프로젝트로 파일을 읽어 측정 마커·실행 상태·기대 샘플 수를 검증하고, 프레임별 CSV·CPU 함수 비용·GC 할당 경로·p50/p95/p99와 Markdown/JSON 보고서를 만든다. 입력과 게임 씬은 변경하지 않는다. 유효한 Measure 내부 프레임만 성능 비교에 사용하며, GPU 시간과 Render Thread CPU 시간을 혼동하지 않는다. 메타데이터가 없는 단일 파일은 마커만 검증했다는 경고를 남긴다.

실행 예시·종료 코드·유효성 기준은 [Profiler CLI 사용법](../Tools/Profiling/README.md)을 따른다. 변경 전후 자동 비교는 후속 단계다.

## 15. MCP 실행·캡처·분석 자동화

`PerformanceDemoMcpTool`의 `performance_demo` 도구는 start/status/stop/analyze를 제공한다. 작업 상태를 SessionState와 `Logs/PerformanceMcp/job.json`에 저장해 플레이 진입·종료에 따른 도메인 리로드를 넘겨 이어간다. 플레이 진입 완료를 기다린 뒤 러너를 실행하며, 원본 시나리오 대신 메모리 사본으로 반복 수를 적용한다. 자동 실행 중 백그라운드 실행을 켜고 종료 시 설정을 복구한다. 직접 시작한 플레이 모드만 종료한다. 정상 캡처 후 기존 CLI를 별도 프로세스로 실행하고 유효성 판정·보고서 경로를 반환한다.

`Start-UnityMcp.ps1`은 설치된 Unity MCP와 버전을 맞춘 로컬 서버를 시작·재사용하고, 열려 있는 에디터에 연결을 요청한다. `Run-PerformanceDemo.py`는 MCP HTTP 프로토콜로 이 도구를 호출한다. 상세 명령은 [CLI / MCP 사용법](../Tools/Profiling/README.md)에 있다. Unity 6000.3.22f1에서 실제 S1_Areas_20 1회 실행 → raw 저장 → 플레이 종료 → CLI 분석을 완료해 유효 캡처 1개를 확인했다. S0 워밍업 중 중지·저장·플레이 종료도 확인했다. 이 검증은 에디터 실행이며 Development Build의 성능 기준 측정은 아니다.
