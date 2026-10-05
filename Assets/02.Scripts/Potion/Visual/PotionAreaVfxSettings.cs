using UnityEngine;

[DisallowMultipleComponent]
public sealed class PotionAreaVfxSettings : MonoBehaviour
{
    [Header("장판 버스트")]
    [Tooltip("장판에 배치할 VFX 개수입니다. 반복 재생 시 각 VFX는 재생이 끝나면 새 위치에서 다시 재생됩니다.")]
    [Min(1)] public int burstCount = 12;
    [Tooltip("VFX 하나의 가로 크기입니다(월드 단위). fillArea가 켜져 있으면 이 값 대신 장판 너비 × fillScale을 사용합니다.")]
    [Min(0.05f)] public float burstSize = 0.4f;
    [Tooltip("조합 VFX 강화 시에도 burstSize를 유지합니다. 작은 이펙트를 여러 개 배치할 때 사용합니다.")]
    public bool preserveBurstSizeOnCombination;
    [Tooltip("VFX 크기의 무작위 편차 비율입니다. 0.3이면 burstSize의 70~130%로 생성됩니다. fillArea가 켜져 있으면 적용하지 않습니다.")]
    [Range(0f, 0.9f)] public float sizeJitter = 0.3f;
    [Tooltip("VFX 생성 위치가 퍼지는 범위 비율입니다. 1이면 장판 전체, 값이 작을수록 중앙에 모입니다. VFX 크기와 효과 판정 범위는 바뀌지 않습니다.")]
    [Range(0.1f, 1f)] public float spawnAreaRatio = 0.85f;
    [Tooltip("반복 재생 시 다음 버스트까지의 대기 시간 범위입니다(초). X는 최솟값, Y는 최댓값입니다. playOnce가 켜져 있으면 적용하지 않습니다.")]
    public Vector2 restDelayRange = new(0f, 0.08f);
    [Tooltip("VFX 이미지와 프리팹 색상에 곱할 색입니다. 흰색은 원래 색을 유지하며, 알파는 투명도를 조절합니다. 장판 바탕색에는 영향을 주지 않습니다.")]
    public Color tint = Color.white;

    [Tooltip("VFX의 하단 중앙을 장판 안의 생성 위치에 맞춥니다. 불처럼 위로 솟는 이펙트에 사용합니다.")]
    public bool anchorAtBottom;

    [Tooltip("장판 너비 × fillScale로 VFX 크기를 정합니다. 버스트가 하나면 중앙에 고정합니다.")]
    public bool fillArea;
    [Tooltip("fillArea가 켜져 있을 때 장판 너비 대비 VFX 가로 크기 비율입니다. 1이면 장판 너비와 같고, 0.5이면 절반입니다.")]
    [Min(0.05f)] public float fillScale = 1f;
    [Tooltip("모든 버스트를 처음부터 동시에 한 번 재생하고 다시 생성하지 않습니다.")]
    public bool playOnce;
}
