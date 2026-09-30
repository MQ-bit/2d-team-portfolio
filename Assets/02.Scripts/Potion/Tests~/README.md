# 상태 실행기 검증

## 데이터 통합 (2026-09-30)

효과 설정은 `PotionData`와 `PotionCombinationData`에 직접 저장한다. 기존 중첩 프로필의 값을 우선 이관했으며, 공통 실행기는 `IPotionEffectData`로 두 에셋을 읽는다. 구형 효과 수치와 기본 프로필 폴백은 제거했다. 갈증 변화량은 단일 포션에만 있고, 포션별 비행 속도·최대 투척 거리 필드는 없다. 실제 투척 거리 설정은 기존 `PlayerStatus`가 담당한다.

이번 변경은 Unity 참조를 사용한 전체 C# 소스 컴파일, 20개 결정적 시나리오, 41개 에셋 구조 검사로 확인했다. 기존 프로필이 있던 40개 에셋의 효과 값·시각 정보·식별 정보 보존도 확인했다. 아래 Unity 플레이 검증 기록은 이전 실행 결과이며 이번 변경 후 다시 실행한 결과는 아니다.

## Unity 실제 프리팹 통합 검증 (2026-09-30 추가)

`Validation/Editor/PotionValidationBatch.cs`는 Unity 플레이 모드에서 기존 `07.Datas` 에셋과 `03.Prefabs` 프리팹을 로드한다. 실제 Physics2D Trigger, 중복 Collider, 장판 수명/잔류, 6종 섭취, 14종 조합, 흡혈, 기화, 서리 단계, 재진입, PlayerStatus와 투사체 연결, Collider와 SpriteRenderer의 크기를 검사한다. 적 대신 `PotionValidationTarget` 인터페이스 구현을 사용한다. 이 대상은 에디터 전용이며 빌드에는 포함되지 않는다.

열린 프로젝트와 충돌하지 않도록 Assets/Packages/ProjectSettings를 복사한 별도 프로젝트에서 실행한다. 테스트는 빈 씬에서 실행하고 씬이나 프리팹을 저장하지 않는다. 원본 Library는 실행 중인 에디터의 프로세스 정보를 포함하므로 복사하지 않는다.

```powershell
& 'C:/Program Files/Unity/Hub/Editor/6000.3.22f1/Editor/Unity.exe' `
  -batchmode -nographics -projectPath '<검증용 프로젝트 절대 경로>' `
  -executeMethod PotionValidationBatch.Run -logFile '<로그 절대 경로>'
```

`-quit`은 지정하지 않는다. 플레이 검증이 끝나면 자동 종료하며 실패가 있으면 종료 코드 1을 반환한다. 결과는 검증 프로젝트의 `Assets/02.Scripts/Potion/Tests~/unity-integration-results.txt`에 기록된다. 해당 폴더를 먼저 만든다. `-nographics` 실행이므로 크기 비교는 Renderer bounds 기준이며 렌더링된 화면·VFX·입력 애니메이션의 시각 검증을 대신하지 않는다.

현재 결과와 한계는 `../POTION_TEST_REPORT.md`를 참조한다.

## .NET 결정적 상태 검증

실행 위치는 프로젝트 루트다. .NET 9 이상 SDK가 필요하며 NuGet 패키지는 사용하지 않는다.

```powershell
$env:DOTNET_CLI_HOME = Join-Path (Get-Location) 'Assets/02.Scripts/Potion/Tests~/.dotnet'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_GENERATE_ASPNET_CERTIFICATE = 'false'
dotnet run --project Assets/02.Scripts/Potion/Tests~/StatusRuntime/StatusRuntime.csproj --verbosity quiet
```

`Tests~`는 Unity 임포트에서 제외된다. 상태 실행기, Receiver, 수치 전달부, PlayerStatus의 실제 소스를 직접 컴파일한다. Unity의 Time/MonoBehaviour/Mathf 등은 테스트 대역을 사용한다. 따라서 이 검증은 결정적 시간·수치·인터페이스 계약 검증이며 실제 Unity 물리, 프리팹 배치, MonoBehaviour 실행 순서 검증을 대신하지 않는다. Unity 참조를 사용하는 전체 솔루션 빌드는 Rider에서 별도로 확인한다.

검증: DoT 종료 경계와 불완전 주기, 서로 다른 상태 동시 피해, 비중첩 갱신, 강한 효과/출처 유지, 잔류 독립 수명, 서리 스택·겹침·빙결, 단발/주기 마비, 둔화 출처 해제, 사망, 과잉 피해의 실제 HP 감소량, 비활성화 초기화, 피해 콜백 재진입, 플레이어 이동/대시, HP·갈증 인터페이스 수치 전달, 단일 장판 5종 총량·수명 경계, 중복 대상 진입·이탈 1회 처리, 6종 섭취 수치·갈증 방향·투척 필터 분리·기화 상태 유지, 지속형 6조합의 착탄·복수 원소 Tick·빙결·마비·잔류 상태·총 피해, 흡혈 4종의 원본 효과 상속·실제 피해 회복·잔류 시전자와 기화 4속성의 30 즉발 피해.

적 구현은 현재 프로젝트에서 발견되지 않았다. FakeHealth는 Player/Enemy 어느 쪽도 가정하지 않는 계약 검증 대상이며 실제 적 이동 연결 완료를 의미하지 않는다.

## 에셋·통합 연결 검증

```powershell
powershell -ExecutionPolicy Bypass -File Assets/02.Scripts/Potion/Tests~/ValidatePotionAssets.ps1
```

내부 기본 에셋 6+14종의 이름·핵심 수치·GUID, 외부 DB의 6+15종 참조, 장판 프리팹의 실행 컴포넌트/DB 참조, 플레이어 Receiver 연결을 검사한다. 모든 포션 에셋의 통합 필드, 중복 키와 구형 필드 제거, 조합 갈증 필드 제거도 확인한다. 외부 에셋의 커스텀 수치는 기본 에셋과 달라도 허용한다.
