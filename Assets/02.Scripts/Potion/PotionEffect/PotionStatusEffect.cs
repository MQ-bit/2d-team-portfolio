using System.Collections.Generic;
using UnityEngine;

public enum PotionStatusEffect
{
    Burning,
    Poisoned,
    Frozen,
    Paralyzed
}

public sealed class PotionStatusEffectState
{
    private static readonly Dictionary<PotionElement, PotionStatusEffect> StatusByElement = new()
    {
        { PotionElement.Fire, PotionStatusEffect.Burning },
        { PotionElement.Poison, PotionStatusEffect.Poisoned },
        { PotionElement.Ice, PotionStatusEffect.Frozen },
        { PotionElement.Lightning, PotionStatusEffect.Paralyzed }
    };

    // Each receiver owns its own expiration times; only the mapping is shared.
    private readonly Dictionary<PotionStatusEffect, float> _expiresAtByStatus = new(4);

    public bool CanAct => !IsActive(PotionStatusEffect.Frozen) && !IsActive(PotionStatusEffect.Paralyzed);

    public static bool TryGetStatus(PotionElement element, out PotionStatusEffect status)
    {
        return StatusByElement.TryGetValue(element, out status);
    }

    public bool IsActive(PotionStatusEffect status)
    {
        return _expiresAtByStatus.TryGetValue(status, out float expiresAt) && Time.time < expiresAt;
    }

    public void Apply(PotionStatusEffect status, float duration)
    {
        if (duration <= 0f)
        {
            return;
        }

        _expiresAtByStatus.TryGetValue(status, out float previousExpiresAt);
        _expiresAtByStatus[status] = Mathf.Max(previousExpiresAt, Time.time + duration);
    }
}
