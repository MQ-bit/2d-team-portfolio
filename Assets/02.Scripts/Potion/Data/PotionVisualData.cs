using UnityEngine;

[System.Serializable]
public struct PotionVisualData
{
    [SerializeField] private Sprite _iconSprite;
    [SerializeField] private Sprite _areaSprite;
    [SerializeField] private Color _areaColor;
    [SerializeField] private GameObject _areaVfxPrefab;

    public Sprite IconSprite => _iconSprite;
    public Sprite AreaSprite => _areaSprite;
    public Color AreaColor => _areaColor;
    public GameObject AreaVfxPrefab => _areaVfxPrefab;

    public PotionVisualData(Sprite potionSprite, Color areaColor, Sprite areaSprite)
    {
        _iconSprite = potionSprite;
        _areaSprite = areaSprite;
        _areaColor = areaColor;
        _areaVfxPrefab = null;
    }
}
