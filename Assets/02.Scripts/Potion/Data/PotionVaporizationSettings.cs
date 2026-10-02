using UnityEngine;

[CreateAssetMenu(fileName = "PotionVaporizationSettings", menuName = "Potion/Vaporization Settings")]
public sealed class PotionVaporizationSettings : ScriptableObject
{
    [Header("기화 단독 표시")]
    [Tooltip("반응하지 않은 기화 장판이 표시되며 다른 장판과의 반응을 기다리는 시간(초).")]
    [SerializeField, Min(0f)] private float _waitingDuration = 2f;

    [Header("기화 반응 VFX")]
    [Tooltip("대상 장판이 제거된 영역에서 재생할 프리팹. SpriteSheetPlayer와 SpriteRenderer가 필요하며 PotionAreaVfxSettings로 버스트를 설정합니다. 비워두면 반응 VFX를 생성하지 않습니다.")]
    [SerializeField] private GameObject _reactionVfxPrefab;
    [Tooltip("즉발 피해·힐과 별개로 반응 VFX 오브젝트를 유지하는 시간(초).")]
    [SerializeField, Min(0f)] private float _reactionVfxDuration = 1f;

    public float WaitingDuration => Mathf.Max(0f, _waitingDuration);
    public GameObject ReactionVfxPrefab => _reactionVfxPrefab;
    public float ReactionVfxDuration => Mathf.Max(0f, _reactionVfxDuration);
}
