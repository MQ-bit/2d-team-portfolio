using System;
using UnityEngine;

[Serializable]
public struct PotionItem
{
    [SerializeField] private PotionData _effectData;
    [SerializeField] private Sprite _sprite;

    public PotionData PotionData => _effectData;
    public Sprite Sprite => _sprite;
    public bool IsEmpty => _effectData == null;

    public PotionItem(PotionData effectData, Sprite sprite)
    {
        _effectData = effectData;
        _sprite = sprite;
    }

    public bool CanStackItem(PotionItem other)
    {
        return _effectData == other._effectData
            && _sprite == other._sprite;
    }
}