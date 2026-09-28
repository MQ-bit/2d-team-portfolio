using UnityEngine;

// 조합 수치만 계산한다. 피해 주기 실행, HP 변경, 장판 제거는 호출자가 담당한다.
public static class PotionCombinationEffectResolver
{
    private static readonly System.Collections.Generic.Dictionary<PotionCombination, PotionElement> SourceElements = new()
    {
        { PotionCombination.VampiricFire, PotionElement.Fire },
        { PotionCombination.VampiricPoison, PotionElement.Poison },
        { PotionCombination.VampiricFrost, PotionElement.Ice },
        { PotionCombination.VampiricLightning, PotionElement.Lightning },
        { PotionCombination.PurifyingLife, PotionElement.Vaporization }
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
            !combination.IsMatch(first.Element, second.Element) ||
            !SourceElements.TryGetValue(combination.Combination, out PotionElement sourceElement))
        {
            return false;
        }

        PotionData source = first.Element == PotionElement.Heal ? second : first;
        PotionData healing = first.Element == PotionElement.Heal ? first : second;
        if (healing.Element != PotionElement.Heal || source.Element != sourceElement) return false;

        if (combination.Combination == PotionCombination.PurifyingLife)
        {
            if (!combination.TryGetEffect(PotionCombinationEffectKind.RestorePlayerHealth, out var recovery))
                return false;

            bool removeGroundAreas = combination.TryGetEffect(PotionCombinationEffectKind.RemoveGroundAreas, out _);
            result = new Result(combination.Combination, PotionElement.None,
                0f, 0f, NonNegativeFinite(combination.CombinationDuration), 0f,
                NonNegativeFinite(recovery.Amount), removeGroundAreas, null, 0f);
            return true;
        }

        if (!combination.TryGetEffect(PotionCombinationEffectKind.Damage, out var damage)) return false;

        PotionStatusEffect? status = null;
        float statusDuration = 0f;
        if (combination.TryGetEffect(PotionCombinationEffectKind.ApplyStatus, out var statusEffect))
        {
            status = statusEffect.Status;
            statusDuration = NonNegativeFinite(statusEffect.Duration);
            if (statusDuration == 0f) statusDuration = NonNegativeFinite(source.EffectDuration);
        }

        float damageAmount = damage.UseSourcePotionDamage ? source.AreaEffectAmount : damage.Amount;
        float tickInterval = NonNegativeFinite(damage.TickInterval);
        if (tickInterval == 0f) tickInterval = NonNegativeFinite(source.TickInterval);

        result = new Result(combination.Combination, sourceElement,
            NonNegativeFinite(damageAmount), tickInterval,
            NonNegativeFinite(combination.CombinationDuration),
            Mathf.Clamp01(NonNegativeFinite(damage.LifeStealRatio)), 0f, false, status, statusDuration);
        return true;
    }

    private static float NonNegativeFinite(float value)
    {
        return float.IsNaN(value) || float.IsInfinity(value) ? 0f : Mathf.Max(0f, value);
    }
}
