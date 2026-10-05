using UnityEngine;

[CreateAssetMenu(fileName = "PotionVaporizationSettings", menuName = "Potion/Vaporization Settings")]
public sealed class PotionVaporizationSettings : ScriptableObject
{
    [Header("기화 단독 표시")]
    [Tooltip("반응하지 않은 기화 장판이 표시되며 다른 장판과의 반응을 기다리는 시간(초).")]
    [SerializeField, Min(0f)] private float _waitingDuration = 2f;

    [Header("조합 장판 + 기화 반응 VFX")]
    [Tooltip("이미 두 원소가 조합된 장판에 기화를 적용할 때 재생할 프리팹. 단일 원소 + 기화는 해당 조합 데이터의 AreaVfxPrefab을 사용합니다. SpriteSheetPlayer와 SpriteRenderer가 필요하며 PotionAreaVfxSettings로 버스트를 설정합니다. 비워두면 조합 장판의 기화 반응 VFX를 생성하지 않습니다.")]
    [SerializeField] private GameObject _reactionVfxPrefab;
    [Tooltip("단일 원소 및 조합 장판의 기화 반응 VFX 오브젝트를 유지하는 시간(초). 즉발 피해·힐과 별개입니다.")]
    [SerializeField, Min(0f)] private float _reactionVfxDuration = 1f;

    public float WaitingDuration => Mathf.Max(0f, _waitingDuration);
    public GameObject ReactionVfxPrefab => _reactionVfxPrefab;
    public float ReactionVfxDuration => Mathf.Max(0f, _reactionVfxDuration);
}
