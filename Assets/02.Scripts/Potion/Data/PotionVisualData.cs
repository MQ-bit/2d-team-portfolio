using UnityEngine;

[System.Serializable]
public struct PotionVisualData
{
    [SerializeField] private Sprite _iconSprite;
    [SerializeField] private Sprite _areaSprite;
    [SerializeField] private Color _areaColor;

    public Sprite IconSprite => _iconSprite;
    public Sprite AreaSprite => _areaSprite;
    public Color AreaColor => _areaColor;

    public PotionVisualData(Sprite potionSprite, Color areaColor, Sprite areaSprite)
    {
        _iconSprite = potionSprite;
        _areaSprite = areaSprite;
        _areaColor = areaColor;
    }
}
