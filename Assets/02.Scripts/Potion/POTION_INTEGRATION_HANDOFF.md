# 포션 통합 인계

작성일: 2026-09-29  
기준: 포션 구현 계획 8단계

## 현재 런타임 연결

- 외부 `PotionDatabase`의 기존 단일 포션 6종은 그대로 사용한다. 각 레거시 에셋의 원소 enum을 키로 `Resources/SinglePotionDefaults/{Element}`를 조회하므로 새 착탄·장판·잔류·섭취 수치가 적용된다.
- 외부 `PotionCombinationDatabase`의 기존 조합 15종과 `PotionGroundArea.prefab`의 DB 참조는 그대로 사용한다. 활성 조합 14종은 조합 enum을 키로 `Resources/CombinationDefaults/{Combination}`을 조회한다.
- `PurifyingLife`는 새 기획의 활성 조합에서 제외했다. 직렬화 호환을 위해 외부 DB와 enum ID 5는 보존하지만 내부 기본 프로필은 만들지 않았다.
- `PotionGroundArea.prefab`에는 `PotionAreaEffect`, `PotionAreaCombiner`, `PotionCombinedAreaShape`와 조합 DB 참조가 연결되어 있다.
- `Player.prefab`에는 `PotionEffectReceiver`가 연결되어 있어 플레이어 HP·갈증·이동 제한 경로를 사용한다.

## 외부 연결 대기

| 항목 | 현재 상태 | 필요한 작업 |
|---|---|---|
| 투척 거리 | 새 데이터 기본값은 8이지만 `Player.prefab`/`PlayerStatus`는 6 | 조준 제한과 미리보기가 `PotionData.MaxThrowDistance`를 사용하도록 외부 투척 흐름을 설계하거나 프리팹 정책을 확정 |
| 비행 속도 | 새 데이터 기본값은 10이지만 `PlayerThrowingModule`/프리팹은 8 | 비행 시간 계산이 `PotionData.FlightSpeed`를 사용하도록 외부 호출부 연결 |
| 충돌 착탄 | 목표점 도착 방식 유지 | 벽·대상 충돌 Layer 정책을 확정한 뒤 `PotionProjectile` 충돌 경로 추가 |
| 적 대상 | 프로젝트에서 적 `IDamageable`/이동 구현체를 찾지 못함 | 적 루트에 `PotionEffectReceiver(TargetKind=Enemy)`를 배치하고 이동 속도에 `GetMovementSpeed` 사용 |
| Unity 실플레이 | 에디터 라이선스 부재로 미실시 | Trigger 순서, 타원 Collider, VFX, 프리팹 배치, 실제 입력·대시 변위를 플레이 모드에서 확인 |

외부 연결 대기 항목은 현재 Potion 내부 수치 실행과 별개다. 연결 전에는 새 투척 거리·속도와 실제 적 적용을 플레이 반영 완료로 표시하지 않는다.

## 재현 검증

프로젝트 루트에서 다음을 실행한다.

```powershell
powershell -ExecutionPolicy Bypass -File Assets/02.Scripts/Potion/Tests~/ValidatePotionAssets.ps1

$env:DOTNET_CLI_HOME = Join-Path (Get-Location) 'Assets/02.Scripts/Potion/Tests~/.dotnet'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_GENERATE_ASPNET_CERTIFICATE = 'false'
dotnet run --project Assets/02.Scripts/Potion/Tests~/StatusRuntime/StatusRuntime.csproj --verbosity quiet
```

에셋 검증은 내부 기본 에셋 6+14종의 이름·핵심 수치·GUID, 외부 DB의 6+15종 참조, 장판 프리팹의 실행 컴포넌트/DB 연결, 플레이어 Receiver 연결을 검사한다. 상태 검증은 시간·수치·상태·조합 계약 19개 시나리오를 실행한다. 두 검증 모두 Unity 물리와 실제 프리팹 실행 순서를 대신하지 않는다.
