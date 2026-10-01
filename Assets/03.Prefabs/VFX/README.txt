GameVFX - 장판 스킬 이펙트 15종

[사용법]
- 03.Prefabs/VFX/ 의 프리팹을 (텍스처: 04.Sprites/VFX, 스크립트: 02.Scripts/VFX/SpriteSheetPlayer.cs) 장판 오브젝트의 자식으로 넣으면 장판이 켜져 있는 동안 루프 재생됩니다.
- 스케일 1 = 폭 1유닛. localScale 에 장판 지름(유닛)을 넣으면 크기가 맞습니다.
- 속도는 SpriteSheetPlayer.fps (기본 15).

[포션 장판 연결]
- PotionData/PotionCombinationData의 Visual Data > Area Vfx Prefab에 원본 프리팹을 지정합니다.
- PotionGroundArea는 VFXBurstField를 생성해 장판 안의 여러 위치에서 버스트를 반복 재생합니다. 바닥 이미지는 유지합니다.
- PotionGroundArea의 Area VFX Bursts에서 개수(기본 12), 개별 크기(기본 0.4유닛), 배치 범위 비율(기본 0.85)을 조절합니다.
- 단일 장판은 원형, 조합 장판은 실제 타원 크기와 방향을 사용합니다. 개별 VFX는 원래 비율을 유지합니다.
- 조합 시 원본 버스트를 제거하고 조합 VFX로 교체하며, 장판이 제거되면 버스트도 함께 제거됩니다.
- VFXFieldTestController는 테스트 씬의 프리팹 전환용으로 유지합니다. 실제 포션 장판에는 추가할 필요가 없습니다.

[원본 매핑]
생명        Super Pixel Effects Gigapack / round_sparkle_burst_002_large_green
생명+화염   Super Pixel Effects Gigapack / round_sparkle_burst_003_large_red
생명+독     Effect and FX Pixel / Part 13 / 633.png row 4
생명+서리   Super Pixel Effects Gigapack / round_sparkle_burst_001_large_blue
생명+전기   Super Pixel Effects Gigapack / spell_absorb_001_large_violet
화염        Effect and FX Pixel / Part 10 / 464.png row 0
화염+독     Super Pixel Effects Gigapack / spell_poison_001_large_green
화염+서리   Effect and FX Pixel / Part 13 / 615.png row 4
화염+전기   Effect and FX Pixel / Part 4 / 176.png row 1
독          Super Pixel Effects Gigapack / status_poison_001_large_green
독+서리     Effect and FX Pixel / Part 14 / 655.png row 6
독+전기     Effect and FX Pixel / Part 14 / 653.png row 8
서리        Effect and FX Pixel / Part 11 / 529.png row 2
서리+전기   Effect and FX Pixel / Part 13 / 623.png row 5
전기        Super Pixel Effects Gigapack / lightning_burst_002_large_violet

[라이선스 / 크레딧]
- Super Pixel Effects Gigapack: 게임에 포함 OK(상업 포함), 크레딧 표기 필요.
  예) "Pixel Art Assets - Will Tice / unTied Games"  (http://untiedgames.com/files/license.txt)
  에셋 자체 재배포(에셋스토어 업로드 등) 금지.
- Effect and FX Pixel All Free: 다운로드 페이지의 라이선스 조건 확인 필요.
