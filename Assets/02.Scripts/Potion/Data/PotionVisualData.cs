using UnityEngine;

public enum PotionAreaSurfaceStyle
{
    Flow = 0,
    Embers = 1,
    Bubbles = 2,
    Frost = 3,
    Life = 4,
    Electricity = 5,
    Mist = 6,
    Cracks = 7,
    HealingSpring = 8,
    FireVortex = 10
}

[System.Serializable]
public struct PotionVisualData
{
    [SerializeField] private Sprite _iconSprite;
    [SerializeField] private Sprite _cardBackgroundSprite;
    [SerializeField] private Sprite _areaSprite;
    [SerializeField] private Color _areaColor;
    [Tooltip("할당된 VFX의 밝은 부분과 어울리는 장판 강조색")]
    [SerializeField] private Color _areaAccentColor;
    [Tooltip("할당된 VFX에 맞춘 장판 표면 연출")]
    [SerializeField] private PotionAreaSurfaceStyle _areaSurfaceStyle;
    [SerializeField] private GameObject _areaVfxPrefab;
    [Tooltip("장판을 만들지 않는 포션의 착탄 후 표시 유지 시간입니다(초). 지속 효과는 없으며, 기화는 이 시간 동안 다른 포션과 조합할 수 있습니다. 0이면 즉시 제거합니다.")]
    [SerializeField, Min(0f)] private float _impactVisualDuration;

    public Sprite IconSprite => _iconSprite;
    public Sprite CardBackgroundSprite => _cardBackgroundSprite;
    public Sprite AreaSprite => _areaSprite;
    public Color AreaColor => _areaColor;
    public Color AreaAccentColor => _areaAccentColor.a > 0f
        ? _areaAccentColor : Color.Lerp(_areaColor, Color.white, 0.55f);
    public PotionAreaSurfaceStyle AreaSurfaceStyle => _areaSurfaceStyle;
    public GameObject AreaVfxPrefab => _areaVfxPrefab;
    public float ImpactVisualDuration => Mathf.Max(0f, _impactVisualDuration);

    public PotionVisualData(Sprite potionSprite, Color areaColor, Sprite areaSprite, Sprite cardBackgroundSprite)
    {
        _iconSprite = potionSprite;
        _cardBackgroundSprite = cardBackgroundSprite;
        _areaSprite = areaSprite;
        _areaColor = areaColor;
        _areaAccentColor = Color.Lerp(areaColor, Color.white, 0.55f);
        _areaSurfaceStyle = PotionAreaSurfaceStyle.Flow;
        _areaVfxPrefab = null;
        _impactVisualDuration = 0f;
    }
}
