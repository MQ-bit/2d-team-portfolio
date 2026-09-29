using UnityEngine;

// 조합 수치만 계산한다. 피해 주기 실행, HP 변경, 장판 제거는 호출자가 담당한다.
public static class PotionCombinationEffectCalculator
{
    private const float FixedDamageDuration = 4f;
    private const float CombinationDamageMultiplier = 1.2f;
    private const float CombinationTickInterval = 1f;

    private static readonly System.Collections.Generic.Dictionary<PotionCombination, PotionElement> SourceElements = new()
    {
        { PotionCombination.VampiricFire, PotionElement.Fire },
        { PotionCombination.VampiricPoison, PotionElement.Poison },
        { PotionCombination.VampiricFrost, PotionElement.Ice },
        { PotionCombination.VampiricLightning, PotionElement.Lightning }
    };

    public readonly struct Result
    {
        public PotionCombination Combination { get; }
        public PotionElement DamageElement { get; }
        public float DamagePerTick { get; }
        public float TickInterval { get; }
        public float CombinationDuration { get; }
        public float LifeStealRatio { get; }
        public float PlayerHealthRecovery { get; }
        public bool RemoveGroundAreas { get; }
        public PotionStatusEffect? Status { get; }
        public float StatusDuration { get; }

        internal Result(PotionCombination combination, PotionElement damageElement,
            float damagePerTick, float tickInterval, float damageDuration,
            float lifeStealRatio, float playerHealthRecovery, bool removeGroundAreas,
            PotionStatusEffect? status, float statusDuration)
        {
            Combination = combination;
            DamageElement = damageElement;
            DamagePerTick = damagePerTick;
            TickInterval = tickInterval;
            CombinationDuration = damageDuration;
            LifeStealRatio = lifeStealRatio;
            PlayerHealthRecovery = playerHealthRecovery;
            RemoveGroundAreas = removeGroundAreas;
            Status = status;
            StatusDuration = statusDuration;
        }

        // 과잉 피해/방어 등을 반영한 실제 HP 감소량을 전달해야 한다.
        public float CalculateLifeSteal(float actualDamage)
        {
            return NonNegativeFinite(actualDamage) * LifeStealRatio;
        }
    }

    public static bool TryCalculate(PotionCombinationData combination, PotionData first,
        PotionData second, out Result result)
    {
        result = default;
        if (combination == null || first == null || second == null ||
            !combination.IsMatch(first.Element, second.Element))
        {
            return false;
        }

        if (combination.Combination == PotionCombination.PurifyingLife)
        {
            if (!combination.TryGetEffect(PotionCombinationEffectKind.RestorePlayerHealth, out var recovery))
                return false;

            bool removeGroundAreas = combination.TryGetEffect(PotionCombinationEffectKind.RemoveGroundAreas, out _);
            result = new Result(combination.Combination, PotionElement.None,
                0f, 0f, FixedDamageDuration, 0f,
                NonNegativeFinite(recovery.Amount), removeGroundAreas, null, 0f);
            return true;
        }

        if (SourceElements.TryGetValue(combination.Combination, out PotionElement sourceElement))
        {
            return TryCalculateVampiric(combination, first, second, sourceElement, out result);
        }

        if (first.Element == PotionElement.Vaporization || second.Element == PotionElement.Vaporization)
        {
            return TryCalculateVaporization(combination, first, second, out result);
        }

        return TryCalculateStandard(combination, first, second, out result);
    }

    private static bool TryCalculateVampiric(PotionCombinationData combination, PotionData first,
        PotionData second, PotionElement sourceElement, out Result result)
    {
        result = default;
        PotionData source = first.Element == PotionElement.Heal ? second : first;
        PotionData healing = first.Element == PotionElement.Heal ? first : second;
        if (healing.Element != PotionElement.Heal || source.Element != sourceElement ||
            !combination.TryGetEffect(PotionCombinationEffectKind.Damage, out var damage))
        {
            return false;
        }

        ResolveStatus(combination, first, second, out PotionStatusEffect? status, out float statusDuration);

        float damageAmount = damage.UseSourceElementDamage ? source.AreaEffectAmount : damage.Amount;
        float tickInterval = NonNegativeFinite(damage.TickInterval);
        if (tickInterval == 0f) tickInterval = NonNegativeFinite(source.TickInterval);

        result = new Result(combination.Combination, sourceElement,
            NonNegativeFinite(damageAmount), tickInterval,
            FixedDamageDuration,
            Mathf.Clamp01(NonNegativeFinite(damage.LifeStealRatio)), 0f, false, status, statusDuration);
        return true;
    }

    private static bool TryCalculateStandard(PotionCombinationData combination, PotionData first,
        PotionData second, out Result result)
    {
        result = default;
        if (first.Element == PotionElement.Heal || second.Element == PotionElement.Heal ||
            (!combination.TryGetEffect(PotionCombinationEffectKind.Damage, out _) &&
             !combination.TryGetEffect(PotionCombinationEffectKind.PeriodicDamage, out _)))
        {
            return false;
        }

        float damagePerSecond = (CalculateAreaDamagePerSecond(first) +
                                 CalculateAreaDamagePerSecond(second)) * CombinationDamageMultiplier;
        if (damagePerSecond <= 0f) return false;

        ResolveStatus(combination, first, second, out PotionStatusEffect? status, out float statusDuration);
        result = new Result(combination.Combination, ResolveDamageElement(combination, first),
            damagePerSecond, CombinationTickInterval, FixedDamageDuration,
            0f, 0f, false, status, statusDuration);
        return true;
    }

    private static bool TryCalculateVaporization(PotionCombinationData combination, PotionData first,
        PotionData second, out Result result)
    {
        result = default;
        PotionData source = first.Element == PotionElement.Vaporization ? second : first;
        if (source.Element == PotionElement.Heal ||
            !combination.TryGetEffect(PotionCombinationEffectKind.AreaDamage, out _))
        {
            return false;
        }

        float instantDamage = CalculateTotalAreaDamage(source) * CombinationDamageMultiplier;
        if (instantDamage <= 0f) return false;

        ResolveStatus(combination, first, second, out PotionStatusEffect? status, out float statusDuration);
        bool removeGroundAreas = combination.TryGetEffect(PotionCombinationEffectKind.RemoveGroundAreas, out _);
        result = new Result(combination.Combination, source.Element,
            instantDamage, 0f, FixedDamageDuration,
            0f, 0f, removeGroundAreas, status, statusDuration);
        return true;
    }

    private static float CalculateAreaDamagePerSecond(PotionData potion)
    {
        float duration = NonNegativeFinite(potion.AreaDuration);
        float totalDamage = CalculateTotalAreaDamage(potion);
        return duration > 0f ? totalDamage / duration : totalDamage;
    }

    private static float CalculateTotalAreaDamage(PotionData potion)
    {
        float damage = NonNegativeFinite(potion.AreaEffectAmount);
        float duration = NonNegativeFinite(potion.AreaDuration);
        float interval = NonNegativeFinite(potion.TickInterval);
        if (damage <= 0f) return 0f;
        if (duration <= 0f || interval <= 0f) return damage;

        int tickCount = Mathf.Max(1, Mathf.CeilToInt(duration / interval));
        return damage * tickCount;
    }

    private static PotionElement ResolveDamageElement(PotionCombinationData combination, PotionData fallback)
    {
        foreach (PotionCombinationEffect effect in combination.Effects)
        {
            if (effect == null || effect.Element == PotionElement.None) continue;
            if (effect.Kind == PotionCombinationEffectKind.Damage ||
                effect.Kind == PotionCombinationEffectKind.PeriodicDamage ||
                effect.Kind == PotionCombinationEffectKind.AreaDamage)
            {
                return effect.Element;
            }
        }

        return fallback.Element;
    }

    private static void ResolveStatus(PotionCombinationData combination, PotionData first, PotionData second,
        out PotionStatusEffect? status, out float duration)
    {
        status = null;
        duration = 0f;
        if (!combination.TryGetEffect(PotionCombinationEffectKind.ApplyStatus, out var statusEffect)) return;

        status = statusEffect.Status;
        duration = NonNegativeFinite(statusEffect.Duration);
        if (duration > 0f) return;

        PotionElement statusElement = statusEffect.Status switch
        {
            PotionStatusEffect.Burning => PotionElement.Fire,
            PotionStatusEffect.Poisoned => PotionElement.Poison,
            PotionStatusEffect.Frozen => PotionElement.Ice,
            PotionStatusEffect.Paralyzed => PotionElement.Lightning,
            _ => PotionElement.None
        };

        if (first.Element == statusElement) duration = NonNegativeFinite(first.EffectDuration);
        else if (second.Element == statusElement) duration = NonNegativeFinite(second.EffectDuration);
    }

    private static float NonNegativeFinite(float value)
    {
        return float.IsNaN(value) || float.IsInfinity(value) ? 0f : Mathf.Max(0f, value);
    }
}
