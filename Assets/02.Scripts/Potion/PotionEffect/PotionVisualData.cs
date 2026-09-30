using UnityEngine;

[System.Serializable]
public struct PotionVisualData
{
    //Icon 스프라이트 받기!
    [SerializeField] private Sprite _iconSprite; //Icon Sprite
    [SerializeField] private Color _areaColor;

    public Sprite IconSprite => _iconSprite;
    public Color AreaColor => _areaColor;

    public PotionVisualData(Sprite potionSprite, Color areaColor)
    {
        _iconSprite = potionSprite;
        _areaColor = areaColor;
    }
}
