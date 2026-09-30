using UnityEngine;

[System.Serializable]
public struct PotionVisualData
{
    [SerializeField] private Sprite _potionSprite;
    [SerializeField] private Color _areaColor;

    public Sprite PotionSprite => _potionSprite;
    public Color AreaColor => _areaColor;

    public PotionVisualData(Sprite potionSprite, Color areaColor)
    {
        _potionSprite = potionSprite;
        _areaColor = areaColor;
    }
}
