# 상태 실행기 검증

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

내부 기본 에셋 6+14종의 이름·핵심 수치·GUID, 외부 DB의 6+15종 참조, 장판 프리팹의 실행 컴포넌트/DB 참조, 플레이어 Receiver 연결을 검사한다. 새 데이터의 투척 거리 8·비행 속도 10은 외부 플레이어 투척 흐름에 아직 연결되지 않았으므로 실패가 아닌 인계 항목으로 출력한다.
