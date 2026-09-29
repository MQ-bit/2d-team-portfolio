using UnityEngine;

[System.Serializable]
public struct PotionVisualData
{
    [SerializeField] private Sprite _potionSprite;
    [SerializeField] private Sprite _areaSprite;
    [SerializeField] private Color _areaColor;

    public Sprite PotionSprite => _potionSprite;
    public Sprite AreaSprite => _areaSprite;
    public Color AreaColor => _areaColor;

    public PotionVisualData(Sprite potionSprite, Color areaColor, Sprite areaSprite)
    {
        _potionSprite = potionSprite;
        _areaSprite = areaSprite;
        _areaColor = areaColor;
    }
}
