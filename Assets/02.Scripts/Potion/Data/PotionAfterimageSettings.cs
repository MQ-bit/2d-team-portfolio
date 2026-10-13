using System;
using UnityEngine;

// 기화 반응(기화 + 원소 장판)이 일어난 직후 바닥에 잠깐 남는 잔상의 설정.
// 원소마다 쓸 스프라이트 묶음(시트를 자른 조각들)과 개수, 크기를 따로 정한다.
// 잔상은 기화가 아닌 쪽 장판 범위(합쳐진 장판이면 합쳐진 전체 범위) 안의 무작위 위치에 나온다. 범위를 보여주는 타원 표시는 없다.
[CreateAssetMenu(fileName = "PotionAfterimageSettings", menuName = "Potion/Afterimage Settings")]
public sealed class PotionAfterimageSettings : ScriptableObject
{
    [Serializable]
    public sealed class ElementSet
    {
        [Tooltip("잔상으로 쓸 스프라이트들. 나올 때마다 이 중에서 무작위로 고른다.")]
        [SerializeField] private Sprite[] _sprites;
        [Tooltip("반응 한 번에 나오는 잔상 수의 최소/최대.")]
        [SerializeField, Min(0)] private int _countMin = 10;
        [SerializeField, Min(0)] private int _countMax = 14;
        [Tooltip("스프라이트 원래 크기에 곱하는 배율의 최소/최대. 조각이 작아서 기본이 1보다 크다.")]
        [SerializeField, Min(0.01f)] private float _scaleMin = 2f;
        [SerializeField, Min(0.01f)] private float _scaleMax = 3f;
        [Tooltip("잔상에 곱하는 색. 기본은 흰색(원본 색 그대로).")]
        [SerializeField] private Color _tint = Color.white;

        public Sprite[] Sprites => _sprites;
        public Color Tint => _tint;

        public int RandomCount(float multiplier)
        {
            int min = Mathf.Max(0, _countMin);
            int max = Mathf.Max(min, _countMax);
            return Mathf.RoundToInt(UnityEngine.Random.Range(min, max + 1) * Mathf.Max(0f, multiplier));
        }

        public float RandomScale()
        {
            float min = Mathf.Max(0.01f, _scaleMin);
            return UnityEngine.Random.Range(min, Mathf.Max(min, _scaleMax));
        }
    }

    [Header("원소별 잔상 (기화 + 원소)")]
    [Tooltip("기화 + 전기")]
    [SerializeField] private ElementSet _lightning = new ElementSet();
    [Tooltip("기화 + 불")]
    [SerializeField] private ElementSet _fire = new ElementSet();
    [Tooltip("기화 + 생명")]
    [SerializeField] private ElementSet _heal = new ElementSet();
    [Tooltip("기화 + 서리")]
    [SerializeField] private ElementSet _ice = new ElementSet();
    [Tooltip("기화 + 독")]
    [SerializeField] private ElementSet _poison = new ElementSet();

    [Header("시간")]
    [Tooltip("생기자마자 가장 진한 상태로 유지하는 시간(초).")]
    [SerializeField, Min(0f)] private float _holdDuration = 0.5f;
    [Tooltip("그 뒤 서서히 사라지는 시간(초). 유지 + 페이드가 끝나면 완전히 사라진다.")]
    [SerializeField, Min(0.01f)] private float _fadeDuration = 0.5f;

    [Header("표시")]
    [Tooltip("기화 장판의 정렬 순서에 더하는 값. 바닥 위에 보이게 한다.")]
    [SerializeField] private int _sortingOrderOffset = 1;
    [Tooltip("켜면 잔상을 무작위로 좌우 반전한다.")]
    [SerializeField] private bool _randomFlip = true;

    public float HoldDuration => Mathf.Max(0f, _holdDuration);
    public float FadeDuration => Mathf.Max(0.01f, _fadeDuration);
    public float TotalDuration => HoldDuration + FadeDuration;
    public int SortingOrderOffset => _sortingOrderOffset;
    public bool RandomFlip => _randomFlip;

    public bool TryGetSet(PotionElement element, out ElementSet set)
    {
        set = element switch
        {
            PotionElement.Lightning => _lightning,
            PotionElement.Fire => _fire,
            PotionElement.Heal => _heal,
            PotionElement.Ice => _ice,
            PotionElement.Poison => _poison,
            _ => null
        };
        return set != null && set.Sprites != null && set.Sprites.Length > 0;
    }
}
