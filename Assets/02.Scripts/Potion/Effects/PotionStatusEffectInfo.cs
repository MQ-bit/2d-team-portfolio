using UnityEngine;

// 장판 GameObject가 사라져도 원인과 시전자는 대상의 잔류 상태에 남는다.
public readonly struct PotionEffectSource
{
    public int SourceId { get; }
    public PotionData Potion { get; }
    public PotionCombinationData Combination { get; }
    public IDamageable Owner { get; }
    public float LifeStealRatio { get; }

    public PotionEffectSource(int sourceId, PotionData potion = null,
        PotionCombinationData combination = null, IDamageable owner = null, float lifeStealRatio = 0f)
    {
        SourceId = sourceId;
        Potion = potion;
        Combination = combination;
        Owner = owner;
        LifeStealRatio = Mathf.Clamp01(PotionDataValidation.Finite(lifeStealRatio));
    }
}

// 에셋 수치를 적용 시 복사한다. 현재 시간과 대상 정보는 실행기에서 관리한다.
public readonly struct PotionStatusEffectInfo
{
    public PotionStatusEffectTracker Status { get; }
    public PotionElement Element { get; }
    public float Duration { get; }
    public float DamagePerTick { get; }
    public float TickInterval { get; }
    public bool ApplyImmediately { get; }
    public float MoveSpeedMultiplier { get; }
    public float ImmobilizeDuration { get; }
    public PotionEffectSource Source { get; }

    public PotionStatusEffectInfo(PotionStatusEffectTracker status, float duration, PotionEffectSource source,
        PotionElement element = PotionElement.None, float damagePerTick = 0f, float tickInterval = 0f,
        bool applyImmediately = false, float moveSpeedMultiplier = 1f, float immobilizeDuration = 0f)
    {
        Status = status;
        Element = element;
        Duration = PotionDataValidation.NonNegative(duration);
        DamagePerTick = status == PotionStatusEffectTracker.Burning || status == PotionStatusEffectTracker.Poisoned
            ? PotionDataValidation.NonNegative(damagePerTick) : 0f;
        float interval = PotionDataValidation.NonNegative(tickInterval);
        TickInterval = interval > 0f ? Mathf.Max(0.01f, interval) : 0f;
        ApplyImmediately = applyImmediately;
        MoveSpeedMultiplier = PotionDataValidation.Multiplier(moveSpeedMultiplier);
        ImmobilizeDuration = PotionDataValidation.NonNegative(immobilizeDuration);
        Source = source;
    }

    public static PotionStatusEffectInfo FromDefinition(PotionEffectDefinition effect, PotionEffectSource source) =>
        new(effect.Status, effect.Duration, source, effect.Element, effect.Amount, effect.TickInterval,
            effect.ApplyImmediately, effect.MoveSpeedMultiplier, effect.ImmobilizeDuration);
}

public readonly struct PotionStatusDamage
{
    public PotionStatusEffectInfo EffectInfo { get; }
    public float Amount => EffectInfo.DamagePerTick;
    public PotionEffectSource Source => EffectInfo.Source;
    public PotionStatusDamage(PotionStatusEffectInfo effectInfo) => EffectInfo = effectInfo;
}

public readonly struct PotionDamageResult
{
    public PotionEffectSource Source { get; }
    public PotionElement Element { get; }
    public float ActualDamage { get; }
    public float LifeStealAmount => ActualDamage * Source.LifeStealRatio;

    public PotionDamageResult(PotionEffectSource source, PotionElement element, float actualDamage)
    {
        Source = source;
        Element = element;
        ActualDamage = PotionDataValidation.NonNegative(actualDamage);
    }
}
