using System;
using UnityEngine;

[Serializable]
public struct PotionItem
{
    [SerializeField] private PotionData _effectData;
    [SerializeField] private Sprite _bottleSprite;

    public PotionData PotionData => _effectData;
    public Sprite BottleSprite => _bottleSprite;
    public bool IsEmpty => _effectData == null;

    public PotionItem(PotionData effectData, Sprite sprite)
    {
        _effectData = effectData;
        _bottleSprite = sprite;
    }

    public bool IsSameItem(PotionItem other)
    {
        return _effectData == other._effectData
            && _bottleSprite == other._bottleSprite;
    }
}