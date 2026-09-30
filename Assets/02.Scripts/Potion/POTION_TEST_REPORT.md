# 포션 기획 대비 실제 Unity 검증

2026-09-30 / Unity 6000.3.22f1 / 기획 기준: POTION_IMPLEMENTATION_PLAN.md

## 판정

**전체 완료 판정은 실패. 자동 검증 136개 항목 중 117개 통과, 19개 실패.**
19개 실패는 투척 거리·속도 2개와 장판 표시 크기 17개로 구성된다. 피해·섭취·상태·조합 수치 검증은 통과했다. 이번 요청은 테스트이므로 게임 동작과 프리팹 수치는 변경하지 않았다.

## 재현된 불일치

| 항목 | 기획/검사 기준 | 실제 | 근거 |
|---|---|---|---|
| 투척 최대 거리 | 8 | 6 | Player.prefab의 PlayerStatus 및 PlayerThrowingModule.ClampToThrowRange가 사용하는 값 |
| 비행 속도 | 10 | 8 | Player.prefab의 _throwSpeed, ThrowPotion의 distance / _throwSpeed |
| 단일 지속 장판 5종 표시 | 판정 지름 4와 일치 | SpriteRenderer bounds 지름 1.28 | 생명·화염·독·서리·전기 모두 재현 |
| 지속 조합 6종 표시 | 중심 거리 2, 반경 2에서 타원 지름 6 × 5.656854 | 표시 3.28 × 1.28 | 각 조합에서 가로·세로 2개씩 실패. PolygonCollider 자체 치수는 통과 |

소스상 원인: `PotionGroundArea.Initialize`는 Collider 반경을 갱신하지만 SpriteRenderer의 크기를 반경에 맞추지 않는다. `PotionCombinedAreaShape.CreateEllipseVisual`은 원본 렌더러 크기로 visualDiameter를 계산하고 단축에도 그 값을 사용한다. 따라서 Collider에서 계산한 타원과 표시 크기가 다르다. 표시 검사는 렌더러 경계값 기준이며 이미지의 투명 여백이나 실제 픽셀 화면까지 판정한 것은 아니다.

## 통과한 실행 검증

- 기존 07.Datas의 레거시 에셋 → Resources 기본 프로필 로딩 → 실제 PotionGroundArea.prefab 경로.
- 실제 Potion.prefab 투사체의 착탄 → 장판 → 잔류 피해(화염 총 40), 투사체 제거.
- 단일 장판 6종의 반경, 필터, 수명 종료, 두 Collider를 가진 동일 대상의 피해 중복 방지. 생명 회복 24, 화염 40/독 40(잔류 포함), 서리 20, 전기 24, 기화 25.
- 섭취 6종: 생명 회복 30, 화염 피해 24, 독 25, 서리 15, 전기 20, 기화 15. 갈증 Delta -40/-30/-30/-30/-30/+30.
- 지속 조합 6종: 양쪽 원본 선점, 32정점 PolygonCollider와 타원 치수, 착탄+장판+잔류 합계 73/45/80/40/40/48.
- 흡혈 4종: 잔류를 포함한 피해 40/40/20/24, 시전자 회복 8/8/4/4.8.
- 기화 조합 4종: 다중 Collider에도 피해 30 한 번, 잔류 없음, 두 장판 제거.
- 실제 물리 진입 후 서리 1스택 ×0.85 → 2스택 ×0.70 → 3번째 Tick에서 스택 0 및 빙결. 이탈 후 기존 빙결 유지와 만료 해제.
- 화염 진입 피해 8, 이탈 뒤 주기 전 재진입하면 누적 16으로 즉시 추가 피해.
- 실제 PlayerStatus 연결: 서리 섭취 후 HP 85, 일반 이동/대시 속도 0, 해제 후 5/15 복구.

## 실행 환경과 한계

- 프로젝트 전체 Unity 임포트·컴파일 성공. 사용자 계정으로 실행하자 라이선스가 인식됐다. 기존 문서의 라이선스 부재는 이번 실행의 최종 장애가 아니다.
- 열린 작업 프로젝트와 별개인 `.utmp/potion-play-validation` 복사본의 빈 씬에서 `-batchmode -nographics -executeMethod PotionValidationBatch.Run`을 실행했다. 최종 실행은 예상한 불일치 때문에 종료 코드 1을 반환했다.
- 적 대신 에디터 전용 `PotionValidationTarget`을 사용했다. 실제 적 HP/이동 구현은 여전히 발견되지 않아 적 AI·이동 통합 완료로 볼 수 없다.
- Headless 실행이므로 실제 입력, 인벤토리 소비·애니메이션, 대시 변위, VFX 화면은 확인하지 않았다. 벽/대상 조기 착탄 정책도 미결정 상태다.
- 맹독화염 중독 3초, 이탈 후 빙결 유지 등은 구현의 현재 임시 정책을 확인한 것이며 기획 확정으로 취급하지 않는다.
- 로그에 UnityEditor.Search.SearchDatabase의 ArgumentOutOfRangeException이 발생했다. 포션 스택의 예외는 아니며 모든 136개 검증은 끝까지 실행됐다. 원시 로그는 Tests~/unity-integration.log에 있다.
- .NET 상태 테스트는 누락된 PotionEffectApplier/PotionAreaTimeline 참조를 복구해 **빌드 오류 0·경고 0**을 확인했다. 기존 csproj가 루트의 *.csproj 규칙으로 제외되어 있던 것도 예외 규칙으로 바로잡았다. 실행은 Windows 애플리케이션 제어(0x800711C7)에 막혔으므로 기존 19개 결정적 시나리오가 이번에 재통과했다고 주장하지 않는다.
- ValidatePotionAssets.ps1 통과: 기본 6+14 에셋, GUID, 외부 DB, 프리팹 연결. 이 검사는 거리·속도 불일치를 실패가 아닌 정보로 출력하므로 단독 성공으로 전체 기획 준수를 판단하면 안 된다.

## 재현 자료

- [전체 결과 136개](Tests~/UnityIntegrationResults-2026-09-30.txt)
- [실행 방법](Tests~/README.md)
- [실제 Unity 통합 검증 코드](Validation/Editor/PotionValidationBatch.cs)
- [검증용 대상](Validation/PotionValidationTarget.cs)

수정 권장 순서: PlayerThrowingModule의 거리·속도를 포션 데이터에 연결 → 단일 표시 크기를 반경과 동기화 → 조합 표시를 Collider와 동일한 장·단축으로 계산 → 같은 검증 재실행.

## Receiver 중복 제거 후 회귀 검증 (2026-09-30)

- 직접/상태 피해의 전달·흡혈·이벤트를 ApplyDamage로 공통화하고, 상태 갱신 전 Tick 및 생존 검사를 PrepareStatusApplication으로 공통화했다. 기존 오버로드별 후처리 시점은 보존했다.
- 미사용 Receiver._playerStatus와 Receiver.ExitFrostArea를 제거했다. PlayerStatus.DashSpeed는 Receiver.GetDashSpeed를 사용하도록 통일했다.
- Unity 재실행: **142개 중 123개 통과, 19개 기존 실패 유지**. 기존 실패 목록과 비교해 추가 실패 없음. .NET 테스트 프로젝트 빌드 오류/경고 0개.
- 새 6개 검증 통과: 직접 피해의 공통 이벤트 단독 발생, 흡혈 1회, 상태 이벤트보다 먼저 흡혈 완료, 상태→공통 이벤트 순서, 즉시 상태 피해 1회, 콜백에서 상태 해제 후 추가 피해 중단.
- [회귀 검증 전체 결과](Tests~/UnityRefactorResults-2026-09-30.txt). 투척 거리·속도 및 장판 표시 크기 문제는 이번 중복 제거 범위에서 변경하지 않았다.
