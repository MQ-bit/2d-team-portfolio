# 추가 게임 루프 시나리오 Profiler 측정 보고서

## 측정 조건

Unity 6000.3.22f1 Windows Editor, Play Mode, Deep Profile OFF, Profile Editor OFF. Unity binary Profiler raw를 기록하고 동일 버전의 RawFrameDataView로 다시 읽었다. 설정과 게임 코드는 측정을 위해 변경하지 않았다.

신규 24개와 UI ON 기준 1개를 각각 유효 실행 1회씩 확보했다. 무효 시도는 별도 보존한다. S9는 한 실행 안에서 10사이클을 기록한다. 반복 간 분산을 검증한 성능 기준값이 아니라 병목 후보 탐색 자료다. 새 시나리오는 Demo UI OFF, 기존 기준은 ON이다.

전체 프레임 시간에는 에디터 작업·대기가 포함된다. PlayerLoop 평균은 Main Thread의 PlayerLoop 마커 inclusive 시간이며 그 내부 대기도 포함할 수 있다. 두 값을 빼서 순수 EditorLoop 비용으로 간주하지 않는다. GC는 메인 스레드 할당량이며 잔존 메모리/누수가 아니다. GPU 시간은 측정하지 않았다.

PerfDemo.Measure 내부 프레임만 선택하고 시작·종료 경계와 Setup/Cleanup을 제외했다. 런타임 상태·게임 루프 기능 판정·raw 파일·Measure 샘플 수를 함께 검증했다. 실패 시도는 results.json에 보존하고 아래 유효 통계에서 제외한다.

현재 유효 조건 25개, 유효 raw 34개. 전체 시도 27회.

![전체 프레임 평균과 p95](../Logs/GameLoopProfiling/comparison.svg)

## 전체 결과

| 시나리오 | 평균 ms | p95 ms | p99 ms | 최대 ms | >16.67 / >33.33 | PlayerLoop 평균 ms | 전체 GC KiB/f | PlayerLoop GC KiB/f |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| S0_Baseline | 7.736 | 10.825 | 13.572 | 23.103 | 2 / 0 | 3.147 | 39.66 | 23.35 |
| S0_Baseline_UI_Off | 8.087 | 11.365 | 13.789 | 76.365 | 3 / 1 | 2.167 | 16.93 | 0.58 |
| S4_Combine_Burst_05 | 9.969 | 15.938 | 19.609 | 217.737 | 20 / 3 | 4.077 | 23.16 | 6.60 |
| S4_Combine_Burst_10 | 11.758 | 17.530 | 44.954 | 596.739 | 36 / 8 | 5.646 | 32.21 | 15.74 |
| S4_Combine_ComplexCorner | 9.082 | 14.496 | 25.822 | 132.678 | 17 / 6 | 3.248 | 19.02 | 2.63 |
| S4_Combine_NoOverlap | 8.004 | 11.150 | 16.157 | 20.017 | 7 / 0 | 2.296 | 17.63 | 1.25 |
| S4_Combine_Pair | 8.570 | 14.227 | 16.866 | 27.191 | 14 / 0 | 2.783 | 17.65 | 1.20 |
| S4_Combine_SimpleWall | 8.890 | 14.966 | 18.418 | 131.893 | 22 / 1 | 3.135 | 19.00 | 2.59 |
| S4_Combine_Staggered_05 | 9.708 | 14.808 | 20.663 | 151.635 | 27 / 5 | 3.607 | 21.84 | 5.13 |
| S4_Combine_Staggered_10 | 10.167 | 14.723 | 62.520 | 196.881 | 35 / 16 | 4.375 | 28.13 | 11.12 |
| S6_Chase_Moving_10 | 9.853 | 14.885 | 18.633 | 22.785 | 40 / 0 | 3.416 | 20.33 | 2.05 |
| S6_Chase_Moving_30 | 11.455 | 16.780 | 21.332 | 25.758 | 67 / 0 | 4.287 | 24.45 | 4.85 |
| S6_Chase_Moving_60 | 13.906 | 20.050 | 23.802 | 30.050 | 281 / 0 | 6.094 | 29.98 | 9.63 |
| S6_Chase_Moving_Dense_30 | 13.275 | 14.410 | 20.900 | 506.806 | 17 / 9 | 4.958 | 24.39 | 4.82 |
| S6_Chase_Static_10 | 11.034 | 11.302 | 13.308 | 500.504 | 10 / 10 | 4.042 | 20.32 | 2.05 |
| S6_Chase_Static_30 | 11.987 | 14.640 | 21.032 | 496.418 | 28 / 7 | 3.847 | 24.36 | 4.74 |
| S6_Chase_Static_60 | 17.751 | 19.460 | 83.262 | 718.297 | 238 / 15 | 8.116 | 30.01 | 9.28 |
| S7_Area_ContactEffects_01 | 8.889 | 10.858 | 12.213 | 45.845 | 3 / 2 | 2.333 | 20.19 | 2.95 |
| S7_Area_ContactEffects_10 | 9.237 | 12.079 | 18.632 | 49.756 | 11 / 1 | 3.051 | 25.33 | 7.41 |
| S7_Area_ContactEffects_30 | 10.743 | 16.052 | 21.335 | 52.763 | 16 / 3 | 3.852 | 35.50 | 16.62 |
| S7_Combine_ContactEffects_10 | 8.943 | 12.057 | 14.269 | 118.577 | 3 / 2 | 2.928 | 20.85 | 3.56 |
| S8_Combat_Minimal | 9.358 | 12.472 | 16.642 | 45.237 | 9 / 3 | 3.036 | 20.75 | 2.63 |
| S8_Combat_Peak | 11.139 | 15.133 | 25.085 | 78.087 | 16 / 2 | 4.117 | 30.05 | 10.71 |
| S8_Combat_Representative | 10.236 | 13.383 | 19.134 | 51.351 | 12 / 2 | 3.518 | 24.15 | 5.03 |
| S9_Combat_Lifecycle | 13.090 | 11.194 | 56.566 | 526.160 | 121 / 97 | 6.346 | 20.29 | 2.39 |

## 무효/실패 시도

아래 시도는 위 유효 통계에 합치지 않았다. 재측정 성공이 첫 중단의 원인을 설명하거나 큰 지연이 없었음을 입증하지는 않는다. 원본은 보존했다.

- S6_Chase_Moving_Dense_30, 시도 1: Frame/update gap 1.010s exceeds 1.000s; interval is invalid.
  실행 원본: `C:/Users/sdkmg/AppData/LocalLow/DefaultCompany/PotionJunkie\PerformanceDemo\20261015_063024_775_5c488e03`

- S9_Combat_Lifecycle, 시도 1: Frame/update gap 1.001s exceeds 1.000s; interval is invalid.
  실행 원본: `C:/Users/sdkmg/AppData/LocalLow/DefaultCompany/PotionJunkie\PerformanceDemo\20261015_063826_361_2936371c`

## 분석과 최적화 우선순위

이번 결과에서 우선 검토할 대상은 **착탄·조합 순간의 CPU 스파이크, 상태이상 시각 효과의 GC, 다수 원거리 적의 탐색 비용**이다. 측정은 최적화 후보를 보여준다. 목표 기기의 성능 기준이나 변경 후 개선 효과를 확정한 것은 아니다.

### 1. 착탄·조합 순간의 CPU 비용 — 우선순위 높음

| 조건 | 전체 p95 ms | 전체 최대 ms | 조합 마커 최대 단일 호출 ms | 실제 조합 수 |
| --- | ---: | ---: | ---: | ---: |
| 비중첩 | 11.150 | 20.017 | 없음 | 0 |
| 한 쌍 | 14.227 | 27.191 | 8.152 | 1 |
| 동시 5쌍 | 15.938 | 217.737 | 63.306 | 5 |
| 동시 10쌍 | 17.530 | 596.739 | 72.745 | 10 |
| 순차 5쌍 | 14.808 | 151.635 | 58.263 | 5 |
| 순차 10쌍 | 14.723 | 196.881 | 71.136 | 10 |

동시 10쌍의 `PerfDemo.CombineCommit`은 10회 합계 377.659ms, inclusive GC 3,384,044bytes였다. `PotionProjectile.Update` 최대 단일 호출도 149.470ms여서 장판 생성·착탄과 그 하위 조합 처리 모두 확인할 필요가 있다. 조합 마커는 형상·클리핑·시각 효과 설정·효과 전환·관측 이벤트를 포함한다. 순수 조합 탐색 시간으로 해석하지 않는다.

순차 실행은 이번 기록에서 최대 프레임을 낮췄지만, 단일 조합 최대는 여전히 약 71ms이고 33.33ms 초과 프레임은 동시 10쌍 8개, 순차 10쌍 16개였다. **작업을 나누는 것만으로 비용 자체가 해결됐다고 판단할 수 없다.** 최대 프레임 전체를 특정 하위 함수에 귀속하는 이벤트 프레임 분석은 아직 하지 않았다.

벽 조건의 조합 마커 최대는 단순 벽 약 47.77ms, 복잡한 모서리 약 49.87ms였다. 단일 실행만으로 벽 복잡도에 따른 비례 관계를 확정할 수 없다.

소스에서 우선 분리 계측할 구간:

- `Assets/02.Scripts/Potion/Combination/PotionCombinedAreaShape.cs`의 `Create`: 형상 생성, 벽 조회/차집합, collider 설정, 시각 효과 연결을 함께 수행한다.
- `Assets/02.Scripts/Potion/Visual/PotionAreaMaskRenderer.cs`의 `SetFootprint`: 잘린 장판에 대해 Color32 배열 생성, 최대 축 256 크기의 픽셀 판정, Texture2D 생성/업로드를 수행한다. CPU 경로 안에서 수행되므로 GPU 시간 측정이 없어도 생성 비용은 조사할 수 있다.
- `Assets/02.Scripts/Potion/Cliping/PotionAreaFootprint.cs`, `PotionWallQuery.cs`: 형상 배열/컬렉션과 collider 경로 생성.

다음 검증은 위 구간에 개별 마커를 추가하고, 중복 생성 제거·형상/마스크 재사용·시각 객체 재사용을 각각 적용해 동일 조건을 재측정하는 순서다. 현재 캡처만으로 특정 줄이 73ms의 원인이라고 확정하지 않는다.

### 2. 상태이상 시각 효과의 반복 할당 — 우선순위 높음

S7 장판 접촉 30명에서 PlayerLoop GC는 **16.62KiB/frame**이며, `PotionStatusVisual.LateUpdate` inclusive GC가 **10.74KiB/frame**, 약 65%였다. 해당 함수의 inclusive CPU는 평균 0.330ms/frame이다. 할당 경로 CSV에서 이 함수 바로 아래 126,600회, 7,085,074bytes의 GC.Alloc가 확인되고, MaterialEditor 하위 할당은 별도 경로로 기록된다.

소스상 유력한 조사 경로는 `PotionStatusVisual.IsActive` → `PotionEffectReceiver.IsInStatusArea` → `PotionAreaEffectTracker.HasStatus` → `IPotionEffectData.GetEffects`다. HasStatus는 상태별로 효과 목록을 반복 조회하고, GetEffects는 `yield return` 기반 iterator다. 다만 Deep Profile을 끈 현재 캡처에는 이 세부 호출 스택이 없어, **개별 할당의 정확한 호출 지점은 추가 할당 스택 검증이 필요하다.**

상태/장판 변경 시 표시 상태를 갱신하거나, 반복 iterator 생성 없이 효과를 조회하는 방식을 우선 실험할 가치가 있다. 장판 타임라인의 `Synchronize`에도 매번 HashSet 및 키 목록을 생성하는 코드가 있어, `PotionAreaEffect.Update`/`PotionEffectReceiver.Update`의 할당도 다음 대상으로 확인한다. 이번 작업에서는 게임 로직을 변경하지 않았다.

S7의 적은 원래 체력으로 실제 사망한다. 1/10/30명 조건에서 포션 피해 이벤트 6/60/180회와 적 사망 1/10/30개를 확인했다. 이 수치는 초기 접촉과 사망까지 포함한 구간이며, 적 30명이 계속 생존하는 지속 부하로 해석하지 않는다. 조합 접촉 10명은 조합 1회, 포션 피해 이벤트 40회, 사망 10개였다.

### 3. 다수 원거리 적의 탐색과 FixedUpdate — 우선순위 중간

| 추적 조건 | PlayerLoop 평균 ms/f | RangedEnemyController.FixedUpdate inclusive ms/f | 같은 함수 평균 us/호출 |
| --- | ---: | ---: | ---: |
| 이동 10명 | 3.416 | 0.103 | 24.92 |
| 이동 30명 | 4.287 | 0.427 | 31.13 |
| 이동 60명 | 6.094 | 1.464 | 44.36 |
| 정지 10명 | 4.042 | 0.100 | 22.37 |
| 정지 30명 | 3.847 | 0.551 | 42.19 |
| 정지 60명 | 8.116 | 2.455 | 65.37 |

프레임당 값은 물리 스텝 수와 화면 대기의 영향도 받는다. 호출당 비용도 함께 보면 많은 적에서 원거리 적 처리 비용이 커지는 관측이 남는다. 이동 60명의 물리 범위 조회 마커 `Physics2D.OverlapCircleAll`은 Self 평균 약 0.633ms/frame이었다.

`Assets/02.Scripts/Entity/Enemy/RangedEnemyController.cs`는 매 FixedUpdate에 `FindPlayerInRange`를 호출하고, 전체 대상 필터로 OverlapCircle을 수행해 각 collider의 부모 컴포넌트를 확인한다. **C# 소스는 이미 재사용 List를 쓰므로, native 마커 이름의 All만 보고 새 배열 할당이 원인이라고 판단하지 않는다.** 플레이어 레이어 필터·대상 캐시·탐색 주기를 검토하되 타겟 전환과 감지 범위를 유지하는지 함께 검증해야 한다.

추적 조건은 기능 검증에서 적 수 유지와 실제 적 이동을 확인했다. 부하 유지용 고체력 플레이어 사본을 사용하며 실제 게임의 사망 빈도 비교에는 사용하지 않는다.

### 4. 에디터·화면 표시 대기는 별도 해석

유효한 밀집 30명 재측정의 최대 프레임은 약 506.81ms였다. `GfxDeviceD3D12.WaitForLastPresentation.WaitForGPU` 최대 단일 대기는 약 465.54ms였다. 정지 10명에서도 전체 최대 약 500.50ms와 표시 대기 약 483.19ms가 함께 관측됐다. 이 marker 최대값들만으로 서로 같은 프레임이라고 단정하거나 중첩 시간을 더하지 않는다. 적 계산만으로 설명할 수 없는 큰 표시 대기가 존재함을 보여준다.

PlayerLoop에도 표시 대기가 포함된다. 따라서 PlayerLoop를 분리했어도 그 전체가 순수 게임 CPU 연산은 아니다. WaitForGPU라는 마커 이름만으로 GPU 자체가 병목이라고 확정할 수도 없다. 실제 GPU 시간, 드라이버/화면 표시 조건, Development Build에서 재검증해야 한다.

### 5. Demo UI OFF의 효과는 함수 비용으로 확인

이번 UI ON 기준에서 `PerformanceDemoView.OnGUI`는 Self 약 0.679ms/frame, inclusive 약 1.215ms/frame, inclusive GC 약 21.67KiB/frame이었다. UI OFF에서는 같은 함수의 GC가 0이고 Self 약 0.002ms/frame이었다. PlayerLoop GC도 23.35 → 0.58KiB/frame으로 줄었다.

그러나 전체 평균 프레임은 ON 7.74ms, OFF 8.09ms로 개선되지 않았다. 에디터·대기·시간에 따른 변동이 있으므로 이 한 쌍으로 전체 FPS 개선률을 제시하지 않는다. UI 비용이 제거된 사실과 제품 성능 개선 판정은 구분한다.

### 6. 실제 복합 전투 결과의 비교 범위

| 조건 | 전체 평균 / p95 ms | PlayerLoop 평균 ms | PlayerLoop GC KiB/f | 실제 결과 |
| --- | ---: | ---: | ---: | --- |
| Minimal | 9.358 / 12.472 | 3.036 | 2.63 | 적 6개, 2웨이브 완료, 조합 2회, 투척 4회 |
| Representative | 10.236 / 13.383 | 3.518 | 5.03 | 적 20개 생성, 3웨이브 중 2번째 시작 후 플레이어 패배 |
| Peak | 11.139 / 15.133 | 4.117 | 10.71 | 적 60개 생성, 2번째 웨이브 중 플레이어 패배 |

세 조건 모두 기능·캡처 유효성을 통과했다. Representative/Peak는 원래 설정대로 필요한 상호작용 후의 패배를 허용한 실행이며, 모든 적 처치나 전 웨이브 완료를 뜻하지 않는다. Minimal 승리와 패배 조건을 같은 작업량의 성능 비교로 섞지 않는다. 전투는 고정 공간이며 Stage/방 이동·실제 게임 HUD와 GPU 분석은 포함하지 않았다.

### 7. 반복 전투 10사이클 — 기능 통과, 표시 대기와 초기 자원 증가 관측

재측정 10사이클 모두 적 6개·2웨이브를 완료했고, 사이클마다 조합 2회가 발생했다. raw 10개 모두 런타임·기능·마커 검증을 통과했다. 전체 7,873개 선택 프레임의 평균은 13.090ms, p95는 11.194ms, p99는 56.566ms, 최대는 526.160ms였다. 소수의 긴 프레임 때문에 평균이 p95보다 높다. 16.67ms 초과는 121개, 33.33ms 초과는 97개다.

PlayerLoop 평균은 6.346ms/frame이지만, `TimeUpdate.WaitForLastPresentationAndUpdateTime`만 inclusive 평균 3.815ms/frame, 최대 511.38ms였다. 이 시간을 모두 전투 연산으로 해석하면 안 된다. PlayerLoop GC는 2.386KiB/frame이다.

| 사이클 | 평균 ms | p95 ms | 최대 ms | 전체 GC KiB/f |
| --- | ---: | ---: | ---: | ---: |
| 1 | 12.412 | 11.638 | 513.403 | 20.927 |
| 2 | 10.695 | 11.071 | 519.030 | 20.132 |
| 3 | 15.160 | 11.529 | 518.573 | 20.269 |
| 4 | 13.620 | 11.117 | 495.226 | 20.927 |
| 5 | 11.370 | 10.537 | 495.894 | 20.034 |
| 6 | 15.409 | 10.846 | 526.160 | 20.222 |
| 7 | 12.607 | 11.338 | 517.540 | 20.261 |
| 8 | 11.733 | 10.855 | 517.294 | 19.933 |
| 9 | 12.406 | 11.086 | 498.915 | 19.957 |
| 10 | 17.190 | 13.334 | 495.475 | 20.387 |

정리 지점 20회 모두 씬 수는 1개였다. Mesh 135개, Material 257개는 유지됐다. Texture는 첫 워밍업 후 1,567개에서 첫 측정 후 1,578개로 11개 증가하고 이후 유지됐다. 초기 증가 원인은 이 관측만으로 확정할 수 없다. 관리 힙 관측은 약 897.39~955.89MiB 사이에서 증가와 감소가 모두 나타났다. 에디터·Profiler·로드된 공유 자원을 포함하는 값이므로 누수 없음이나 누수 확정의 근거로 쓰지 않는다. 장기 반복 및 Memory Profiler 참조 비교는 후속 검증이다.

### 8. 무효 실행 두 건의 raw 진단

최초 밀집 추적 30명은 갱신 간격 1.010초로 중단됐다. raw의 전체 최대 프레임은 1,011.96ms, `SceneView.Paint` 최대는 992.29ms였고 PlayerLoop 최대는 22.41ms였다. 최초 반복 전투의 두 번째 사이클도 갱신 간격 1.001초로 중단됐다. 해당 raw의 전체 최대는 999.42ms, SceneView.Paint 최대는 982.04ms, PlayerLoop 최대는 18.61ms였다. 두 raw 모두 Render Thread의 Semaphore.WaitForSignal에 약 978~988ms의 대기도 기록됐다.

이 자료는 에디터 SceneView 처리/대기가 긴 구간이 있었음을 보여주며, 실제 게임 로직이 1초 동안 계산했다는 근거는 아니다. SceneView 내부의 구체적인 지연 원인까지 확정하지는 않았다. 두 실행은 정상 성능 통계에서 제외하고 원본과 진단 결과를 보존했다. 반복 전투 첫 실행의 정상 첫 사이클 raw는 개별 유효성을 통과했지만, 완주한 재측정 10사이클과 섞지 않았다.

진단 보고서 경로:

- 밀집 추적: `Logs/ProfilerReports/mcp_20261015_064301_f1dc2412/capture_001/report.md`
- 반복 전투 첫 실행: `Logs/ProfilerReports/mcp_20261015_064433_d01a4661/report.md`
- 진단 원본 목록: `Logs/GameLoopProfiling/invalid-diagnostics.json`

### 후속 검증 순서와 판정

1. 착탄/조합을 형상·클리핑·마스크·VFX 구간으로 나눠 계측한다. 한 변경씩 적용하고 동시/순차 10쌍을 최소 3회씩 비교한다. 같은 실제 조합 수·효과를 유지하면서 단일 처리 최대와 p95/p99, 예산 초과 프레임이 줄어야 한다.
2. 상태 표시와 타임라인 할당을 분리 검증한다. S7 30명과 조합 접촉 10명에서 같은 피해·사망·표시 결과를 유지하며 PlayerLoop GC 및 해당 함수 GC가 줄어야 한다. 시각 효과를 꺼서 결과가 달라지는 실험은 별도 조건으로 기록한다.
3. 적 탐색을 검토한다. 이동/정지 10·30·60명에서 같은 감지·공격 동작과 적 수를 유지하고 FixedUpdate 호출당 비용 및 게임 구간 비용이 감소하는지 본다.
4. 마지막으로 목표 기기의 Development Build에서 각 조건을 최소 3회 재측정한다. 표시 대기와 GPU 시간을 별도로 확인하고 목표 프레임 예산으로 판정한다. 이번 에디터 수치만으로 60FPS 보장을 선언하지 않는다.


## 개별 상세 결과

각 raw의 report.md, frames.csv, hotspots.csv, allocation-sites.csv는 아래 경로에 있다. 이 문서에도 주요 상세 수치를 포함해 링크 없이 확인할 수 있게 했다.

### S0_Baseline

![프레임 추이](../Logs/GameLoopProfiling/S0_Baseline.svg)

선택 프레임 2,585개 / 유효 raw 1개.

CPU 상위 Self 비용(메인 스레드, inclusive 값 중첩 합산 금지):

| 마커 | Self ms/f | Inclusive ms/f | Inclusive GC KiB/f | 최대 단일 호출 ms |
| --- | ---: | ---: | ---: | ---: |
| OnGUI | 1.8528 | 5.6577 | 36.902 | 14.660 |
| Assembly-CSharp.dll!::PerformanceDemoView.OnGUI() [Invoke] | 0.6791 | 1.2151 | 21.674 | 6.140 |
| GfxDeviceD3D12.WaitForLastPresentation.WaitForGPU | 0.4886 | 0.4886 | 0.000 | 8.435 |
| UnityEngine.IMGUIModule.dll!UnityEngine::GUIStyle.GetMeshInfo() [Invoke] | 0.3891 | 0.3913 | 0.072 | 0.496 |
| Inl_On Record Render Graph | 0.3449 | 0.3460 | 0.000 | 0.641 |
| WaitForJobGroupID | 0.2834 | 0.2993 | 0.000 | 0.370 |
| IMGUIContainer | 0.2670 | 5.9618 | 38.340 | 18.006 |
| Material.SetPassFast | 0.2533 | 0.2676 | 0.000 | 0.255 |
| UnityEngine.IMGUIModule.dll!UnityEngine::GUIStyle.GetDimensions() [Invoke] | 0.2222 | 0.2241 | 0.019 | 2.545 |
| Profiler.FlushMemoryCounters | 0.1655 | 0.1655 | 0.000 | 0.486 |
| UIR.DrawChain | 0.1366 | 6.1491 | 38.340 | 18.039 |
| AssetDatabase.GetGUIDFromAssetPathV2 | 0.0829 | 0.0829 | 0.000 | 0.228 |
| UnityEngine.IMGUIModule.dll!UnityEngine::GUIUtility.EndGUI() [Invoke] | 0.0815 | 0.1251 | 1.108 | 2.015 |
| Inl_UniversalRenderPipeline.RenderSingleCameraInternal: Main Camera | 0.0674 | 0.6042 | 0.000 | 1.221 |
| Inl_NRPRGComp_ExecuteBeginRenderpassCommand | 0.0636 | 0.0636 | 0.000 | 0.237 |

게임 코드 별도 보기(상위 inclusive 구간, 서로 중첩 가능):

| 함수/마커 | Inclusive ms/f | Self ms/f | Inclusive GC KiB/f | 평균 호출 us |
| --- | ---: | ---: | ---: | ---: |
| Assembly-CSharp.dll!::SoundManager.Update() [Invoke] | 0.0039 | 0.0039 | 0.000 | 3.95 |

실행 원본: `C:/Users/sdkmg/AppData/LocalLow/DefaultCompany/PotionJunkie\PerformanceDemo\20261015_062040_014_021e4e94`

분석 폴더: `C:\Users\sdkmg\Desktop\project\2d-team01\Logs/ProfilerReports/mcp_20261015_062033_162200f2`
- repeat 1: Measure 기대/실제 2586/2586; `C:\Users\sdkmg\Desktop\project\2d-team01\Logs\ProfilerReports\mcp_20261015_062033_162200f2\capture_001`

### S0_Baseline_UI_Off

![프레임 추이](../Logs/GameLoopProfiling/S0_Baseline_UI_Off.svg)

선택 프레임 2,473개 / 유효 raw 1개.

CPU 상위 Self 비용(메인 스레드, inclusive 값 중첩 합산 금지):

| 마커 | Self ms/f | Inclusive ms/f | Inclusive GC KiB/f | 최대 단일 호출 ms |
| --- | ---: | ---: | ---: | ---: |
| OnGUI | 2.4264 | 5.5194 | 14.118 | 5.618 |
| GfxDeviceD3D12.WaitForLastPresentation.WaitForGPU | 0.5690 | 0.5690 | 0.000 | 9.851 |
| Inl_On Record Render Graph | 0.4419 | 0.4434 | 0.000 | 0.746 |
| IMGUIContainer | 0.3552 | 5.9253 | 15.555 | 6.637 |
| WaitForJobGroupID | 0.2880 | 0.3041 | 0.000 | 0.409 |
| Material.SetPassFast | 0.2035 | 0.2161 | 0.000 | 0.238 |
| Profiler.FlushMemoryCounters | 0.1872 | 0.1872 | 0.000 | 0.540 |
| UnityEngine.IMGUIModule.dll!UnityEngine::GUIStyle.GetMeshInfo() [Invoke] | 0.1788 | 0.1798 | 0.018 | 3.013 |
| UIR.DrawChain | 0.1739 | 6.1667 | 15.555 | 7.621 |
| AssetDatabase.GetGUIDFromAssetPathV2 | 0.1047 | 0.1047 | 0.000 | 0.274 |
| Inl_UniversalRenderPipeline.RenderSingleCameraInternal: Main Camera | 0.0864 | 0.7562 | 0.000 | 1.746 |
| Inl_NRPRGComp_ExecuteBeginRenderpassCommand | 0.0854 | 0.0854 | 0.000 | 0.839 |
| Inl_UniversalRenderPipeline.RenderSingleCameraInternal: SceneCamera | 0.0797 | 0.7447 | 0.000 | 1.567 |
| Inl_UniversalRenderTotal | 0.0795 | 2.2097 | 0.934 | 2.373 |
| RenderTreeManager.Process | 0.0717 | 0.1126 | 0.000 | 0.313 |

게임 코드 별도 보기(상위 inclusive 구간, 서로 중첩 가능):

| 함수/마커 | Inclusive ms/f | Self ms/f | Inclusive GC KiB/f | 평균 호출 us |
| --- | ---: | ---: | ---: | ---: |
| Assembly-CSharp.dll!::SoundManager.Update() [Invoke] | 0.0045 | 0.0045 | 0.000 | 4.50 |

실행 원본: `C:/Users/sdkmg/AppData/LocalLow/DefaultCompany/PotionJunkie\PerformanceDemo\20261015_062204_721_c05c6672`

분석 폴더: `C:\Users\sdkmg\Desktop\project\2d-team01\Logs/ProfilerReports/mcp_20261015_062156_604dfddd`
- repeat 1: Measure 기대/실제 2474/2474; `C:\Users\sdkmg\Desktop\project\2d-team01\Logs\ProfilerReports\mcp_20261015_062156_604dfddd\capture_001`

### S4_Combine_Burst_05

![프레임 추이](../Logs/GameLoopProfiling/S4_Combine_Burst_05.svg)

선택 프레임 702개 / 유효 raw 1개.

기능 판정: True. 결과: IntervalCompleted. 조합 5회, 적 생성 0개, 투척 0회.

피해 이벤트: 적 0, 포션 0, 플레이어 0. 적 사망/해제 0/0. 플레이어 이동 합계 0.00. 부하 유지용 고체력 사본: False.

CPU 상위 Self 비용(메인 스레드, inclusive 값 중첩 합산 금지):

| 마커 | Self ms/f | Inclusive ms/f | Inclusive GC KiB/f | 최대 단일 호출 ms |
| --- | ---: | ---: | ---: | ---: |
| OnGUI | 2.4209 | 5.7135 | 14.350 | 5.016 |
| GfxDeviceD3D12.WaitForLastPresentation.WaitForGPU | 1.0595 | 1.0595 | 0.000 | 15.357 |
| Inl_On Record Render Graph | 0.4232 | 0.4245 | 0.000 | 0.572 |
| IMGUIContainer | 0.3451 | 6.1074 | 15.801 | 6.680 |
| WaitForJobGroupID | 0.3310 | 0.5275 | 0.000 | 1.103 |
| Assembly-CSharp.dll!::VFXBurstField.Update() [Invoke] | 0.3152 | 0.3491 | 0.000 | 0.477 |
| Assembly-CSharp.dll!::PotionProjectile.Update() [Invoke] | 0.2522 | 0.5274 | 5.115 | 94.938 |
| Material.SetPassFast | 0.2080 | 0.2204 | 0.000 | 0.298 |
| UnityEngine.IMGUIModule.dll!UnityEngine::GUIStyle.GetMeshInfo() [Invoke] | 0.1909 | 0.1909 | 0.000 | 0.130 |
| PerfDemo.CombineCommit | 0.1901 | 0.2333 | 2.093 | 63.306 |
| Profiler.FlushMemoryCounters | 0.1841 | 0.1841 | 0.000 | 0.388 |
| UIR.DrawChain | 0.1712 | 6.3420 | 15.801 | 6.724 |
| Idle | 0.1447 | 0.1447 | 0.000 | 1.033 |
| AssetDatabase.GetGUIDFromAssetPathV2 | 0.1033 | 0.1033 | 0.000 | 0.317 |
| GizmoManager.DrawGizmos | 0.1026 | 0.2850 | 0.574 | 0.445 |

게임 코드 별도 보기(상위 inclusive 구간, 서로 중첩 가능):

| 함수/마커 | Inclusive ms/f | Self ms/f | Inclusive GC KiB/f | 평균 호출 us |
| --- | ---: | ---: | ---: | ---: |
| Assembly-CSharp.dll!::PotionProjectile.Update() [Invoke] | 0.5274 | 0.2522 | 5.115 | 923.28 |
| Assembly-CSharp.dll!::VFXBurstField.Update() [Invoke] | 0.3491 | 0.3152 | 0.000 | 77.80 |
| PerfDemo.CombineCommit | 0.2333 | 0.1901 | 2.093 | 32758.02 |
| Assembly-CSharp.dll!::PotionAreaVisual.Update() [Invoke] | 0.0338 | 0.0338 | 0.000 | 3.90 |
| Assembly-CSharp.dll!::PlayerMeleeModule.OnDrawGizmos() [Invoke] | 0.0241 | 0.0241 | 0.000 | 12.03 |
| Assembly-CSharp.dll!::PotionAreaEffect.Update() [Invoke] | 0.0174 | 0.0148 | 0.725 | 3.89 |
| Assembly-CSharp.dll!::PotionStatusVisual.LateUpdate() [Invoke] | 0.0163 | 0.0163 | 0.000 | 16.29 |
| Assembly-CSharp.dll!::SoundManager.Update() [Invoke] | 0.0151 | 0.0151 | 0.000 | 15.14 |
| Assembly-CSharp.dll!::StatusScreenVignette.Update() [Invoke] | 0.0144 | 0.0144 | 0.000 | 14.37 |
| Assembly-CSharp.dll!::PlayerAnimationDriver.Update() [Invoke] | 0.0059 | 0.0059 | 0.000 | 5.92 |

실행 원본: `C:/Users/sdkmg/AppData/LocalLow/DefaultCompany/PotionJunkie\PerformanceDemo\20261015_062308_836_ea116c0c`

분석 폴더: `C:\Users\sdkmg\Desktop\project\2d-team01\Logs/ProfilerReports/mcp_20261015_062259_19504a2d`
- repeat 1: Measure 기대/실제 703/703; `C:\Users\sdkmg\Desktop\project\2d-team01\Logs\ProfilerReports\mcp_20261015_062259_19504a2d\capture_001`

### S4_Combine_Burst_10

![프레임 추이](../Logs/GameLoopProfiling/S4_Combine_Burst_10.svg)

선택 프레임 595개 / 유효 raw 1개.

기능 판정: True. 결과: IntervalCompleted. 조합 10회, 적 생성 0개, 투척 0회.

피해 이벤트: 적 0, 포션 0, 플레이어 0. 적 사망/해제 0/0. 플레이어 이동 합계 0.00. 부하 유지용 고체력 사본: False.

CPU 상위 Self 비용(메인 스레드, inclusive 값 중첩 합산 금지):

| 마커 | Self ms/f | Inclusive ms/f | Inclusive GC KiB/f | 최대 단일 호출 ms |
| --- | ---: | ---: | ---: | ---: |
| OnGUI | 2.4065 | 5.9510 | 14.267 | 4.866 |
| GfxDeviceD3D12.WaitForLastPresentation.WaitForGPU | 1.1128 | 1.1128 | 0.000 | 36.024 |
| Assembly-CSharp.dll!::PotionProjectile.Update() [Invoke] | 0.7072 | 1.4388 | 13.710 | 149.470 |
| Assembly-CSharp.dll!::VFXBurstField.Update() [Invoke] | 0.5874 | 0.6619 | 0.000 | 0.475 |
| PerfDemo.CombineCommit | 0.5329 | 0.6347 | 5.554 | 72.745 |
| Inl_On Record Render Graph | 0.4270 | 0.4285 | 0.000 | 0.623 |
| WaitForJobGroupID | 0.3836 | 0.7132 | 0.000 | 1.538 |
| IMGUIContainer | 0.3533 | 6.3532 | 15.713 | 6.000 |
| Idle | 0.2450 | 0.2450 | 0.000 | 1.476 |
| Material.SetPassFast | 0.2120 | 0.2247 | 0.000 | 0.213 |
| Profiler.FlushMemoryCounters | 0.1987 | 0.1987 | 0.000 | 0.527 |
| UnityEngine.IMGUIModule.dll!UnityEngine::GUIStyle.GetMeshInfo() [Invoke] | 0.1751 | 0.1751 | 0.000 | 0.138 |
| UIR.DrawChain | 0.1731 | 6.5912 | 15.713 | 6.034 |
| Semaphore.WaitForSignal | 0.1490 | 0.1490 | 0.000 | 25.027 |
| GizmoManager.DrawGizmos | 0.1232 | 0.3119 | 0.574 | 0.503 |

게임 코드 별도 보기(상위 inclusive 구간, 서로 중첩 가능):

| 함수/마커 | Inclusive ms/f | Self ms/f | Inclusive GC KiB/f | 평균 호출 us |
| --- | ---: | ---: | ---: | ---: |
| Assembly-CSharp.dll!::PotionProjectile.Update() [Invoke] | 1.4388 | 0.7072 | 13.710 | 1262.68 |
| Assembly-CSharp.dll!::VFXBurstField.Update() [Invoke] | 0.6619 | 0.5874 | 0.000 | 78.99 |
| PerfDemo.CombineCommit | 0.6347 | 0.5329 | 5.554 | 37765.91 |
| Assembly-CSharp.dll!::PotionAreaVisual.Update() [Invoke] | 0.0558 | 0.0558 | 0.000 | 3.36 |
| Assembly-CSharp.dll!::PlayerMeleeModule.OnDrawGizmos() [Invoke] | 0.0250 | 0.0250 | 0.000 | 12.51 |
| Assembly-CSharp.dll!::PotionAreaEffect.Update() [Invoke] | 0.0245 | 0.0215 | 1.228 | 2.93 |
| Assembly-CSharp.dll!::SoundManager.Update() [Invoke] | 0.0175 | 0.0175 | 0.000 | 17.48 |
| Assembly-CSharp.dll!::PotionStatusVisual.LateUpdate() [Invoke] | 0.0172 | 0.0172 | 0.000 | 17.23 |
| Assembly-CSharp.dll!::StatusScreenVignette.Update() [Invoke] | 0.0143 | 0.0143 | 0.000 | 14.26 |
| Assembly-CSharp.dll!::PotionAreaController.Update() [Invoke] | 0.0066 | 0.0066 | 0.000 | 0.40 |

실행 원본: `C:/Users/sdkmg/AppData/LocalLow/DefaultCompany/PotionJunkie\PerformanceDemo\20261015_062341_324_981cb9be`

분석 폴더: `C:\Users\sdkmg\Desktop\project\2d-team01\Logs/ProfilerReports/mcp_20261015_062332_835ffa10`
- repeat 1: Measure 기대/실제 596/596; `C:\Users\sdkmg\Desktop\project\2d-team01\Logs\ProfilerReports\mcp_20261015_062332_835ffa10\capture_001`

### S4_Combine_ComplexCorner

![프레임 추이](../Logs/GameLoopProfiling/S4_Combine_ComplexCorner.svg)

선택 프레임 770개 / 유효 raw 1개.

기능 판정: True. 결과: IntervalCompleted. 조합 1회, 적 생성 0개, 투척 0회.

피해 이벤트: 적 0, 포션 0, 플레이어 0. 적 사망/해제 0/0. 플레이어 이동 합계 0.00. 부하 유지용 고체력 사본: False.

CPU 상위 Self 비용(메인 스레드, inclusive 값 중첩 합산 금지):

| 마커 | Self ms/f | Inclusive ms/f | Inclusive GC KiB/f | 최대 단일 호출 ms |
| --- | ---: | ---: | ---: | ---: |
| OnGUI | 2.3336 | 5.4309 | 14.188 | 3.638 |
| GfxDeviceD3D12.WaitForLastPresentation.WaitForGPU | 1.1725 | 1.1725 | 0.000 | 62.535 |
| Inl_On Record Render Graph | 0.4193 | 0.4208 | 0.000 | 0.597 |
| IMGUIContainer | 0.3435 | 5.8220 | 15.630 | 4.216 |
| WaitForJobGroupID | 0.3293 | 0.3629 | 0.000 | 0.363 |
| Material.SetPassFast | 0.2030 | 0.2154 | 0.000 | 0.195 |
| Profiler.FlushMemoryCounters | 0.1904 | 0.1904 | 0.000 | 0.475 |
| UnityEngine.IMGUIModule.dll!UnityEngine::GUIStyle.GetMeshInfo() [Invoke] | 0.1758 | 0.1758 | 0.000 | 0.186 |
| UIR.DrawChain | 0.1705 | 6.0552 | 15.630 | 4.826 |
| AssetDatabase.GetGUIDFromAssetPathV2 | 0.0986 | 0.0986 | 0.000 | 0.136 |
| GizmoManager.DrawGizmos | 0.0892 | 0.2643 | 0.574 | 0.408 |
| Inl_UniversalRenderPipeline.RenderSingleCameraInternal: SceneCamera | 0.0857 | 0.7289 | 0.000 | 1.423 |
| Inl_UniversalRenderPipeline.RenderSingleCameraInternal: Main Camera | 0.0839 | 0.7414 | 0.000 | 1.207 |
| Assembly-CSharp.dll!::PotionProjectile.Update() [Invoke] | 0.0836 | 0.1599 | 1.792 | 96.394 |
| Inl_NRPRGComp_ExecuteBeginRenderpassCommand | 0.0786 | 0.0786 | 0.000 | 0.140 |

게임 코드 별도 보기(상위 inclusive 구간, 서로 중첩 가능):

| 함수/마커 | Inclusive ms/f | Self ms/f | Inclusive GC KiB/f | 평균 호출 us |
| --- | ---: | ---: | ---: | ---: |
| Assembly-CSharp.dll!::PotionProjectile.Update() [Invoke] | 0.1599 | 0.0836 | 1.792 | 843.24 |
| Assembly-CSharp.dll!::VFXBurstField.Update() [Invoke] | 0.0876 | 0.0768 | 0.000 | 103.88 |
| PerfDemo.CombineCommit | 0.0648 | 0.0564 | 0.568 | 49870.80 |
| Assembly-CSharp.dll!::PlayerMeleeModule.OnDrawGizmos() [Invoke] | 0.0241 | 0.0241 | 0.000 | 12.04 |
| Assembly-CSharp.dll!::PotionAreaVisual.Update() [Invoke] | 0.0174 | 0.0174 | 0.000 | 10.48 |
| Assembly-CSharp.dll!::PotionStatusVisual.LateUpdate() [Invoke] | 0.0155 | 0.0155 | 0.000 | 15.49 |
| Assembly-CSharp.dll!::StatusScreenVignette.Update() [Invoke] | 0.0146 | 0.0146 | 0.000 | 14.60 |
| Assembly-CSharp.dll!::SoundManager.Update() [Invoke] | 0.0097 | 0.0097 | 0.000 | 9.68 |
| Assembly-CSharp.dll!::PotionAreaEffect.Update() [Invoke] | 0.0071 | 0.0065 | 0.125 | 8.42 |
| Assembly-CSharp.dll!::PlayerAnimationDriver.Update() [Invoke] | 0.0066 | 0.0066 | 0.000 | 6.57 |

실행 원본: `C:/Users/sdkmg/AppData/LocalLow/DefaultCompany/PotionJunkie\PerformanceDemo\20261015_062414_609_fc05476d`

분석 폴더: `C:\Users\sdkmg\Desktop\project\2d-team01\Logs/ProfilerReports/mcp_20261015_062405_cd167a9f`
- repeat 1: Measure 기대/실제 771/771; `C:\Users\sdkmg\Desktop\project\2d-team01\Logs\ProfilerReports\mcp_20261015_062405_cd167a9f\capture_001`

### S4_Combine_NoOverlap

![프레임 추이](../Logs/GameLoopProfiling/S4_Combine_NoOverlap.svg)

선택 프레임 874개 / 유효 raw 1개.

기능 판정: True. 결과: IntervalCompleted. 조합 0회, 적 생성 0개, 투척 0회.

피해 이벤트: 적 0, 포션 0, 플레이어 0. 적 사망/해제 0/0. 플레이어 이동 합계 0.00. 부하 유지용 고체력 사본: False.

CPU 상위 Self 비용(메인 스레드, inclusive 값 중첩 합산 금지):

| 마커 | Self ms/f | Inclusive ms/f | Inclusive GC KiB/f | 최대 단일 호출 ms |
| --- | ---: | ---: | ---: | ---: |
| OnGUI | 2.3217 | 5.3908 | 14.192 | 3.903 |
| GfxDeviceD3D12.WaitForLastPresentation.WaitForGPU | 0.4348 | 0.4348 | 0.000 | 11.750 |
| Inl_On Record Render Graph | 0.4185 | 0.4198 | 0.000 | 0.624 |
| IMGUIContainer | 0.3442 | 5.7826 | 15.635 | 4.595 |
| WaitForJobGroupID | 0.3208 | 0.3521 | 0.000 | 0.344 |
| Material.SetPassFast | 0.2052 | 0.2177 | 0.000 | 0.373 |
| Profiler.FlushMemoryCounters | 0.1832 | 0.1832 | 0.000 | 0.509 |
| UnityEngine.IMGUIModule.dll!UnityEngine::GUIStyle.GetMeshInfo() [Invoke] | 0.1740 | 0.1740 | 0.000 | 0.189 |
| UIR.DrawChain | 0.1687 | 6.0129 | 15.635 | 5.114 |
| AssetDatabase.GetGUIDFromAssetPathV2 | 0.0991 | 0.0991 | 0.000 | 0.090 |
| GizmoManager.DrawGizmos | 0.0853 | 0.2597 | 0.574 | 0.668 |
| Inl_UniversalRenderPipeline.RenderSingleCameraInternal: Main Camera | 0.0832 | 0.7382 | 0.000 | 1.391 |
| Inl_UniversalRenderPipeline.RenderSingleCameraInternal: SceneCamera | 0.0825 | 0.7134 | 0.000 | 1.302 |
| Inl_NRPRGComp_ExecuteBeginRenderpassCommand | 0.0806 | 0.0806 | 0.000 | 0.200 |
| Inl_UniversalRenderTotal | 0.0765 | 2.2041 | 0.934 | 1.869 |

게임 코드 별도 보기(상위 inclusive 구간, 서로 중첩 가능):

| 함수/마커 | Inclusive ms/f | Self ms/f | Inclusive GC KiB/f | 평균 호출 us |
| --- | ---: | ---: | ---: | ---: |
| Assembly-CSharp.dll!::VFXBurstField.Update() [Invoke] | 0.0540 | 0.0465 | 0.000 | 64.92 |
| Assembly-CSharp.dll!::PlayerMeleeModule.OnDrawGizmos() [Invoke] | 0.0236 | 0.0236 | 0.000 | 11.79 |
| Assembly-CSharp.dll!::PotionAreaVisual.Update() [Invoke] | 0.0211 | 0.0211 | 0.000 | 12.68 |
| Assembly-CSharp.dll!::PotionStatusVisual.LateUpdate() [Invoke] | 0.0153 | 0.0153 | 0.000 | 15.31 |
| Assembly-CSharp.dll!::StatusScreenVignette.Update() [Invoke] | 0.0137 | 0.0137 | 0.000 | 13.67 |
| Assembly-CSharp.dll!::PotionProjectile.Update() [Invoke] | 0.0126 | 0.0051 | 0.303 | 68.09 |
| Assembly-CSharp.dll!::SoundManager.Update() [Invoke] | 0.0089 | 0.0089 | 0.000 | 8.91 |
| Assembly-CSharp.dll!::PotionAreaEffect.Update() [Invoke] | 0.0080 | 0.0078 | 0.234 | 4.83 |
| Assembly-CSharp.dll!::PotionEffectReceiver.Update() [Invoke] | 0.0079 | 0.0077 | 0.125 | 7.86 |
| Assembly-CSharp.dll!::PlayerAnimationDriver.Update() [Invoke] | 0.0064 | 0.0064 | 0.000 | 6.40 |

실행 원본: `C:/Users/sdkmg/AppData/LocalLow/DefaultCompany/PotionJunkie\PerformanceDemo\20261015_062448_764_0404fcbd`

분석 폴더: `C:\Users\sdkmg\Desktop\project\2d-team01\Logs/ProfilerReports/mcp_20261015_062440_2a605dad`
- repeat 1: Measure 기대/실제 875/875; `C:\Users\sdkmg\Desktop\project\2d-team01\Logs\ProfilerReports\mcp_20261015_062440_2a605dad\capture_001`

### S4_Combine_Pair

![프레임 추이](../Logs/GameLoopProfiling/S4_Combine_Pair.svg)

선택 프레임 816개 / 유효 raw 1개.

기능 판정: True. 결과: IntervalCompleted. 조합 1회, 적 생성 0개, 투척 0회.

피해 이벤트: 적 0, 포션 0, 플레이어 0. 적 사망/해제 0/0. 플레이어 이동 합계 0.00. 부하 유지용 고체력 사본: False.

CPU 상위 Self 비용(메인 스레드, inclusive 값 중첩 합산 금지):

| 마커 | Self ms/f | Inclusive ms/f | Inclusive GC KiB/f | 최대 단일 호출 ms |
| --- | ---: | ---: | ---: | ---: |
| OnGUI | 2.3293 | 5.4519 | 14.234 | 3.784 |
| GfxDeviceD3D12.WaitForLastPresentation.WaitForGPU | 0.8448 | 0.8448 | 0.000 | 16.739 |
| Inl_On Record Render Graph | 0.4260 | 0.4274 | 0.000 | 0.610 |
| IMGUIContainer | 0.3438 | 5.8439 | 15.679 | 4.394 |
| WaitForJobGroupID | 0.3386 | 0.3733 | 0.000 | 0.445 |
| Material.SetPassFast | 0.2036 | 0.2160 | 0.000 | 0.515 |
| Profiler.FlushMemoryCounters | 0.1882 | 0.1882 | 0.000 | 0.512 |
| UnityEngine.IMGUIModule.dll!UnityEngine::GUIStyle.GetMeshInfo() [Invoke] | 0.1774 | 0.1774 | 0.000 | 0.269 |
| UIR.DrawChain | 0.1679 | 6.0736 | 15.679 | 4.492 |
| AssetDatabase.GetGUIDFromAssetPathV2 | 0.0989 | 0.0989 | 0.000 | 0.136 |
| GizmoManager.DrawGizmos | 0.0902 | 0.2685 | 0.574 | 0.540 |
| Inl_UniversalRenderPipeline.RenderSingleCameraInternal: SceneCamera | 0.0839 | 0.7314 | 0.000 | 1.381 |
| Inl_UniversalRenderPipeline.RenderSingleCameraInternal: Main Camera | 0.0824 | 0.7505 | 0.000 | 1.729 |
| Inl_NRPRGComp_ExecuteBeginRenderpassCommand | 0.0796 | 0.0796 | 0.000 | 0.193 |
| Inl_UniversalRenderTotal | 0.0778 | 2.2493 | 0.934 | 2.373 |

게임 코드 별도 보기(상위 inclusive 구간, 서로 중첩 가능):

| 함수/마커 | Inclusive ms/f | Self ms/f | Inclusive GC KiB/f | 평균 호출 us |
| --- | ---: | ---: | ---: | ---: |
| Assembly-CSharp.dll!::VFXBurstField.Update() [Invoke] | 0.0806 | 0.0710 | 0.000 | 90.77 |
| Assembly-CSharp.dll!::PotionProjectile.Update() [Invoke] | 0.0246 | 0.0058 | 0.347 | 135.82 |
| Assembly-CSharp.dll!::PlayerMeleeModule.OnDrawGizmos() [Invoke] | 0.0240 | 0.0240 | 0.000 | 11.99 |
| Assembly-CSharp.dll!::PotionAreaVisual.Update() [Invoke] | 0.0166 | 0.0166 | 0.000 | 9.99 |
| Assembly-CSharp.dll!::PotionStatusVisual.LateUpdate() [Invoke] | 0.0157 | 0.0157 | 0.000 | 15.67 |
| Assembly-CSharp.dll!::StatusScreenVignette.Update() [Invoke] | 0.0148 | 0.0148 | 0.000 | 14.82 |
| PerfDemo.CombineCommit | 0.0100 | 0.0030 | 0.024 | 8152.30 |
| Assembly-CSharp.dll!::SoundManager.Update() [Invoke] | 0.0091 | 0.0091 | 0.000 | 9.11 |
| Assembly-CSharp.dll!::PotionAreaEffect.Update() [Invoke] | 0.0073 | 0.0069 | 0.160 | 8.25 |
| Assembly-CSharp.dll!::PlayerAnimationDriver.Update() [Invoke] | 0.0065 | 0.0065 | 0.000 | 6.45 |

실행 원본: `C:/Users/sdkmg/AppData/LocalLow/DefaultCompany/PotionJunkie\PerformanceDemo\20261015_062523_728_98dd4c37`

분석 폴더: `C:\Users\sdkmg\Desktop\project\2d-team01\Logs/ProfilerReports/mcp_20261015_062514_cd1e7893`
- repeat 1: Measure 기대/실제 817/817; `C:\Users\sdkmg\Desktop\project\2d-team01\Logs\ProfilerReports\mcp_20261015_062514_cd1e7893\capture_001`

### S4_Combine_SimpleWall

![프레임 추이](../Logs/GameLoopProfiling/S4_Combine_SimpleWall.svg)

선택 프레임 787개 / 유효 raw 1개.

기능 판정: True. 결과: IntervalCompleted. 조합 1회, 적 생성 0개, 투척 0회.

피해 이벤트: 적 0, 포션 0, 플레이어 0. 적 사망/해제 0/0. 플레이어 이동 합계 0.00. 부하 유지용 고체력 사본: False.

CPU 상위 Self 비용(메인 스레드, inclusive 값 중첩 합산 금지):

| 마커 | Self ms/f | Inclusive ms/f | Inclusive GC KiB/f | 최대 단일 호출 ms |
| --- | ---: | ---: | ---: | ---: |
| OnGUI | 2.3318 | 5.4282 | 14.203 | 3.817 |
| GfxDeviceD3D12.WaitForLastPresentation.WaitForGPU | 1.0534 | 1.0534 | 0.000 | 16.277 |
| Inl_On Record Render Graph | 0.4212 | 0.4228 | 0.000 | 0.610 |
| IMGUIContainer | 0.3425 | 5.8202 | 15.646 | 4.548 |
| WaitForJobGroupID | 0.3233 | 0.3598 | 0.000 | 0.372 |
| Material.SetPassFast | 0.2051 | 0.2176 | 0.000 | 0.247 |
| Profiler.FlushMemoryCounters | 0.1894 | 0.1894 | 0.000 | 0.583 |
| UnityEngine.IMGUIModule.dll!UnityEngine::GUIStyle.GetMeshInfo() [Invoke] | 0.1786 | 0.1786 | 0.000 | 0.301 |
| UIR.DrawChain | 0.1676 | 6.0506 | 15.646 | 4.579 |
| AssetDatabase.GetGUIDFromAssetPathV2 | 0.1012 | 0.1012 | 0.000 | 0.226 |
| GizmoManager.DrawGizmos | 0.0878 | 0.2626 | 0.574 | 0.465 |
| Assembly-CSharp.dll!::PotionProjectile.Update() [Invoke] | 0.0853 | 0.1577 | 1.735 | 97.114 |
| Inl_UniversalRenderPipeline.RenderSingleCameraInternal: SceneCamera | 0.0845 | 0.7184 | 0.000 | 1.350 |
| Inl_UniversalRenderPipeline.RenderSingleCameraInternal: Main Camera | 0.0838 | 0.7472 | 0.000 | 1.243 |
| Inl_NRPRGComp_ExecuteBeginRenderpassCommand | 0.0803 | 0.0803 | 0.000 | 0.242 |

게임 코드 별도 보기(상위 inclusive 구간, 서로 중첩 가능):

| 함수/마커 | Inclusive ms/f | Self ms/f | Inclusive GC KiB/f | 평균 호출 us |
| --- | ---: | ---: | ---: | ---: |
| Assembly-CSharp.dll!::PotionProjectile.Update() [Invoke] | 0.1577 | 0.0853 | 1.735 | 886.30 |
| Assembly-CSharp.dll!::VFXBurstField.Update() [Invoke] | 0.0882 | 0.0777 | 0.000 | 99.40 |
| PerfDemo.CombineCommit | 0.0607 | 0.0525 | 0.548 | 47773.20 |
| Assembly-CSharp.dll!::PlayerMeleeModule.OnDrawGizmos() [Invoke] | 0.0236 | 0.0236 | 0.000 | 11.78 |
| Assembly-CSharp.dll!::PotionAreaVisual.Update() [Invoke] | 0.0165 | 0.0165 | 0.000 | 9.79 |
| Assembly-CSharp.dll!::PotionStatusVisual.LateUpdate() [Invoke] | 0.0153 | 0.0153 | 0.000 | 15.25 |
| Assembly-CSharp.dll!::StatusScreenVignette.Update() [Invoke] | 0.0148 | 0.0148 | 0.000 | 14.80 |
| Assembly-CSharp.dll!::PotionAreaEffect.Update() [Invoke] | 0.0104 | 0.0094 | 0.153 | 11.72 |
| Assembly-CSharp.dll!::SoundManager.Update() [Invoke] | 0.0099 | 0.0099 | 0.000 | 9.88 |
| Assembly-CSharp.dll!::PlayerAnimationDriver.Update() [Invoke] | 0.0062 | 0.0062 | 0.000 | 6.21 |

실행 원본: `C:/Users/sdkmg/AppData/LocalLow/DefaultCompany/PotionJunkie\PerformanceDemo\20261015_062558_581_d8c91026`

분석 폴더: `C:\Users\sdkmg\Desktop\project\2d-team01\Logs/ProfilerReports/mcp_20261015_062549_a8d96db8`
- repeat 1: Measure 기대/실제 788/788; `C:\Users\sdkmg\Desktop\project\2d-team01\Logs\ProfilerReports\mcp_20261015_062549_a8d96db8\capture_001`

### S4_Combine_Staggered_05

![프레임 추이](../Logs/GameLoopProfiling/S4_Combine_Staggered_05.svg)

선택 프레임 926개 / 유효 raw 1개.

기능 판정: True. 결과: IntervalCompleted. 조합 5회, 적 생성 0개, 투척 0회.

피해 이벤트: 적 0, 포션 0, 플레이어 0. 적 사망/해제 0/0. 플레이어 이동 합계 0.00. 부하 유지용 고체력 사본: False.

CPU 상위 Self 비용(메인 스레드, inclusive 값 중첩 합산 금지):

| 마커 | Self ms/f | Inclusive ms/f | Inclusive GC KiB/f | 최대 단일 호출 ms |
| --- | ---: | ---: | ---: | ---: |
| OnGUI | 2.4957 | 5.8542 | 14.445 | 6.754 |
| GfxDeviceD3D12.WaitForLastPresentation.WaitForGPU | 0.6807 | 0.6807 | 0.000 | 25.807 |
| Inl_On Record Render Graph | 0.4399 | 0.4415 | 0.000 | 0.787 |
| IMGUIContainer | 0.3638 | 6.2704 | 15.900 | 8.144 |
| WaitForJobGroupID | 0.3319 | 0.4867 | 0.000 | 0.810 |
| Assembly-CSharp.dll!::VFXBurstField.Update() [Invoke] | 0.2608 | 0.2903 | 0.000 | 0.509 |
| Assembly-CSharp.dll!::PotionProjectile.Update() [Invoke] | 0.2360 | 0.5044 | 3.864 | 113.016 |
| Material.SetPassFast | 0.2206 | 0.2338 | 0.000 | 0.233 |
| Profiler.FlushMemoryCounters | 0.1963 | 0.1963 | 0.000 | 0.770 |
| UnityEngine.IMGUIModule.dll!UnityEngine::GUIStyle.GetMeshInfo() [Invoke] | 0.1858 | 0.1858 | 0.000 | 0.679 |
| PerfDemo.CombineCommit | 0.1810 | 0.2257 | 1.572 | 58.263 |
| UIR.DrawChain | 0.1758 | 6.5126 | 15.900 | 8.212 |
| AssetDatabase.GetGUIDFromAssetPathV2 | 0.1072 | 0.1072 | 0.000 | 0.204 |
| Idle | 0.1064 | 0.1064 | 0.000 | 0.742 |
| GizmoManager.DrawGizmos | 0.1039 | 0.2943 | 0.574 | 0.724 |

게임 코드 별도 보기(상위 inclusive 구간, 서로 중첩 가능):

| 함수/마커 | Inclusive ms/f | Self ms/f | Inclusive GC KiB/f | 평균 호출 us |
| --- | ---: | ---: | ---: | ---: |
| Assembly-CSharp.dll!::PotionProjectile.Update() [Invoke] | 0.5044 | 0.2360 | 3.864 | 1286.63 |
| Assembly-CSharp.dll!::VFXBurstField.Update() [Invoke] | 0.2903 | 0.2608 | 0.000 | 84.23 |
| PerfDemo.CombineCommit | 0.2257 | 0.1810 | 1.572 | 41792.04 |
| Assembly-CSharp.dll!::PotionAreaVisual.Update() [Invoke] | 0.0291 | 0.0291 | 0.000 | 4.36 |
| Assembly-CSharp.dll!::PlayerMeleeModule.OnDrawGizmos() [Invoke] | 0.0253 | 0.0253 | 0.000 | 12.67 |
| Assembly-CSharp.dll!::PotionAreaEffect.Update() [Invoke] | 0.0183 | 0.0140 | 0.555 | 5.30 |
| Assembly-CSharp.dll!::PotionStatusVisual.LateUpdate() [Invoke] | 0.0172 | 0.0172 | 0.000 | 17.17 |
| Assembly-CSharp.dll!::StatusScreenVignette.Update() [Invoke] | 0.0153 | 0.0153 | 0.000 | 15.31 |
| Assembly-CSharp.dll!::SoundManager.Update() [Invoke] | 0.0140 | 0.0140 | 0.000 | 14.00 |
| Assembly-CSharp.dll!::PlayerAnimationDriver.Update() [Invoke] | 0.0068 | 0.0068 | 0.000 | 6.76 |

실행 원본: `C:/Users/sdkmg/AppData/LocalLow/DefaultCompany/PotionJunkie\PerformanceDemo\20261015_062633_447_324afad1`

분석 폴더: `C:\Users\sdkmg\Desktop\project\2d-team01\Logs/ProfilerReports/mcp_20261015_062624_7ae626f3`
- repeat 1: Measure 기대/실제 927/927; `C:\Users\sdkmg\Desktop\project\2d-team01\Logs\ProfilerReports\mcp_20261015_062624_7ae626f3\capture_001`

### S4_Combine_Staggered_10

![프레임 추이](../Logs/GameLoopProfiling/S4_Combine_Staggered_10.svg)

선택 프레임 884개 / 유효 raw 1개.

기능 판정: True. 결과: IntervalCompleted. 조합 10회, 적 생성 0개, 투척 0회.

피해 이벤트: 적 0, 포션 0, 플레이어 0. 적 사망/해제 0/0. 플레이어 이동 합계 0.00. 부하 유지용 고체력 사본: False.

CPU 상위 Self 비용(메인 스레드, inclusive 값 중첩 합산 금지):

| 마커 | Self ms/f | Inclusive ms/f | Inclusive GC KiB/f | 최대 단일 호출 ms |
| --- | ---: | ---: | ---: | ---: |
| OnGUI | 2.3448 | 5.6254 | 14.726 | 3.968 |
| GfxDeviceD3D12.WaitForLastPresentation.WaitForGPU | 0.7242 | 0.7242 | 0.000 | 45.681 |
| Assembly-CSharp.dll!::PotionProjectile.Update() [Invoke] | 0.4881 | 0.9729 | 9.215 | 149.081 |
| Assembly-CSharp.dll!::VFXBurstField.Update() [Invoke] | 0.4461 | 0.4953 | 0.000 | 0.361 |
| Inl_On Record Render Graph | 0.4130 | 0.4143 | 0.000 | 0.506 |
| WaitForJobGroupID | 0.3757 | 0.6160 | 0.000 | 1.342 |
| PerfDemo.CombineCommit | 0.3511 | 0.4188 | 3.723 | 71.136 |
| IMGUIContainer | 0.3430 | 6.0152 | 16.195 | 4.511 |
| Material.SetPassFast | 0.2064 | 0.2184 | 0.000 | 0.163 |
| Profiler.FlushMemoryCounters | 0.1851 | 0.1851 | 0.000 | 0.418 |
| UnityEngine.IMGUIModule.dll!UnityEngine::GUIStyle.GetMeshInfo() [Invoke] | 0.1754 | 0.1754 | 0.000 | 0.182 |
| Idle | 0.1747 | 0.1747 | 0.000 | 0.933 |
| UIR.DrawChain | 0.1646 | 6.2412 | 16.195 | 4.539 |
| AssetDatabase.GetGUIDFromAssetPathV2 | 0.1029 | 0.1029 | 0.000 | 0.657 |
| GizmoManager.DrawGizmos | 0.1029 | 0.2830 | 0.574 | 0.388 |

게임 코드 별도 보기(상위 inclusive 구간, 서로 중첩 가능):

| 함수/마커 | Inclusive ms/f | Self ms/f | Inclusive GC KiB/f | 평균 호출 us |
| --- | ---: | ---: | ---: | ---: |
| Assembly-CSharp.dll!::PotionProjectile.Update() [Invoke] | 0.9729 | 0.4881 | 9.215 | 977.33 |
| Assembly-CSharp.dll!::VFXBurstField.Update() [Invoke] | 0.4953 | 0.4461 | 0.000 | 72.55 |
| PerfDemo.CombineCommit | 0.4188 | 0.3511 | 3.723 | 37021.79 |
| Assembly-CSharp.dll!::PotionAreaVisual.Update() [Invoke] | 0.0442 | 0.0442 | 0.000 | 3.38 |
| Assembly-CSharp.dll!::PotionAreaEffect.Update() [Invoke] | 0.0297 | 0.0227 | 1.140 | 4.36 |
| Assembly-CSharp.dll!::PlayerMeleeModule.OnDrawGizmos() [Invoke] | 0.0230 | 0.0230 | 0.000 | 11.50 |
| Assembly-CSharp.dll!::PotionStatusVisual.LateUpdate() [Invoke] | 0.0160 | 0.0160 | 0.000 | 15.97 |
| Assembly-CSharp.dll!::SoundManager.Update() [Invoke] | 0.0142 | 0.0142 | 0.000 | 14.25 |
| Assembly-CSharp.dll!::StatusScreenVignette.Update() [Invoke] | 0.0136 | 0.0136 | 0.000 | 13.60 |
| Assembly-CSharp.dll!::PlayerAnimationDriver.Update() [Invoke] | 0.0061 | 0.0061 | 0.000 | 6.13 |

실행 원본: `C:/Users/sdkmg/AppData/LocalLow/DefaultCompany/PotionJunkie\PerformanceDemo\20261015_062710_194_d6435d66`

분석 폴더: `C:\Users\sdkmg\Desktop\project\2d-team01\Logs/ProfilerReports/mcp_20261015_062701_ad2deb0f`
- repeat 1: Measure 기대/실제 885/885; `C:\Users\sdkmg\Desktop\project\2d-team01\Logs\ProfilerReports\mcp_20261015_062701_ad2deb0f\capture_001`

### S6_Chase_Moving_10

![프레임 추이](../Logs/GameLoopProfiling/S6_Chase_Moving_10.svg)

선택 프레임 1,522개 / 유효 raw 1개.

기능 판정: True. 결과: IntervalCompleted. 조합 0회, 적 생성 10개, 투척 0회.

피해 이벤트: 적 0, 포션 0, 플레이어 67. 적 사망/해제 0/0. 플레이어 이동 합계 12.50. 부하 유지용 고체력 사본: True.

CPU 상위 Self 비용(메인 스레드, inclusive 값 중첩 합산 금지):

| 마커 | Self ms/f | Inclusive ms/f | Inclusive GC KiB/f | 최대 단일 호출 ms |
| --- | ---: | ---: | ---: | ---: |
| OnGUI | 2.6636 | 5.9382 | 15.807 | 3.962 |
| WaitForJobGroupID | 0.6831 | 0.7779 | 0.000 | 0.571 |
| GfxDeviceD3D12.WaitForLastPresentation.WaitForGPU | 0.6203 | 0.6203 | 0.000 | 12.915 |
| Inl_On Record Render Graph | 0.4327 | 0.4342 | 0.000 | 0.488 |
| IMGUIContainer | 0.3779 | 6.3668 | 17.331 | 4.677 |
| Material.SetPassFast | 0.2244 | 0.2378 | 0.000 | 0.154 |
| UnityEngine.IMGUIModule.dll!UnityEngine::GUIStyle.GetMeshInfo() [Invoke] | 0.2122 | 0.2122 | 0.000 | 0.240 |
| Profiler.FlushMemoryCounters | 0.1891 | 0.1891 | 0.000 | 0.554 |
| UIR.DrawChain | 0.1739 | 6.6074 | 17.331 | 4.718 |
| GizmoManager.DrawGizmos | 0.1109 | 0.3152 | 0.574 | 0.488 |
| AssetDatabase.GetGUIDFromAssetPathV2 | 0.1063 | 0.1063 | 0.000 | 0.172 |
| Assembly-CSharp.dll!::PotionStatusVisual.LateUpdate() [Invoke] | 0.1049 | 0.1049 | 0.000 | 0.206 |
| Semaphore.WaitForSignal | 0.0896 | 0.0896 | 0.000 | 9.530 |
| Inl_UniversalRenderPipeline.RenderSingleCameraInternal: SceneCamera | 0.0876 | 0.7204 | 0.000 | 1.161 |
| Inl_UniversalRenderPipeline.RenderSingleCameraInternal: Main Camera | 0.0858 | 0.8086 | 0.000 | 1.369 |

게임 코드 별도 보기(상위 inclusive 구간, 서로 중첩 가능):

| 함수/마커 | Inclusive ms/f | Self ms/f | Inclusive GC KiB/f | 평균 호출 us |
| --- | ---: | ---: | ---: | ---: |
| Assembly-CSharp.dll!::PotionStatusVisual.LateUpdate() [Invoke] | 0.1049 | 0.1049 | 0.000 | 9.54 |
| Assembly-CSharp.dll!::RangedEnemyController.FixedUpdate() [Invoke] | 0.1033 | 0.0555 | 0.086 | 24.92 |
| Assembly-CSharp.dll!::PlayerMeleeModule.OnDrawGizmos() [Invoke] | 0.0245 | 0.0245 | 0.000 | 12.27 |
| Assembly-CSharp.dll!::PlayerMovementModule.FixedUpdate() [Invoke] | 0.0188 | 0.0157 | 0.000 | 45.38 |
| Assembly-CSharp.dll!::StatusScreenVignette.Update() [Invoke] | 0.0152 | 0.0152 | 0.000 | 15.18 |
| Assembly-CSharp.dll!::PotionEffectReceiver.Update() [Invoke] | 0.0139 | 0.0132 | 1.375 | 1.26 |
| Assembly-CSharp.dll!::EnemyProjectile.OnTriggerEnter2D() [Invoke] | 0.0137 | 0.0048 | 0.015 | 52.91 |
| Assembly-CSharp.dll!::EnemyProjectile.FixedUpdate() [Invoke] | 0.0136 | 0.0091 | 0.000 | 4.35 |
| Assembly-CSharp.dll!::SoundManager.Update() [Invoke] | 0.0092 | 0.0092 | 0.000 | 9.16 |
| Assembly-CSharp.dll!::EnemyAnimationDriver.FixedUpdate() [Invoke] | 0.0090 | 0.0090 | 0.000 | 2.18 |

실행 원본: `C:/Users/sdkmg/AppData/LocalLow/DefaultCompany/PotionJunkie\PerformanceDemo\20261015_062747_091_37d8eb46`

분석 폴더: `C:\Users\sdkmg\Desktop\project\2d-team01\Logs/ProfilerReports/mcp_20261015_062738_3fff3d94`
- repeat 1: Measure 기대/실제 1523/1523; `C:\Users\sdkmg\Desktop\project\2d-team01\Logs\ProfilerReports\mcp_20261015_062738_3fff3d94\capture_001`

### S6_Chase_Moving_30

![프레임 추이](../Logs/GameLoopProfiling/S6_Chase_Moving_30.svg)

선택 프레임 1,309개 / 유효 raw 1개.

기능 판정: True. 결과: IntervalCompleted. 조합 0회, 적 생성 30개, 투척 0회.

피해 이벤트: 적 0, 포션 0, 플레이어 163. 적 사망/해제 0/0. 플레이어 이동 합계 11.98. 부하 유지용 고체력 사본: True.

CPU 상위 Self 비용(메인 스레드, inclusive 값 중첩 합산 금지):

| 마커 | Self ms/f | Inclusive ms/f | Inclusive GC KiB/f | 최대 단일 호출 ms |
| --- | ---: | ---: | ---: | ---: |
| OnGUI | 2.9794 | 6.6173 | 16.951 | 4.167 |
| WaitForJobGroupID | 0.8168 | 0.9605 | 0.000 | 0.517 |
| Inl_On Record Render Graph | 0.4461 | 0.4477 | 0.000 | 0.521 |
| IMGUIContainer | 0.4014 | 7.0735 | 18.534 | 5.045 |
| GfxDeviceD3D12.WaitForLastPresentation.WaitForGPU | 0.3918 | 0.3918 | 0.000 | 14.135 |
| Material.SetPassFast | 0.2541 | 0.2692 | 0.000 | 0.211 |
| UnityEngine.IMGUIModule.dll!UnityEngine::GUIStyle.GetMeshInfo() [Invoke] | 0.2399 | 0.2399 | 0.000 | 0.218 |
| Assembly-CSharp.dll!::RangedEnemyController.FixedUpdate() [Invoke] | 0.2101 | 0.4273 | 0.358 | 3.461 |
| Assembly-CSharp.dll!::PotionStatusVisual.LateUpdate() [Invoke] | 0.2052 | 0.2052 | 0.000 | 0.229 |
| Profiler.FlushMemoryCounters | 0.1962 | 0.1962 | 0.000 | 0.515 |
| UIR.DrawChain | 0.1850 | 7.3313 | 18.534 | 5.448 |
| GizmoManager.DrawGizmos | 0.1624 | 0.4695 | 0.574 | 0.898 |
| Physics2D.OverlapCircleAll | 0.1479 | 0.1479 | 0.000 | 0.200 |
| AssetDatabase.GetGUIDFromAssetPathV2 | 0.1146 | 0.1146 | 0.000 | 0.143 |
| Semaphore.WaitForSignal | 0.1100 | 0.1100 | 0.000 | 10.078 |

게임 코드 별도 보기(상위 inclusive 구간, 서로 중첩 가능):

| 함수/마커 | Inclusive ms/f | Self ms/f | Inclusive GC KiB/f | 평균 호출 us |
| --- | ---: | ---: | ---: | ---: |
| Assembly-CSharp.dll!::RangedEnemyController.FixedUpdate() [Invoke] | 0.4273 | 0.2101 | 0.358 | 31.13 |
| Assembly-CSharp.dll!::PotionStatusVisual.LateUpdate() [Invoke] | 0.2052 | 0.2052 | 0.000 | 6.62 |
| Assembly-CSharp.dll!::PlayerMovementModule.FixedUpdate() [Invoke] | 0.0478 | 0.0355 | 0.000 | 104.54 |
| Assembly-CSharp.dll!::EnemyProjectile.FixedUpdate() [Invoke] | 0.0361 | 0.0239 | 0.000 | 3.73 |
| Assembly-CSharp.dll!::EnemyProjectile.OnTriggerEnter2D() [Invoke] | 0.0356 | 0.0117 | 0.040 | 19.88 |
| Assembly-CSharp.dll!::PotionEffectReceiver.Update() [Invoke] | 0.0302 | 0.0285 | 3.875 | 0.97 |
| Assembly-CSharp.dll!::EnemyAnimationDriver.FixedUpdate() [Invoke] | 0.0286 | 0.0286 | 0.000 | 2.08 |
| Assembly-CSharp.dll!::PlayerMeleeModule.OnDrawGizmos() [Invoke] | 0.0252 | 0.0252 | 0.000 | 12.58 |
| Assembly-CSharp.dll!::EnemyAnimationDriver.Update() [Invoke] | 0.0243 | 0.0243 | 0.000 | 0.81 |
| Assembly-CSharp.dll!::PlayerItemCollector.OnTriggerStay2D() [Invoke] | 0.0196 | 0.0196 | 0.000 | 1.08 |

실행 원본: `C:/Users/sdkmg/AppData/LocalLow/DefaultCompany/PotionJunkie\PerformanceDemo\20261015_062839_091_a635c1c2`

분석 폴더: `C:\Users\sdkmg\Desktop\project\2d-team01\Logs/ProfilerReports/mcp_20261015_062830_8a33c36c`
- repeat 1: Measure 기대/실제 1310/1310; `C:\Users\sdkmg\Desktop\project\2d-team01\Logs\ProfilerReports\mcp_20261015_062830_8a33c36c\capture_001`

### S6_Chase_Moving_60

![프레임 추이](../Logs/GameLoopProfiling/S6_Chase_Moving_60.svg)

선택 프레임 1,078개 / 유효 raw 1개.

기능 판정: True. 결과: IntervalCompleted. 조합 0회, 적 생성 60개, 투척 0회.

피해 이벤트: 적 0, 포션 0, 플레이어 278. 적 사망/해제 0/0. 플레이어 이동 합계 11.88. 부하 유지용 고체력 사본: True.

CPU 상위 Self 비용(메인 스레드, inclusive 값 중첩 합산 금지):

| 마커 | Self ms/f | Inclusive ms/f | Inclusive GC KiB/f | 최대 단일 호출 ms |
| --- | ---: | ---: | ---: | ---: |
| OnGUI | 3.2674 | 7.3894 | 17.608 | 3.871 |
| WaitForJobGroupID | 0.9027 | 1.0694 | 0.000 | 0.770 |
| Assembly-CSharp.dll!::RangedEnemyController.FixedUpdate() [Invoke] | 0.6709 | 1.4642 | 1.336 | 3.601 |
| Physics2D.OverlapCircleAll | 0.6333 | 0.6333 | 0.000 | 0.257 |
| Inl_On Record Render Graph | 0.4605 | 0.4622 | 0.000 | 0.461 |
| IMGUIContainer | 0.4181 | 7.8667 | 19.225 | 4.795 |
| Assembly-CSharp.dll!::PotionStatusVisual.LateUpdate() [Invoke] | 0.3250 | 0.3250 | 0.000 | 0.234 |
| Material.SetPassFast | 0.2833 | 0.3000 | 0.000 | 0.189 |
| GizmoManager.RecalculateGizmos | 0.2683 | 0.2684 | 0.000 | 1.094 |
| UnityEngine.IMGUIModule.dll!UnityEngine::GUIStyle.GetMeshInfo() [Invoke] | 0.2617 | 0.2617 | 0.000 | 0.216 |
| GizmoManager.DrawGizmos | 0.2488 | 0.7576 | 0.574 | 1.236 |
| Profiler.FlushMemoryCounters | 0.1991 | 0.1991 | 0.000 | 0.733 |
| UIR.DrawChain | 0.1902 | 8.1336 | 19.225 | 5.732 |
| Physics2D.SendTriggerContactCallbacks | 0.1748 | 0.3253 | 0.083 | 1.031 |
| Physics2D.FindNewContacts | 0.1287 | 0.1287 | 0.000 | 0.418 |

게임 코드 별도 보기(상위 inclusive 구간, 서로 중첩 가능):

| 함수/마커 | Inclusive ms/f | Self ms/f | Inclusive GC KiB/f | 평균 호출 us |
| --- | ---: | ---: | ---: | ---: |
| Assembly-CSharp.dll!::RangedEnemyController.FixedUpdate() [Invoke] | 1.4642 | 0.6709 | 1.336 | 44.36 |
| Assembly-CSharp.dll!::PotionStatusVisual.LateUpdate() [Invoke] | 0.3250 | 0.3250 | 0.000 | 5.33 |
| Assembly-CSharp.dll!::PlayerMovementModule.FixedUpdate() [Invoke] | 0.1158 | 0.0757 | 0.000 | 210.52 |
| Assembly-CSharp.dll!::EnemyProjectile.FixedUpdate() [Invoke] | 0.0803 | 0.0504 | 0.000 | 3.76 |
| Assembly-CSharp.dll!::EnemyProjectile.OnTriggerEnter2D() [Invoke] | 0.0742 | 0.0244 | 0.083 | 9.90 |
| Assembly-CSharp.dll!::EnemyAnimationDriver.FixedUpdate() [Invoke] | 0.0707 | 0.0707 | 0.000 | 2.14 |
| Assembly-CSharp.dll!::PotionEffectReceiver.Update() [Invoke] | 0.0510 | 0.0478 | 7.625 | 0.84 |
| Assembly-CSharp.dll!::EnemyAnimationDriver.Update() [Invoke] | 0.0471 | 0.0471 | 0.000 | 0.78 |
| Assembly-CSharp.dll!::PlayerItemCollector.OnTriggerStay2D() [Invoke] | 0.0403 | 0.0403 | 0.000 | 1.00 |
| Assembly-CSharp.dll!::PlayerInteractionModule.OnTriggerStay2D() [Invoke] | 0.0345 | 0.0345 | 0.000 | 0.85 |

실행 원본: `C:/Users/sdkmg/AppData/LocalLow/DefaultCompany/PotionJunkie\PerformanceDemo\20261015_062931_482_597cde5e`

분석 폴더: `C:\Users\sdkmg\Desktop\project\2d-team01\Logs/ProfilerReports/mcp_20261015_062922_cf27eb43`
- repeat 1: Measure 기대/실제 1079/1079; `C:\Users\sdkmg\Desktop\project\2d-team01\Logs\ProfilerReports\mcp_20261015_062922_cf27eb43\capture_001`

### S6_Chase_Moving_Dense_30

![프레임 추이](../Logs/GameLoopProfiling/S6_Chase_Moving_Dense_30.svg)

선택 프레임 1,130개 / 유효 raw 1개.

기능 판정: True. 결과: IntervalCompleted. 조합 0회, 적 생성 30개, 투척 0회.

피해 이벤트: 적 0, 포션 0, 플레이어 182. 적 사망/해제 0/0. 플레이어 이동 합계 10.49. 부하 유지용 고체력 사본: True.

CPU 상위 Self 비용(메인 스레드, inclusive 값 중첩 합산 금지):

| 마커 | Self ms/f | Inclusive ms/f | Inclusive GC KiB/f | 최대 단일 호출 ms |
| --- | ---: | ---: | ---: | ---: |
| OnGUI | 2.9946 | 6.5346 | 16.911 | 3.918 |
| GfxDeviceD3D12.WaitForLastPresentation.WaitForGPU | 1.2654 | 1.2654 | 0.000 | 465.544 |
| GameView.Paint | 0.8977 | 4.1590 | 9.024 | 500.329 |
| WaitForJobGroupID | 0.7164 | 0.8333 | 0.000 | 0.560 |
| Semaphore.WaitForSignal | 0.4803 | 0.4803 | 0.000 | 473.425 |
| Inl_On Record Render Graph | 0.4401 | 0.4418 | 0.000 | 0.700 |
| IMGUIContainer | 0.3985 | 6.9879 | 18.492 | 5.753 |
| Material.SetPassFast | 0.2550 | 0.2698 | 0.000 | 0.205 |
| UnityEngine.IMGUIModule.dll!UnityEngine::GUIStyle.GetMeshInfo() [Invoke] | 0.2425 | 0.2425 | 0.000 | 0.315 |
| Assembly-CSharp.dll!::RangedEnemyController.FixedUpdate() [Invoke] | 0.2177 | 0.4035 | 0.318 | 5.088 |
| Assembly-CSharp.dll!::PotionStatusVisual.LateUpdate() [Invoke] | 0.1924 | 0.1924 | 0.000 | 0.206 |
| Profiler.FlushMemoryCounters | 0.1901 | 0.1901 | 0.000 | 0.517 |
| UIR.DrawChain | 0.1822 | 7.2415 | 18.492 | 5.857 |
| GizmoManager.DrawGizmos | 0.1558 | 0.4542 | 0.574 | 0.706 |
| Physics2D.OverlapCircleAll | 0.1194 | 0.1194 | 0.000 | 0.257 |

게임 코드 별도 보기(상위 inclusive 구간, 서로 중첩 가능):

| 함수/마커 | Inclusive ms/f | Self ms/f | Inclusive GC KiB/f | 평균 호출 us |
| --- | ---: | ---: | ---: | ---: |
| Assembly-CSharp.dll!::RangedEnemyController.FixedUpdate() [Invoke] | 0.4035 | 0.2177 | 0.318 | 28.73 |
| Assembly-CSharp.dll!::PotionStatusVisual.LateUpdate() [Invoke] | 0.1924 | 0.1924 | 0.000 | 6.21 |
| Assembly-CSharp.dll!::PlayerMovementModule.FixedUpdate() [Invoke] | 0.0461 | 0.0335 | 0.001 | 98.58 |
| Assembly-CSharp.dll!::EnemyProjectile.OnTriggerEnter2D() [Invoke] | 0.0423 | 0.0130 | 0.049 | 15.87 |
| Assembly-CSharp.dll!::PotionEffectReceiver.Update() [Invoke] | 0.0300 | 0.0284 | 3.875 | 0.97 |
| Assembly-CSharp.dll!::EnemyAnimationDriver.FixedUpdate() [Invoke] | 0.0277 | 0.0273 | 0.000 | 1.97 |
| Assembly-CSharp.dll!::EnemyProjectile.FixedUpdate() [Invoke] | 0.0257 | 0.0142 | 0.000 | 4.82 |
| Assembly-CSharp.dll!::EnemyAnimationDriver.Update() [Invoke] | 0.0249 | 0.0249 | 0.000 | 0.83 |
| Assembly-CSharp.dll!::PlayerMeleeModule.OnDrawGizmos() [Invoke] | 0.0249 | 0.0249 | 0.000 | 12.44 |
| Assembly-CSharp.dll!::PlayerItemCollector.OnTriggerStay2D() [Invoke] | 0.0185 | 0.0185 | 0.000 | 1.08 |

실행 원본: `C:/Users/sdkmg/AppData/LocalLow/DefaultCompany/PotionJunkie\PerformanceDemo\20261015_063045_695_0968f59a`

분석 폴더: `C:\Users\sdkmg\Desktop\project\2d-team01\Logs/ProfilerReports/mcp_20261015_063036_7641e3fd`
- repeat 1: Measure 기대/실제 1131/1131; `C:\Users\sdkmg\Desktop\project\2d-team01\Logs\ProfilerReports\mcp_20261015_063036_7641e3fd\capture_001`

### S6_Chase_Static_10

![프레임 추이](../Logs/GameLoopProfiling/S6_Chase_Static_10.svg)

선택 프레임 1,364개 / 유효 raw 1개.

기능 판정: True. 결과: IntervalCompleted. 조합 0회, 적 생성 10개, 투척 0회.

피해 이벤트: 적 0, 포션 0, 플레이어 66. 적 사망/해제 0/0. 플레이어 이동 합계 0.00. 부하 유지용 고체력 사본: True.

CPU 상위 Self 비용(메인 스레드, inclusive 값 중첩 합산 금지):

| 마커 | Self ms/f | Inclusive ms/f | Inclusive GC KiB/f | 최대 단일 호출 ms |
| --- | ---: | ---: | ---: | ---: |
| OnGUI | 2.6510 | 5.8711 | 15.789 | 3.862 |
| GfxDeviceD3D12.WaitForLastPresentation.WaitForGPU | 1.3784 | 1.3784 | 0.000 | 483.193 |
| WaitForJobGroupID | 0.5868 | 0.6625 | 0.000 | 0.329 |
| Semaphore.WaitForSignal | 0.4750 | 0.4750 | 0.000 | 458.978 |
| Inl_On Record Render Graph | 0.4287 | 0.4302 | 0.000 | 0.592 |
| GameView.Paint | 0.3839 | 3.5296 | 9.019 | 494.550 |
| IMGUIContainer | 0.3614 | 6.2831 | 17.313 | 5.213 |
| Material.SetPassFast | 0.2288 | 0.2422 | 0.000 | 0.246 |
| UnityEngine.IMGUIModule.dll!UnityEngine::GUIStyle.GetMeshInfo() [Invoke] | 0.2085 | 0.2085 | 0.000 | 0.206 |
| Profiler.FlushMemoryCounters | 0.1848 | 0.1848 | 0.000 | 0.574 |
| UIR.DrawChain | 0.1710 | 6.5216 | 17.313 | 5.255 |
| AssetDatabase.GetGUIDFromAssetPathV2 | 0.1061 | 0.1061 | 0.000 | 0.184 |
| GizmoManager.DrawGizmos | 0.1058 | 0.3051 | 0.574 | 0.380 |
| Assembly-CSharp.dll!::PotionStatusVisual.LateUpdate() [Invoke] | 0.0918 | 0.0918 | 0.000 | 0.220 |
| Inl_UniversalRenderPipeline.RenderSingleCameraInternal: Main Camera | 0.0856 | 0.7813 | 0.000 | 1.324 |

게임 코드 별도 보기(상위 inclusive 구간, 서로 중첩 가능):

| 함수/마커 | Inclusive ms/f | Self ms/f | Inclusive GC KiB/f | 평균 호출 us |
| --- | ---: | ---: | ---: | ---: |
| Assembly-CSharp.dll!::RangedEnemyController.FixedUpdate() [Invoke] | 0.1000 | 0.0573 | 0.082 | 22.37 |
| Assembly-CSharp.dll!::PotionStatusVisual.LateUpdate() [Invoke] | 0.0918 | 0.0918 | 0.000 | 8.34 |
| Assembly-CSharp.dll!::PlayerMeleeModule.OnDrawGizmos() [Invoke] | 0.0238 | 0.0238 | 0.000 | 11.89 |
| Assembly-CSharp.dll!::PlayerMovementModule.FixedUpdate() [Invoke] | 0.0172 | 0.0148 | 0.000 | 38.51 |
| Assembly-CSharp.dll!::EnemyProjectile.OnTriggerEnter2D() [Invoke] | 0.0158 | 0.0054 | 0.018 | 80.77 |
| Assembly-CSharp.dll!::StatusScreenVignette.Update() [Invoke] | 0.0149 | 0.0149 | 0.000 | 14.92 |
| Assembly-CSharp.dll!::PotionEffectReceiver.Update() [Invoke] | 0.0134 | 0.0127 | 1.375 | 1.22 |
| Assembly-CSharp.dll!::EnemyProjectile.FixedUpdate() [Invoke] | 0.0099 | 0.0058 | 0.000 | 5.65 |
| Assembly-CSharp.dll!::EnemyAnimationDriver.FixedUpdate() [Invoke] | 0.0092 | 0.0092 | 0.000 | 2.07 |
| Assembly-CSharp.dll!::EnemyAnimationDriver.Update() [Invoke] | 0.0087 | 0.0087 | 0.000 | 0.87 |

실행 원본: `C:/Users/sdkmg/AppData/LocalLow/DefaultCompany/PotionJunkie\PerformanceDemo\20261015_063137_144_db674d59`

분석 폴더: `C:\Users\sdkmg\Desktop\project\2d-team01\Logs/ProfilerReports/mcp_20261015_063127_43f1d239`
- repeat 1: Measure 기대/실제 1365/1365; `C:\Users\sdkmg\Desktop\project\2d-team01\Logs\ProfilerReports\mcp_20261015_063127_43f1d239\capture_001`

### S6_Chase_Static_30

![프레임 추이](../Logs/GameLoopProfiling/S6_Chase_Static_30.svg)

선택 프레임 1,251개 / 유효 raw 1개.

기능 판정: True. 결과: IntervalCompleted. 조합 0회, 적 생성 30개, 투척 0회.

피해 이벤트: 적 0, 포션 0, 플레이어 159. 적 사망/해제 0/0. 플레이어 이동 합계 0.00. 부하 유지용 고체력 사본: True.

CPU 상위 Self 비용(메인 스레드, inclusive 값 중첩 합산 금지):

| 마커 | Self ms/f | Inclusive ms/f | Inclusive GC KiB/f | 최대 단일 호출 ms |
| --- | ---: | ---: | ---: | ---: |
| OnGUI | 3.0084 | 6.5800 | 16.963 | 5.812 |
| Semaphore.WaitForSignal | 1.1969 | 1.1969 | 0.000 | 485.304 |
| WaitForJobGroupID | 0.7050 | 0.8330 | 0.000 | 0.407 |
| Inl_On Record Render Graph | 0.4411 | 0.4427 | 0.000 | 1.183 |
| IMGUIContainer | 0.3966 | 7.0321 | 18.548 | 7.711 |
| Assembly-CSharp.dll!::RangedEnemyController.FixedUpdate() [Invoke] | 0.3861 | 0.5514 | 0.232 | 0.915 |
| Material.SetPassFast | 0.2600 | 0.2752 | 0.000 | 0.258 |
| UnityEngine.IMGUIModule.dll!UnityEngine::GUIStyle.GetMeshInfo() [Invoke] | 0.2438 | 0.2438 | 0.000 | 0.230 |
| Assembly-CSharp.dll!::PotionStatusVisual.LateUpdate() [Invoke] | 0.1991 | 0.1991 | 0.000 | 0.166 |
| Profiler.FlushMemoryCounters | 0.1905 | 0.1905 | 0.000 | 0.568 |
| UIR.DrawChain | 0.1841 | 7.2895 | 18.548 | 7.763 |
| GizmoManager.DrawGizmos | 0.1518 | 0.4613 | 0.574 | 0.773 |
| AssetDatabase.GetGUIDFromAssetPathV2 | 0.1152 | 0.1152 | 0.000 | 0.171 |
| Physics2D.OverlapCircleAll | 0.1090 | 0.1090 | 0.000 | 0.291 |
| GizmoManager.RecalculateGizmos | 0.1023 | 0.1023 | 0.000 | 0.638 |

게임 코드 별도 보기(상위 inclusive 구간, 서로 중첩 가능):

| 함수/마커 | Inclusive ms/f | Self ms/f | Inclusive GC KiB/f | 평균 호출 us |
| --- | ---: | ---: | ---: | ---: |
| Assembly-CSharp.dll!::RangedEnemyController.FixedUpdate() [Invoke] | 0.5514 | 0.3861 | 0.232 | 42.19 |
| Assembly-CSharp.dll!::PotionStatusVisual.LateUpdate() [Invoke] | 0.1991 | 0.1991 | 0.000 | 6.42 |
| Assembly-CSharp.dll!::PlayerMovementModule.FixedUpdate() [Invoke] | 0.0446 | 0.0352 | 0.000 | 102.40 |
| Assembly-CSharp.dll!::EnemyProjectile.OnTriggerEnter2D() [Invoke] | 0.0409 | 0.0130 | 0.044 | 43.14 |
| Assembly-CSharp.dll!::PotionEffectReceiver.Update() [Invoke] | 0.0302 | 0.0284 | 3.875 | 0.98 |
| Assembly-CSharp.dll!::EnemyAnimationDriver.FixedUpdate() [Invoke] | 0.0273 | 0.0273 | 0.000 | 2.09 |
| Assembly-CSharp.dll!::PlayerMeleeModule.OnDrawGizmos() [Invoke] | 0.0254 | 0.0254 | 0.000 | 12.71 |
| Assembly-CSharp.dll!::EnemyAnimationDriver.Update() [Invoke] | 0.0248 | 0.0248 | 0.000 | 0.83 |
| Assembly-CSharp.dll!::EnemyProjectile.FixedUpdate() [Invoke] | 0.0216 | 0.0127 | 0.000 | 4.88 |
| Assembly-CSharp.dll!::PlayerItemCollector.OnTriggerStay2D() [Invoke] | 0.0183 | 0.0183 | 0.000 | 1.11 |

실행 원본: `C:/Users/sdkmg/AppData/LocalLow/DefaultCompany/PotionJunkie\PerformanceDemo\20261015_063227_826_f71ef5d5`

분석 폴더: `C:\Users\sdkmg\Desktop\project\2d-team01\Logs/ProfilerReports/mcp_20261015_063218_874e3134`
- repeat 1: Measure 기대/실제 1252/1252; `C:\Users\sdkmg\Desktop\project\2d-team01\Logs\ProfilerReports\mcp_20261015_063218_874e3134\capture_001`

### S6_Chase_Static_60

![프레임 추이](../Logs/GameLoopProfiling/S6_Chase_Static_60.svg)

선택 프레임 882개 / 유효 raw 1개.

기능 판정: True. 결과: IntervalCompleted. 조합 0회, 적 생성 60개, 투척 0회.

피해 이벤트: 적 0, 포션 0, 플레이어 300. 적 사망/해제 0/0. 플레이어 이동 합계 0.00. 부하 유지용 고체력 사본: True.

CPU 상위 Self 비용(메인 스레드, inclusive 값 중첩 합산 금지):

| 마커 | Self ms/f | Inclusive ms/f | Inclusive GC KiB/f | 최대 단일 호출 ms |
| --- | ---: | ---: | ---: | ---: |
| OnGUI | 3.0523 | 6.7636 | 17.959 | 5.222 |
| Semaphore.WaitForSignal | 2.4233 | 2.4233 | 0.000 | 680.403 |
| Assembly-CSharp.dll!::RangedEnemyController.FixedUpdate() [Invoke] | 1.7728 | 2.4546 | 0.916 | 1.313 |
| GfxDeviceD3D12.WaitForLastPresentation.WaitForGPU | 1.6598 | 1.6598 | 0.000 | 487.365 |
| WaitForJobGroupID | 0.8696 | 1.0025 | 0.000 | 0.519 |
| Physics2D.OverlapCircleAll | 0.5373 | 0.5373 | 0.000 | 0.433 |
| Inl_On Record Render Graph | 0.4163 | 0.4178 | 0.000 | 0.526 |
| IMGUIContainer | 0.3892 | 7.2076 | 19.594 | 5.583 |
| Assembly-CSharp.dll!::PotionStatusVisual.LateUpdate() [Invoke] | 0.2931 | 0.2931 | 0.000 | 0.214 |
| GizmoManager.RecalculateGizmos | 0.2692 | 0.2693 | 0.000 | 0.927 |
| Material.SetPassFast | 0.2583 | 0.2735 | 0.000 | 0.212 |
| UnityEngine.IMGUIModule.dll!UnityEngine::GUIStyle.GetMeshInfo() [Invoke] | 0.2469 | 0.2469 | 0.000 | 0.220 |
| Profiler.FlushMemoryCounters | 0.1843 | 0.1843 | 0.000 | 0.497 |
| GizmoManager.DrawGizmos | 0.1841 | 0.6651 | 0.574 | 1.458 |
| UIR.DrawChain | 0.1727 | 7.4500 | 19.594 | 6.259 |

게임 코드 별도 보기(상위 inclusive 구간, 서로 중첩 가능):

| 함수/마커 | Inclusive ms/f | Self ms/f | Inclusive GC KiB/f | 평균 호출 us |
| --- | ---: | ---: | ---: | ---: |
| Assembly-CSharp.dll!::RangedEnemyController.FixedUpdate() [Invoke] | 2.4546 | 1.7728 | 0.916 | 65.37 |
| Assembly-CSharp.dll!::PotionStatusVisual.LateUpdate() [Invoke] | 0.2931 | 0.2931 | 0.000 | 4.80 |
| Assembly-CSharp.dll!::PlayerMovementModule.FixedUpdate() [Invoke] | 0.1114 | 0.0810 | 0.000 | 177.93 |
| Assembly-CSharp.dll!::EnemyProjectile.OnTriggerEnter2D() [Invoke] | 0.0860 | 0.0251 | 0.111 | 19.43 |
| Assembly-CSharp.dll!::EnemyAnimationDriver.FixedUpdate() [Invoke] | 0.0704 | 0.0704 | 0.000 | 1.88 |
| Assembly-CSharp.dll!::PotionEffectReceiver.Update() [Invoke] | 0.0480 | 0.0450 | 7.625 | 0.79 |
| Assembly-CSharp.dll!::EnemyAnimationDriver.Update() [Invoke] | 0.0448 | 0.0448 | 0.000 | 0.75 |
| Assembly-CSharp.dll!::EnemyProjectile.FixedUpdate() [Invoke] | 0.0439 | 0.0248 | 0.000 | 3.94 |
| Assembly-CSharp.dll!::PlayerItemCollector.OnTriggerStay2D() [Invoke] | 0.0411 | 0.0411 | 0.000 | 0.96 |
| Assembly-CSharp.dll!::PlayerInteractionModule.OnTriggerStay2D() [Invoke] | 0.0359 | 0.0359 | 0.000 | 0.84 |

실행 원본: `C:/Users/sdkmg/AppData/LocalLow/DefaultCompany/PotionJunkie\PerformanceDemo\20261015_063320_201_db714c4d`

분석 폴더: `C:\Users\sdkmg\Desktop\project\2d-team01\Logs/ProfilerReports/mcp_20261015_063310_c44fbf47`
- repeat 1: Measure 기대/실제 883/883; `C:\Users\sdkmg\Desktop\project\2d-team01\Logs\ProfilerReports\mcp_20261015_063310_c44fbf47\capture_001`

### S7_Area_ContactEffects_01

![프레임 추이](../Logs/GameLoopProfiling/S7_Area_ContactEffects_01.svg)

선택 프레임 787개 / 유효 raw 1개.

기능 판정: True. 결과: IntervalCompleted. 조합 0회, 적 생성 1개, 투척 0회.

피해 이벤트: 적 7, 포션 6, 플레이어 14. 적 사망/해제 1/1. 플레이어 이동 합계 0.00. 부하 유지용 고체력 사본: True.

CPU 상위 Self 비용(메인 스레드, inclusive 값 중첩 합산 금지):

| 마커 | Self ms/f | Inclusive ms/f | Inclusive GC KiB/f | 최대 단일 호출 ms |
| --- | ---: | ---: | ---: | ---: |
| OnGUI | 2.6438 | 6.1152 | 14.952 | 3.567 |
| Inl_On Record Render Graph | 0.4677 | 0.4694 | 0.000 | 0.434 |
| IMGUIContainer | 0.3791 | 6.5470 | 16.418 | 4.217 |
| WaitForJobGroupID | 0.3127 | 0.3732 | 0.000 | 0.282 |
| Material.SetPassFast | 0.2359 | 0.2500 | 0.000 | 0.206 |
| Profiler.FlushMemoryCounters | 0.2114 | 0.2114 | 0.000 | 0.549 |
| UnityEngine.IMGUIModule.dll!UnityEngine::GUIStyle.GetMeshInfo() [Invoke] | 0.1993 | 0.2006 | 0.023 | 0.746 |
| UIR.DrawChain | 0.1849 | 6.8019 | 16.418 | 5.160 |
| GfxDeviceD3D12.WaitForLastPresentation.WaitForGPU | 0.1804 | 0.1804 | 0.000 | 30.479 |
| AssetDatabase.GetGUIDFromAssetPathV2 | 0.1106 | 0.1106 | 0.000 | 0.205 |
| GizmoManager.DrawGizmos | 0.1068 | 0.3023 | 0.574 | 0.373 |
| Inl_UniversalRenderPipeline.RenderSingleCameraInternal: SceneCamera | 0.0997 | 0.8295 | 0.000 | 1.394 |
| Inl_UniversalRenderPipeline.RenderSingleCameraInternal: Main Camera | 0.0936 | 0.8137 | 0.000 | 1.246 |
| Inl_NRPRGComp_ExecuteBeginRenderpassCommand | 0.0861 | 0.0861 | 0.000 | 0.104 |
| Inl_UniversalRenderTotal | 0.0855 | 2.5129 | 0.934 | 1.947 |

게임 코드 별도 보기(상위 inclusive 구간, 서로 중첩 가능):

| 함수/마커 | Inclusive ms/f | Self ms/f | Inclusive GC KiB/f | 평균 호출 us |
| --- | ---: | ---: | ---: | ---: |
| Assembly-CSharp.dll!::PotionStatusVisual.LateUpdate() [Invoke] | 0.0517 | 0.0505 | 0.972 | 33.27 |
| Assembly-CSharp.dll!::PotionAreaEffect.Update() [Invoke] | 0.0336 | 0.0203 | 0.320 | 35.08 |
| Assembly-CSharp.dll!::StatusScreenVignette.Update() [Invoke] | 0.0286 | 0.0279 | 0.838 | 28.62 |
| Assembly-CSharp.dll!::PlayerMeleeModule.OnDrawGizmos() [Invoke] | 0.0264 | 0.0264 | 0.000 | 13.20 |
| Assembly-CSharp.dll!::PotionAreaVisual.Update() [Invoke] | 0.0233 | 0.0233 | 0.000 | 24.31 |
| Assembly-CSharp.dll!::RangedEnemyController.FixedUpdate() [Invoke] | 0.0117 | 0.0069 | 0.004 | 55.32 |
| Assembly-CSharp.dll!::SoundManager.Update() [Invoke] | 0.0103 | 0.0103 | 0.000 | 10.26 |
| Assembly-CSharp.dll!::PlayerAnimationDriver.Update() [Invoke] | 0.0069 | 0.0069 | 0.000 | 6.90 |
| Assembly-CSharp.dll!::PotionEffectReceiver.Update() [Invoke] | 0.0066 | 0.0063 | 0.194 | 4.23 |
| Assembly-CSharp.dll!::PlayerMovementModule.FixedUpdate() [Invoke] | 0.0063 | 0.0056 | 0.000 | 16.21 |

실행 원본: `C:/Users/sdkmg/AppData/LocalLow/DefaultCompany/PotionJunkie\PerformanceDemo\20261015_063411_076_fd550cc1`

분석 폴더: `C:\Users\sdkmg\Desktop\project\2d-team01\Logs/ProfilerReports/mcp_20261015_063400_712ecffb`
- repeat 1: Measure 기대/실제 788/788; `C:\Users\sdkmg\Desktop\project\2d-team01\Logs\ProfilerReports\mcp_20261015_063400_712ecffb\capture_001`

### S7_Area_ContactEffects_10

![프레임 추이](../Logs/GameLoopProfiling/S7_Area_ContactEffects_10.svg)

선택 프레임 757개 / 유효 raw 1개.

기능 판정: True. 결과: IntervalCompleted. 조합 0회, 적 생성 10개, 투척 0회.

피해 이벤트: 적 70, 포션 60, 플레이어 38. 적 사망/해제 10/10. 플레이어 이동 합계 0.00. 부하 유지용 고체력 사본: True.

CPU 상위 Self 비용(메인 스레드, inclusive 값 중첩 합산 금지):

| 마커 | Self ms/f | Inclusive ms/f | Inclusive GC KiB/f | 최대 단일 호출 ms |
| --- | ---: | ---: | ---: | ---: |
| OnGUI | 2.5391 | 5.7880 | 15.492 | 3.169 |
| WaitForJobGroupID | 0.5447 | 0.6366 | 0.000 | 0.320 |
| Inl_On Record Render Graph | 0.4177 | 0.4191 | 0.000 | 0.420 |
| GfxDeviceD3D12.WaitForLastPresentation.WaitForGPU | 0.4138 | 0.4138 | 0.000 | 22.522 |
| IMGUIContainer | 0.3575 | 6.1957 | 17.000 | 4.029 |
| Material.SetPassFast | 0.2247 | 0.2388 | 0.000 | 0.141 |
| UnityEngine.IMGUIModule.dll!UnityEngine::GUIStyle.GetMeshInfo() [Invoke] | 0.1957 | 0.1965 | 0.024 | 0.395 |
| Profiler.FlushMemoryCounters | 0.1862 | 0.1862 | 0.000 | 0.314 |
| UIR.DrawChain | 0.1690 | 6.4287 | 17.000 | 4.061 |
| Assembly-CSharp.dll!::PotionStatusVisual.LateUpdate() [Invoke] | 0.1667 | 0.1717 | 4.156 | 0.311 |
| GizmoManager.DrawGizmos | 0.1110 | 0.3040 | 0.574 | 0.405 |
| AssetDatabase.GetGUIDFromAssetPathV2 | 0.1034 | 0.1034 | 0.000 | 0.177 |
| Inl_UniversalRenderPipeline.RenderSingleCameraInternal: SceneCamera | 0.0891 | 0.7350 | 0.000 | 1.111 |
| Inl_UniversalRenderPipeline.RenderSingleCameraInternal: Main Camera | 0.0841 | 0.7910 | 0.000 | 1.169 |
| Inl_NRPRGComp_ExecuteBeginRenderpassCommand | 0.0787 | 0.0787 | 0.000 | 0.082 |

게임 코드 별도 보기(상위 inclusive 구간, 서로 중첩 가능):

| 함수/마커 | Inclusive ms/f | Self ms/f | Inclusive GC KiB/f | 평균 호출 us |
| --- | ---: | ---: | ---: | ---: |
| Assembly-CSharp.dll!::PotionStatusVisual.LateUpdate() [Invoke] | 0.1717 | 0.1667 | 4.156 | 25.59 |
| Assembly-CSharp.dll!::PotionAreaEffect.Update() [Invoke] | 0.0571 | 0.0272 | 0.857 | 59.49 |
| Assembly-CSharp.dll!::RangedEnemyController.FixedUpdate() [Invoke] | 0.0570 | 0.0333 | 0.048 | 25.84 |
| Assembly-CSharp.dll!::StatusScreenVignette.Update() [Invoke] | 0.0260 | 0.0252 | 0.840 | 25.97 |
| Assembly-CSharp.dll!::PlayerMeleeModule.OnDrawGizmos() [Invoke] | 0.0235 | 0.0235 | 0.000 | 11.75 |
| Assembly-CSharp.dll!::PotionAreaVisual.Update() [Invoke] | 0.0208 | 0.0208 | 0.000 | 21.66 |
| Assembly-CSharp.dll!::PlayerMovementModule.FixedUpdate() [Invoke] | 0.0117 | 0.0089 | 0.000 | 30.57 |
| Assembly-CSharp.dll!::PotionEffectReceiver.Update() [Invoke] | 0.0101 | 0.0095 | 0.840 | 1.51 |
| Assembly-CSharp.dll!::SoundManager.Update() [Invoke] | 0.0098 | 0.0098 | 0.000 | 9.78 |
| Assembly-CSharp.dll!::EnemyProjectile.OnTriggerEnter2D() [Invoke] | 0.0087 | 0.0026 | 0.009 | 35.98 |

실행 원본: `C:/Users/sdkmg/AppData/LocalLow/DefaultCompany/PotionJunkie\PerformanceDemo\20261015_063446_622_f5cd26c4`

분석 폴더: `C:\Users\sdkmg\Desktop\project\2d-team01\Logs/ProfilerReports/mcp_20261015_063437_f7230207`
- repeat 1: Measure 기대/실제 758/758; `C:\Users\sdkmg\Desktop\project\2d-team01\Logs\ProfilerReports\mcp_20261015_063437_f7230207\capture_001`

### S7_Area_ContactEffects_30

![프레임 추이](../Logs/GameLoopProfiling/S7_Area_ContactEffects_30.svg)

선택 프레임 651개 / 유효 raw 1개.

기능 판정: True. 결과: IntervalCompleted. 조합 0회, 적 생성 30개, 투척 0회.

피해 이벤트: 적 210, 포션 180, 플레이어 78. 적 사망/해제 30/30. 플레이어 이동 합계 0.00. 부하 유지용 고체력 사본: True.

CPU 상위 Self 비용(메인 스레드, inclusive 값 중첩 합산 금지):

| 마커 | Self ms/f | Inclusive ms/f | Inclusive GC KiB/f | 최대 단일 호출 ms |
| --- | ---: | ---: | ---: | ---: |
| OnGUI | 2.8391 | 6.4900 | 16.218 | 4.902 |
| WaitForJobGroupID | 0.6370 | 0.7542 | 0.000 | 0.334 |
| Inl_On Record Render Graph | 0.4410 | 0.4427 | 0.000 | 0.390 |
| IMGUIContainer | 0.3859 | 6.9293 | 17.763 | 5.953 |
| Assembly-CSharp.dll!::PotionStatusVisual.LateUpdate() [Invoke] | 0.3169 | 0.3302 | 10.739 | 0.382 |
| Material.SetPassFast | 0.2494 | 0.2643 | 0.000 | 0.118 |
| UnityEngine.IMGUIModule.dll!UnityEngine::GUIStyle.GetMeshInfo() [Invoke] | 0.2210 | 0.2221 | 0.028 | 0.446 |
| Profiler.FlushMemoryCounters | 0.1975 | 0.1975 | 0.000 | 0.411 |
| UIR.DrawChain | 0.1838 | 7.1840 | 17.763 | 6.003 |
| GizmoManager.DrawGizmos | 0.1725 | 0.4520 | 0.574 | 0.718 |
| GfxDeviceD3D12.WaitForLastPresentation.WaitForGPU | 0.1624 | 0.1624 | 0.000 | 23.544 |
| Assembly-CSharp.dll!::RangedEnemyController.FixedUpdate() [Invoke] | 0.1569 | 0.2707 | 0.199 | 0.517 |
| AssetDatabase.GetGUIDFromAssetPathV2 | 0.1112 | 0.1112 | 0.000 | 0.120 |
| Inl_UniversalRenderPipeline.RenderSingleCameraInternal: SceneCamera | 0.0952 | 0.7717 | 0.000 | 1.195 |
| Inl_UniversalRenderPipeline.RenderSingleCameraInternal: Main Camera | 0.0892 | 0.8812 | 0.000 | 1.439 |

게임 코드 별도 보기(상위 inclusive 구간, 서로 중첩 가능):

| 함수/마커 | Inclusive ms/f | Self ms/f | Inclusive GC KiB/f | 평균 호출 us |
| --- | ---: | ---: | ---: | ---: |
| Assembly-CSharp.dll!::PotionStatusVisual.LateUpdate() [Invoke] | 0.3302 | 0.3169 | 10.739 | 18.68 |
| Assembly-CSharp.dll!::RangedEnemyController.FixedUpdate() [Invoke] | 0.2707 | 0.1569 | 0.199 | 35.17 |
| Assembly-CSharp.dll!::PotionAreaEffect.Update() [Invoke] | 0.1238 | 0.0512 | 1.807 | 130.24 |
| Assembly-CSharp.dll!::PlayerMovementModule.FixedUpdate() [Invoke] | 0.0317 | 0.0192 | 0.000 | 73.35 |
| Assembly-CSharp.dll!::StatusScreenVignette.Update() [Invoke] | 0.0277 | 0.0270 | 0.832 | 27.70 |
| Assembly-CSharp.dll!::EnemyProjectile.OnTriggerEnter2D() [Invoke] | 0.0268 | 0.0077 | 0.030 | 11.88 |
| Assembly-CSharp.dll!::PlayerMeleeModule.OnDrawGizmos() [Invoke] | 0.0256 | 0.0256 | 0.000 | 12.81 |
| Assembly-CSharp.dll!::PotionAreaVisual.Update() [Invoke] | 0.0213 | 0.0213 | 0.000 | 22.45 |
| Assembly-CSharp.dll!::PotionEffectReceiver.Update() [Invoke] | 0.0190 | 0.0178 | 2.216 | 1.07 |
| Assembly-CSharp.dll!::EnemyAnimationDriver.FixedUpdate() [Invoke] | 0.0154 | 0.0154 | 0.000 | 2.00 |

실행 원본: `C:/Users/sdkmg/AppData/LocalLow/DefaultCompany/PotionJunkie\PerformanceDemo\20261015_063522_137_ff88143c`

분석 폴더: `C:\Users\sdkmg\Desktop\project\2d-team01\Logs/ProfilerReports/mcp_20261015_063512_d5b93b2a`
- repeat 1: Measure 기대/실제 652/652; `C:\Users\sdkmg\Desktop\project\2d-team01\Logs\ProfilerReports\mcp_20261015_063512_d5b93b2a\capture_001`

### S7_Combine_ContactEffects_10

![프레임 추이](../Logs/GameLoopProfiling/S7_Combine_ContactEffects_10.svg)

선택 프레임 782개 / 유효 raw 1개.

기능 판정: True. 결과: IntervalCompleted. 조합 1회, 적 생성 10개, 투척 0회.

피해 이벤트: 적 50, 포션 40, 플레이어 35. 적 사망/해제 10/10. 플레이어 이동 합계 0.00. 부하 유지용 고체력 사본: True.

CPU 상위 Self 비용(메인 스레드, inclusive 값 중첩 합산 금지):

| 마커 | Self ms/f | Inclusive ms/f | Inclusive GC KiB/f | 최대 단일 호출 ms |
| --- | ---: | ---: | ---: | ---: |
| OnGUI | 2.4263 | 5.6066 | 14.953 | 3.131 |
| WaitForJobGroupID | 0.4701 | 0.5451 | 0.000 | 0.371 |
| Inl_On Record Render Graph | 0.4118 | 0.4133 | 0.000 | 0.392 |
| GfxDeviceD3D12.WaitForLastPresentation.WaitForGPU | 0.3954 | 0.3954 | 0.000 | 6.828 |
| IMGUIContainer | 0.3475 | 6.0036 | 16.432 | 3.638 |
| Material.SetPassFast | 0.2128 | 0.2252 | 0.000 | 0.226 |
| Profiler.FlushMemoryCounters | 0.1893 | 0.1893 | 0.000 | 0.412 |
| UnityEngine.IMGUIModule.dll!UnityEngine::GUIStyle.GetMeshInfo() [Invoke] | 0.1845 | 0.1854 | 0.023 | 0.408 |
| UIR.DrawChain | 0.1674 | 6.2325 | 16.432 | 4.607 |
| Mono.JIT | 0.1428 | 0.1430 | 0.006 | 31.540 |
| GizmoManager.DrawGizmos | 0.1049 | 0.2921 | 0.574 | 0.396 |
| AssetDatabase.GetGUIDFromAssetPathV2 | 0.1024 | 0.1024 | 0.000 | 0.136 |
| Assembly-CSharp.dll!::PotionStatusVisual.LateUpdate() [Invoke] | 0.0947 | 0.0987 | 0.562 | 3.115 |
| Inl_UniversalRenderPipeline.RenderSingleCameraInternal: SceneCamera | 0.0884 | 0.7392 | 0.000 | 1.128 |
| Inl_UniversalRenderPipeline.RenderSingleCameraInternal: Main Camera | 0.0828 | 0.7631 | 0.000 | 1.269 |

게임 코드 별도 보기(상위 inclusive 구간, 서로 중첩 가능):

| 함수/마커 | Inclusive ms/f | Self ms/f | Inclusive GC KiB/f | 평균 호출 us |
| --- | ---: | ---: | ---: | ---: |
| Assembly-CSharp.dll!::PotionProjectile.Update() [Invoke] | 0.1362 | 0.0117 | 0.526 | 591.53 |
| Assembly-CSharp.dll!::PotionStatusVisual.LateUpdate() [Invoke] | 0.0987 | 0.0947 | 0.562 | 21.32 |
| Assembly-CSharp.dll!::VFXBurstField.Update() [Invoke] | 0.0741 | 0.0658 | 0.000 | 86.86 |
| Assembly-CSharp.dll!::PotionAreaEffect.Update() [Invoke] | 0.0516 | 0.0254 | 0.572 | 60.48 |
| PerfDemo.CombineCommit | 0.0367 | 0.0083 | 0.066 | 28707.30 |
| Assembly-CSharp.dll!::RangedEnemyController.FixedUpdate() [Invoke] | 0.0284 | 0.0158 | 0.024 | 20.95 |
| Assembly-CSharp.dll!::StatusScreenVignette.Update() [Invoke] | 0.0248 | 0.0242 | 0.694 | 24.84 |
| Assembly-CSharp.dll!::PlayerMeleeModule.OnDrawGizmos() [Invoke] | 0.0232 | 0.0232 | 0.000 | 11.59 |
| Assembly-CSharp.dll!::PotionAreaVisual.Update() [Invoke] | 0.0157 | 0.0156 | 0.000 | 9.90 |
| Assembly-CSharp.dll!::SoundManager.Update() [Invoke] | 0.0097 | 0.0097 | 0.000 | 9.74 |

실행 원본: `C:/Users/sdkmg/AppData/LocalLow/DefaultCompany/PotionJunkie\PerformanceDemo\20261015_063559_826_1325da5b`

분석 폴더: `C:\Users\sdkmg\Desktop\project\2d-team01\Logs/ProfilerReports/mcp_20261015_063550_82659357`
- repeat 1: Measure 기대/실제 783/783; `C:\Users\sdkmg\Desktop\project\2d-team01\Logs\ProfilerReports\mcp_20261015_063550_82659357\capture_001`

### S8_Combat_Minimal

![프레임 추이](../Logs/GameLoopProfiling/S8_Combat_Minimal.svg)

선택 프레임 914개 / 유효 raw 1개.

기능 판정: True. 결과: CombatCompleted. 조합 2회, 적 생성 6개, 투척 4회.

피해 이벤트: 적 29, 포션 19, 플레이어 16. 적 사망/해제 6/6. 플레이어 이동 합계 24.90. 부하 유지용 고체력 사본: False.

CPU 상위 Self 비용(메인 스레드, inclusive 값 중첩 합산 금지):

| 마커 | Self ms/f | Inclusive ms/f | Inclusive GC KiB/f | 최대 단일 호출 ms |
| --- | ---: | ---: | ---: | ---: |
| OnGUI | 2.5948 | 5.8796 | 15.672 | 3.503 |
| WaitForJobGroupID | 0.5103 | 0.6416 | 0.000 | 0.351 |
| Inl_On Record Render Graph | 0.4222 | 0.4236 | 0.000 | 0.394 |
| IMGUIContainer | 0.3590 | 6.2897 | 17.186 | 4.174 |
| GfxDeviceD3D12.WaitForLastPresentation.WaitForGPU | 0.3212 | 0.3212 | 0.000 | 32.174 |
| Material.SetPassFast | 0.2245 | 0.2377 | 0.000 | 0.157 |
| UnityEngine.IMGUIModule.dll!UnityEngine::GUIStyle.GetMeshInfo() [Invoke] | 0.2018 | 0.2042 | 0.062 | 0.431 |
| Profiler.FlushMemoryCounters | 0.1863 | 0.1863 | 0.000 | 0.452 |
| UIR.DrawChain | 0.1724 | 6.5259 | 17.186 | 4.784 |
| AssetDatabase.GetGUIDFromAssetPathV2 | 0.1057 | 0.1057 | 0.000 | 0.175 |
| GizmoManager.DrawGizmos | 0.1051 | 0.2962 | 0.574 | 0.369 |
| Assembly-CSharp.dll!::VFXBurstField.Update() [Invoke] | 0.0990 | 0.1104 | 0.000 | 0.742 |
| Inl_UniversalRenderPipeline.RenderSingleCameraInternal: SceneCamera | 0.0895 | 0.7373 | 0.000 | 1.258 |
| Assembly-CSharp.dll!::PotionStatusVisual.LateUpdate() [Invoke] | 0.0886 | 0.0933 | 0.323 | 3.205 |
| Inl_UniversalRenderPipeline.RenderSingleCameraInternal: Main Camera | 0.0836 | 0.7999 | 0.000 | 1.262 |

게임 코드 별도 보기(상위 inclusive 구간, 서로 중첩 가능):

| 함수/마커 | Inclusive ms/f | Self ms/f | Inclusive GC KiB/f | 평균 호출 us |
| --- | ---: | ---: | ---: | ---: |
| Assembly-CSharp.dll!::VFXBurstField.Update() [Invoke] | 0.1104 | 0.0990 | 0.000 | 83.92 |
| Assembly-CSharp.dll!::PotionStatusVisual.LateUpdate() [Invoke] | 0.0933 | 0.0886 | 0.323 | 32.57 |
| Assembly-CSharp.dll!::PotionProjectile.Update() [Invoke] | 0.0719 | 0.0140 | 0.583 | 186.21 |
| Assembly-CSharp.dll!::PotionAreaEffect.Update() [Invoke] | 0.0514 | 0.0224 | 0.457 | 34.19 |
| PerfDemo.CombineCommit | 0.0323 | 0.0084 | 0.069 | 14762.60 |
| Assembly-CSharp.dll!::RangedEnemyController.FixedUpdate() [Invoke] | 0.0252 | 0.0137 | 0.015 | 33.68 |
| Assembly-CSharp.dll!::PlayerMeleeModule.OnDrawGizmos() [Invoke] | 0.0240 | 0.0240 | 0.000 | 11.99 |
| Assembly-CSharp.dll!::PotionAreaVisual.Update() [Invoke] | 0.0211 | 0.0211 | 0.000 | 8.06 |
| Assembly-CSharp.dll!::StatusScreenVignette.Update() [Invoke] | 0.0186 | 0.0183 | 0.183 | 18.61 |
| Assembly-CSharp.dll!::SoundManager.Update() [Invoke] | 0.0138 | 0.0138 | 0.000 | 13.81 |

실행 원본: `C:/Users/sdkmg/AppData/LocalLow/DefaultCompany/PotionJunkie\PerformanceDemo\20261015_063635_242_b9da7b7f`

분석 폴더: `C:\Users\sdkmg\Desktop\project\2d-team01\Logs/ProfilerReports/mcp_20261015_063625_65c04795`
- repeat 1: Measure 기대/실제 916/916; `C:\Users\sdkmg\Desktop\project\2d-team01\Logs\ProfilerReports\mcp_20261015_063625_65c04795\capture_001`

### S8_Combat_Peak

![프레임 추이](../Logs/GameLoopProfiling/S8_Combat_Peak.svg)

선택 프레임 506개 / 유효 raw 1개.

기능 판정: True. 결과: PlayerDefeated. 조합 2회, 적 생성 60개, 투척 4회.

피해 이벤트: 적 174, 포션 146, 플레이어 13. 적 사망/해제 24/30. 플레이어 이동 합계 22.29. 부하 유지용 고체력 사본: False.

**허용된 플레이어 패배다. 웨이브 완료/승리 실행과 동일 작업량으로 비교하지 않는다.**

CPU 상위 Self 비용(메인 스레드, inclusive 값 중첩 합산 금지):

| 마커 | Self ms/f | Inclusive ms/f | Inclusive GC KiB/f | 최대 단일 호출 ms |
| --- | ---: | ---: | ---: | ---: |
| OnGUI | 2.9022 | 6.5010 | 16.678 | 5.061 |
| WaitForJobGroupID | 0.6801 | 0.7946 | 0.000 | 0.437 |
| Inl_On Record Render Graph | 0.4452 | 0.4467 | 0.000 | 0.446 |
| IMGUIContainer | 0.4056 | 6.9625 | 18.245 | 5.687 |
| Material.SetPassFast | 0.2526 | 0.2675 | 0.000 | 0.161 |
| UnityEngine.IMGUIModule.dll!UnityEngine::GUIStyle.GetMeshInfo() [Invoke] | 0.2337 | 0.2354 | 0.033 | 0.254 |
| Assembly-CSharp.dll!::PotionStatusVisual.LateUpdate() [Invoke] | 0.2286 | 0.2447 | 3.105 | 3.092 |
| Profiler.FlushMemoryCounters | 0.2021 | 0.2022 | 0.000 | 0.501 |
| UIR.DrawChain | 0.1874 | 7.2210 | 18.245 | 5.732 |
| Mono.JIT | 0.1608 | 0.1615 | 0.005 | 31.893 |
| GizmoManager.DrawGizmos | 0.1485 | 0.4086 | 0.574 | 0.659 |
| AssetDatabase.GetGUIDFromAssetPathV2 | 0.1154 | 0.1154 | 0.000 | 0.087 |
| Inl_UniversalRenderPipeline.RenderSingleCameraInternal: SceneCamera | 0.0941 | 0.7446 | 0.000 | 1.271 |
| Inl_UniversalRenderPipeline.RenderSingleCameraInternal: Main Camera | 0.0898 | 0.8880 | 0.000 | 1.531 |
| RenderTreeManager.Process | 0.0857 | 0.1295 | 0.000 | 0.162 |

게임 코드 별도 보기(상위 inclusive 구간, 서로 중첩 가능):

| 함수/마커 | Inclusive ms/f | Self ms/f | Inclusive GC KiB/f | 평균 호출 us |
| --- | ---: | ---: | ---: | ---: |
| Assembly-CSharp.dll!::PotionStatusVisual.LateUpdate() [Invoke] | 0.2447 | 0.2286 | 3.105 | 14.34 |
| Assembly-CSharp.dll!::PotionProjectile.Update() [Invoke] | 0.1844 | 0.0256 | 1.303 | 309.91 |
| Assembly-CSharp.dll!::ExplodingEnemyController.FixedUpdate() [Invoke] | 0.1462 | 0.0855 | 0.206 | 38.40 |
| Assembly-CSharp.dll!::RangedEnemyController.FixedUpdate() [Invoke] | 0.1330 | 0.0851 | 0.091 | 33.77 |
| PerfDemo.CombineCommit | 0.1098 | 0.0213 | 0.360 | 27771.90 |
| Assembly-CSharp.dll!::PotionAreaEffect.Update() [Invoke] | 0.0942 | 0.0482 | 1.365 | 76.67 |
| Assembly-CSharp.dll!::VFXBurstField.Update() [Invoke] | 0.0815 | 0.0716 | 0.000 | 86.97 |
| Assembly-CSharp.dll!::WaveController.RunEncounter() [Coroutine: MoveNext] [Invoke] | 0.0585 | 0.0130 | 0.984 | 72.94 |
| Assembly-CSharp.dll!::PotionAreaEffect.OnTriggerEnter2D() [Invoke] | 0.0495 | 0.0118 | 0.422 | 269.33 |
| Assembly-CSharp.dll!::PotionEffectReceiver.Update() [Invoke] | 0.0330 | 0.0241 | 2.161 | 1.94 |

실행 원본: `C:/Users/sdkmg/AppData/LocalLow/DefaultCompany/PotionJunkie\PerformanceDemo\20261015_063713_790_60d40ba2`

분석 폴더: `C:\Users\sdkmg\Desktop\project\2d-team01\Logs/ProfilerReports/mcp_20261015_063703_2d98ca70`
- repeat 1: Measure 기대/실제 508/508; `C:\Users\sdkmg\Desktop\project\2d-team01\Logs\ProfilerReports\mcp_20261015_063703_2d98ca70\capture_001`

### S8_Combat_Representative

![프레임 추이](../Logs/GameLoopProfiling/S8_Combat_Representative.svg)

선택 프레임 755개 / 유효 raw 1개.

기능 판정: True. 결과: PlayerDefeated. 조합 2회, 적 생성 20개, 투척 4회.

피해 이벤트: 적 79, 포션 59, 플레이어 17. 적 사망/해제 15/17. 플레이어 이동 합계 28.37. 부하 유지용 고체력 사본: False.

**허용된 플레이어 패배다. 웨이브 완료/승리 실행과 동일 작업량으로 비교하지 않는다.**

CPU 상위 Self 비용(메인 스레드, inclusive 값 중첩 합산 금지):

| 마커 | Self ms/f | Inclusive ms/f | Inclusive GC KiB/f | 최대 단일 호출 ms |
| --- | ---: | ---: | ---: | ---: |
| OnGUI | 2.7719 | 6.1860 | 16.530 | 3.390 |
| WaitForJobGroupID | 0.6048 | 0.7287 | 0.000 | 0.347 |
| Inl_On Record Render Graph | 0.4286 | 0.4301 | 0.000 | 0.539 |
| GfxDeviceD3D12.WaitForLastPresentation.WaitForGPU | 0.3970 | 0.3970 | 0.000 | 16.962 |
| IMGUIContainer | 0.3770 | 6.6165 | 18.090 | 4.025 |
| Material.SetPassFast | 0.2387 | 0.2531 | 0.000 | 0.194 |
| UnityEngine.IMGUIModule.dll!UnityEngine::GUIStyle.GetMeshInfo() [Invoke] | 0.2201 | 0.2216 | 0.026 | 0.278 |
| Profiler.FlushMemoryCounters | 0.1934 | 0.1934 | 0.000 | 0.400 |
| UIR.DrawChain | 0.1750 | 6.8568 | 18.090 | 4.410 |
| Assembly-CSharp.dll!::PotionStatusVisual.LateUpdate() [Invoke] | 0.1382 | 0.1425 | 1.124 | 1.523 |
| GizmoManager.DrawGizmos | 0.1137 | 0.3329 | 0.574 | 0.354 |
| AssetDatabase.GetGUIDFromAssetPathV2 | 0.1099 | 0.1099 | 0.000 | 0.163 |
| Mono.JIT | 0.0976 | 0.0981 | 0.003 | 34.081 |
| Assembly-CSharp.dll!::VFXBurstField.Update() [Invoke] | 0.0936 | 0.1048 | 0.000 | 0.617 |
| Inl_UniversalRenderPipeline.RenderSingleCameraInternal: SceneCamera | 0.0916 | 0.7457 | 0.000 | 1.353 |

게임 코드 별도 보기(상위 inclusive 구간, 서로 중첩 가능):

| 함수/마커 | Inclusive ms/f | Self ms/f | Inclusive GC KiB/f | 평균 호출 us |
| --- | ---: | ---: | ---: | ---: |
| Assembly-CSharp.dll!::PotionStatusVisual.LateUpdate() [Invoke] | 0.1425 | 0.1382 | 1.124 | 24.34 |
| Assembly-CSharp.dll!::VFXBurstField.Update() [Invoke] | 0.1048 | 0.0936 | 0.000 | 80.40 |
| Assembly-CSharp.dll!::PotionProjectile.Update() [Invoke] | 0.0887 | 0.0166 | 0.761 | 217.49 |
| Assembly-CSharp.dll!::PotionAreaEffect.Update() [Invoke] | 0.0704 | 0.0324 | 0.831 | 46.80 |
| PerfDemo.CombineCommit | 0.0405 | 0.0110 | 0.136 | 15300.40 |
| Assembly-CSharp.dll!::RangedEnemyController.FixedUpdate() [Invoke] | 0.0387 | 0.0207 | 0.029 | 34.12 |
| Assembly-CSharp.dll!::ExplodingEnemyController.FixedUpdate() [Invoke] | 0.0378 | 0.0171 | 0.059 | 36.06 |
| Assembly-CSharp.dll!::PlayerMeleeModule.OnDrawGizmos() [Invoke] | 0.0248 | 0.0248 | 0.000 | 12.40 |
| Assembly-CSharp.dll!::WaveController.RunEncounter() [Coroutine: MoveNext] [Invoke] | 0.0224 | 0.0052 | 0.300 | 35.16 |
| Assembly-CSharp.dll!::StatusScreenVignette.Update() [Invoke] | 0.0213 | 0.0208 | 0.503 | 21.32 |

실행 원본: `C:/Users/sdkmg/AppData/LocalLow/DefaultCompany/PotionJunkie\PerformanceDemo\20261015_063748_047_25d17bfb`

분석 폴더: `C:\Users\sdkmg\Desktop\project\2d-team01\Logs/ProfilerReports/mcp_20261015_063737_55520fa2`
- repeat 1: Measure 기대/실제 757/757; `C:\Users\sdkmg\Desktop\project\2d-team01\Logs\ProfilerReports\mcp_20261015_063737_55520fa2\capture_001`

### S9_Combat_Lifecycle

![프레임 추이](../Logs/GameLoopProfiling/S9_Combat_Lifecycle.svg)

선택 프레임 7,873개 / 유효 raw 10개.

기능 판정: True. 결과: CombatCompleted. 조합 20회, 적 생성 60개, 투척 40회.

피해 이벤트: 적 289, 포션 179, 플레이어 145. 적 사망/해제 60/60. 플레이어 이동 합계 257.01. 부하 유지용 고체력 사본: False.

CPU 상위 Self 비용(메인 스레드, inclusive 값 중첩 합산 금지):

| 마커 | Self ms/f | Inclusive ms/f | Inclusive GC KiB/f | 최대 단일 호출 ms |
| --- | ---: | ---: | ---: | ---: |
| GfxDeviceD3D12.WaitForLastPresentation.WaitForGPU | 3.7899 | 3.7899 | 0.000 | 511.375 |
| OnGUI | 2.4734 | 5.6248 | 15.480 | 4.899 |
| Semaphore.WaitForSignal | 0.7401 | 0.7401 | 0.000 | 506.055 |
| WaitForJobGroupID | 0.4971 | 0.6118 | 0.000 | 0.596 |
| Inl_On Record Render Graph | 0.4063 | 0.4077 | 0.000 | 0.726 |
| IMGUIContainer | 0.3430 | 6.0166 | 16.986 | 6.272 |
| Material.SetPassFast | 0.2138 | 0.2260 | 0.000 | 0.303 |
| UnityEngine.IMGUIModule.dll!UnityEngine::GUIStyle.GetMeshInfo() [Invoke] | 0.1897 | 0.1904 | 0.013 | 2.280 |
| Profiler.FlushMemoryCounters | 0.1827 | 0.1827 | 0.000 | 0.708 |
| UIR.DrawChain | 0.1623 | 6.2405 | 16.986 | 6.365 |
| AssetDatabase.GetGUIDFromAssetPathV2 | 0.1018 | 0.1018 | 0.000 | 0.268 |
| GizmoManager.DrawGizmos | 0.0987 | 0.2811 | 0.574 | 1.137 |
| Assembly-CSharp.dll!::VFXBurstField.Update() [Invoke] | 0.0893 | 0.0986 | 0.000 | 0.728 |
| GameView.Paint | 0.0867 | 3.5881 | 9.019 | 511.531 |
| Inl_UniversalRenderPipeline.RenderSingleCameraInternal: SceneCamera | 0.0853 | 0.7140 | 0.000 | 1.415 |

게임 코드 별도 보기(상위 inclusive 구간, 서로 중첩 가능):

| 함수/마커 | Inclusive ms/f | Self ms/f | Inclusive GC KiB/f | 평균 호출 us |
| --- | ---: | ---: | ---: | ---: |
| Assembly-CSharp.dll!::VFXBurstField.Update() [Invoke] | 0.0986 | 0.0893 | 0.000 | 73.11 |
| Assembly-CSharp.dll!::PotionStatusVisual.LateUpdate() [Invoke] | 0.0769 | 0.0752 | 0.331 | 28.55 |
| Assembly-CSharp.dll!::PotionProjectile.Update() [Invoke] | 0.0579 | 0.0122 | 0.303 | 161.86 |
| Assembly-CSharp.dll!::PotionAreaEffect.Update() [Invoke] | 0.0255 | 0.0169 | 0.461 | 16.50 |
| Assembly-CSharp.dll!::RangedEnemyController.FixedUpdate() [Invoke] | 0.0250 | 0.0129 | 0.017 | 28.80 |
| PerfDemo.CombineCommit | 0.0237 | 0.0061 | 0.092 | 9325.09 |
| Assembly-CSharp.dll!::PlayerMeleeModule.OnDrawGizmos() [Invoke] | 0.0229 | 0.0229 | 0.000 | 11.45 |
| Assembly-CSharp.dll!::PotionAreaVisual.Update() [Invoke] | 0.0203 | 0.0202 | 0.000 | 7.60 |
| Assembly-CSharp.dll!::StatusScreenVignette.Update() [Invoke] | 0.0175 | 0.0172 | 0.162 | 17.48 |
| Assembly-CSharp.dll!::SoundManager.Update() [Invoke] | 0.0139 | 0.0139 | 0.000 | 13.88 |

실행 원본: `C:/Users/sdkmg/AppData/LocalLow/DefaultCompany/PotionJunkie\PerformanceDemo\20261015_063855_673_f485ed48`

분석 폴더: `C:\Users\sdkmg\Desktop\project\2d-team01\Logs/ProfilerReports/mcp_20261015_063844_ca5bf632`
- repeat 1: Measure 기대/실제 812/812; `C:\Users\sdkmg\Desktop\project\2d-team01\Logs\ProfilerReports\mcp_20261015_063844_ca5bf632\capture_001`
- repeat 2: Measure 기대/실제 941/941; `C:\Users\sdkmg\Desktop\project\2d-team01\Logs\ProfilerReports\mcp_20261015_063844_ca5bf632\capture_002`
- repeat 3: Measure 기대/실제 807/807; `C:\Users\sdkmg\Desktop\project\2d-team01\Logs\ProfilerReports\mcp_20261015_063844_ca5bf632\capture_003`
- repeat 4: Measure 기대/실제 650/650; `C:\Users\sdkmg\Desktop\project\2d-team01\Logs\ProfilerReports\mcp_20261015_063844_ca5bf632\capture_004`
- repeat 5: Measure 기대/실제 823/823; `C:\Users\sdkmg\Desktop\project\2d-team01\Logs\ProfilerReports\mcp_20261015_063844_ca5bf632\capture_005`
- repeat 6: Measure 기대/실제 757/757; `C:\Users\sdkmg\Desktop\project\2d-team01\Logs\ProfilerReports\mcp_20261015_063844_ca5bf632\capture_006`
- repeat 7: Measure 기대/실제 834/834; `C:\Users\sdkmg\Desktop\project\2d-team01\Logs\ProfilerReports\mcp_20261015_063844_ca5bf632\capture_007`
- repeat 8: Measure 기대/실제 884/884; `C:\Users\sdkmg\Desktop\project\2d-team01\Logs\ProfilerReports\mcp_20261015_063844_ca5bf632\capture_008`
- repeat 9: Measure 기대/실제 757/757; `C:\Users\sdkmg\Desktop\project\2d-team01\Logs\ProfilerReports\mcp_20261015_063844_ca5bf632\capture_009`
- repeat 10: Measure 기대/실제 628/628; `C:\Users\sdkmg\Desktop\project\2d-team01\Logs\ProfilerReports\mcp_20261015_063844_ca5bf632\capture_010`
