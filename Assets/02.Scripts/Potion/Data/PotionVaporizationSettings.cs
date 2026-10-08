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

    [Header("기화 반응 소리")]
    [Tooltip("기화가 다른 장판과 반응할 때 한 번 재생하는 소리. 어떤 장판과 반응해도 같은 소리를 쓴다.")]
    [SerializeField] private AudioClip _reactionSound;
    [SerializeField, Range(0f, 1f)] private float _reactionSoundVolume = 1f;

    [Header("기화 반응 카메라 쉐이크")]
    [Tooltip("기화 반응의 첫 충격 강도. 여러 장판과 동시에 반응해도 연출은 한 번만 시작합니다. 0이면 흔들지 않습니다.")]
    [SerializeField, Min(0f)] private float _reactionShakeForce = 0.4f;
    [SerializeField, Range(1, 6)] private int _reactionShakePulseCount = 3;
    [SerializeField, Min(0.01f)] private float _reactionShakeInterval = 0.08f;
    [Tooltip("다음 충격에 곱할 강도 배율. 작을수록 빠르게 가라앉습니다.")]
    [SerializeField, Range(0f, 1f)] private float _reactionShakeDecay = 0.65f;

    public AudioClip ReactionSound => _reactionSound;
    public float ReactionSoundVolume => _reactionSoundVolume;
    public float ReactionShakeForce => PotionDataValidation.NonNegative(_reactionShakeForce);
    public int ReactionShakePulseCount => Mathf.Clamp(_reactionShakePulseCount, 1, 6);
    public float ReactionShakeInterval => Mathf.Max(0.01f, PotionDataValidation.NonNegative(_reactionShakeInterval));
    public float ReactionShakeDecay => PotionDataValidation.Multiplier(_reactionShakeDecay);
    public float WaitingDuration => Mathf.Max(0f, _waitingDuration);
    public GameObject ReactionVfxPrefab => _reactionVfxPrefab;
    public float ReactionVfxDuration => Mathf.Max(0f, _reactionVfxDuration);
}
