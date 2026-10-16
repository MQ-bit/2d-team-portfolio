# 게임 루프 시나리오 확장 사용법

기존 `Assets/01.Scenes/Optimization/PerformanceDemoScene.unity`에 항목을 추가하는 구현이다. 기존 11개 시나리오를 보존하고, UI 비교 및 S4/S6/S7/S8/S9 계열 24개를 추가해 총 35개를 구성한다.

추가 24개 전체의 실제 Unity Profiler 측정 결과는 [게임 루프 성능 분석 보고서](PerformanceDemoGameLoopProfilingReport.md)에 있다. UI ON 기준을 포함해 25개 조건·34개 raw를 검증했으며, 비교표·개별 상세·프레임 그래프·무효 실행 진단을 포함한다. 에디터 측정이므로 제품 기기의 성능 기준과 구분한다.

## 추가된 항목

| 계열 | 에셋/변형 | 개수 |
| --- | --- | ---: |
| UI 비교 | S0_Baseline_UI_Off | 1 |
| 조합 | Pair, NoOverlap, Burst 5/10쌍, Staggered 5/10쌍, SimpleWall, ComplexCorner | 8 |
| 추적 | Moving/Static 각각 적 10/30/60개, Moving_Dense_30 | 7 |
| 접촉 효과 | Area_ContactEffects 적 1/10/30개, Combine_ContactEffects_10 | 4 |
| 복합 전투 | S8_Combat_Minimal, Representative, Peak | 3 |
| 반복 전투 | S9_Combat_Lifecycle | 1 |

기존 씬을 다시 생성할 필요는 없다. 에디터의 `Tools > Performance > Add Game Loop Scenarios` 메뉴는 누락된 에셋과 러너 참조만 추가한다. 이미 존재하는 같은 이름의 시나리오 설정을 덮어쓰지 않는다. 데모 씬이 수정 중이면 먼저 저장해야 한다.

## 실행

씬의 Demo UI에서 시나리오를 선택해 실행하거나, 기존 MCP/CLI를 사용한다. 새 시나리오는 기본적으로 실행 중 Demo UI를 숨기고 완료 후 다시 표시한다. 중지는 MCP 또는 플레이 종료로 가능하다.

```powershell
python Tools/Profiling/Run-PerformanceDemo.py start --scenario S4_Combine_Burst_05 --repetitions 1 --wait
python Tools/Profiling/Run-PerformanceDemo.py start --scenario S6_Chase_Moving_10 --repetitions 1 --wait
python Tools/Profiling/Run-PerformanceDemo.py start --scenario S8_Combat_Minimal --repetitions 1 --wait
```

기능만 확인할 때는 `--no-capture --no-analyze`를 함께 지정한다. 이 모드는 raw와 CPU 성능 보고서를 만들지 않으며 포커스 이탈을 성능 캡처 실패로 판단하지 않는다. 실제 Profiler 캡처 실행은 기존 포커스·프레임 중단 유효성 검사를 유지한다.

`S9_Combat_Lifecycle`은 기본적으로 10개 전투 사이클을 실행한다. 사이클마다 별도 raw를 만들고, 워밍업과 측정 후 정리 지점에서 관리 힙·Texture/Mesh/Material 수를 기록한다. 이 값은 에디터 및 로드된 자원을 포함하는 추세 관측용이며 Memory Profiler의 참조 분석이나 누수 판정을 대체하지 않는다.

## 실제 게임 코드 연결

- 런타임 전용 InputReader를 해당 데모 플레이어에만 연결한다. 이동·대시·슬롯·조준/투척·근접 공격은 기존 PlayerController의 입력 처리와 상태 제한을 거친다. 공유 InputReader 에셋을 수정하지 않는다.
- S4는 진단용 실제 PotionProjectile 투척으로 독립적인 fire/poison 장판 쌍을 조합한다. 기존 조합 규칙과 클리핑·피해 코드를 사용한다. 이미 조합된 장판에 세 번째 장판을 강제로 재조합하지 않는다.
- S6/S7은 실제 SpawnManager와 원본 적 프리팹의 AI·물리·공격·피해를 사용한다. 부하 유지용 플레이어 고체력 사본은 `DiagnosticHighHealth`와 변경 설명에 명시한다.
- S8/S9는 원본 플레이어 체력·실제 인벤토리 소비와 쿨다운을 유지하고 실제 WaveController로 웨이브를 진행한다. 최소 전투/수명 반복은 원거리 적 3개씩 2웨이브이며, 순간이동·직접 적 체력 감소·강제 웨이브 완료를 사용하지 않는다. 자폭 적과 원거리 적의 혼합은 Representative/Peak에서 별도로 실행한다.
- 자동 입력은 고정된 첫 투척 순서 후 가까운 적에게 접근해 근접 공격하고 자폭 예열을 피한다. 이 입력 정책 ID와 실제 결과를 기록한다. 완전히 고정된 이동 경로만 재생하는 조건과 구분한다.
- Minimal/Lifecycle은 모든 웨이브 완료가 필수다. Representative/Peak는 초기 설정 적 수·투척·조합·피격이 실제로 발생한 뒤의 패배도 `outcome=PlayerDefeated`로 허용한다. 패배를 웨이브 완료로 기록하지 않으며, 승리와 패배 캡처를 동일 작업량의 성능 비교로 섞지 않는다.
- 조합 확정과 적 생성 관측 이벤트를 실제 코드에 추가했다. 이벤트 구독은 실행 종료·중단 때 해제한다.

## 결과와 유효성

기존 run.json/events.csv/raw 저장 위치를 유지한다. run.json의 `schemaVersion=2`, `gameLoops`에는 이동 거리, 입력 시도/수락, 투척·근접·대시, 실제 생성·피해·처치/해제, 조합 성공·웨이브 시작/완료와 기능 판정을 기록한다. 기존 `captures`의 기록 상태와 오프라인 마커 검증은 계속 분리한다.

조합이 일어나지 않았거나 플레이어가 움직이지 않거나 예상 웨이브가 끝나지 않으면 성공으로 처리하지 않는다. 정상 기능 결과도 캡처 유효성과 제품의 성능 목표 달성을 뜻하지 않는다. 중단·포커스 이탈·긴 프레임 중단으로 무효인 캡처는 기준 성능 비교에서 제외한다.

## 구현 범위

이번 구현은 고정 공간의 전투 루프와 원인 분리 시나리오다. 맵 생성·방 이동·StageManager/StageEncounterCoordinator를 연결한 S8_StageLoop는 포함하지 않는다. 대표/최대 부하 수량은 임시 실험값이며 목표 기기의 실제 전투 조건에 맞춰 확정해야 한다.

전용 S4_Spread의 내부/외부 대상 도달 비교, 상태별 진입/이탈 검증, 다중 리퍼·포션/조합 종류별 확장, VFX ON/OFF 비교, GPU 분석, 실제 게임 HUD·카메라 흔들림 포함, 이벤트 프레임별 CPU 귀속 분석과 Development Build 검증은 후속 범위다. 현재 S7은 실제 포션 피해가 발생했는지 검증하는 조건이다.

상세 설계와 다음 단계는 [PerformanceDemoGameLoopScenarioPlan.md](PerformanceDemoGameLoopScenarioPlan.md)를 따른다. 기능 검증 결과는 `Logs/GameLoopScenarioValidation/`에 저장한다.

### 반복 전투 검증

Unity 6000.3.22f1 Editor의 Profiler 기록 없는 기능 검증에서 S9 10사이클 모두 원거리 적 6개·2웨이브 완료를 통과했다. 워밍업/측정 후 정리 20회 모두 씬 수가 1개로 복원됐다. 해당 실행에서 관측한 Texture/Mesh/Material 수는 1524/135/215로 유지됐다. 이 숫자는 로드된 에디터 자원까지 포함하며 누수 없음의 증명은 아니다.

### 전체 검증 결과

- 신규 24개와 기존 장판 유지/투척/리퍼 3개, 총 27개 조건을 1회씩 기능 검증했다. S9는 10사이클을 포함한다. 최종 27개 모두 통과했고, 중단·포커스 이탈·긴 갱신 중단 및 수정 전 실패 실행은 최종 성공 결과와 별도로 보존했다.
- 순차 조합 실행의 Measure 도중 중지를 요청해 `Stopped` 및 `outcome=Interrupted`, 기능 미통과, 결과 저장과 플레이 종료를 확인했다.
- 최소 전투와 최대 부하를 각각 Profiler 캡처 후 동일 Unity 버전 CLI로 다시 읽어 유효 캡처 1개씩 확인했다. 최소 전투는 `CombatCompleted`, 최대 부하는 `PlayerDefeated`였으며 후자는 웨이브 완료와 구분하는 경고가 보고서에 포함됐다.
- 최소 전투 캡처의 기대/실제 Measure 샘플은 1127/1127, 통계 선택 프레임은 1125개였다. 에디터 검증이며 제품 기기의 성능 기준 측정은 아니다.
- .NET 스크립트 빌드는 오류 0개로 통과했다. 기존 패키지 버전 경고 4개는 남아 있다. 저장된 씬의 시나리오 참조 35개가 중복 없이 존재하는지 확인했다.

로컬 검증 상세: `Logs/GameLoopScenarioValidation/final/report.md`, `results.json`, `stop-check.json`, `profiler-check.json`, `profiler-defeat-check.json`.

Profiler 상세 보고서:

- `Logs/ProfilerReports/mcp_20261015_055646_c5062691/capture_001/report.md` — 최소 전투 완료.
- `Logs/ProfilerReports/mcp_20261015_055746_7be431a6/capture_001/report.md` — 최대 부하의 허용된 패배.
