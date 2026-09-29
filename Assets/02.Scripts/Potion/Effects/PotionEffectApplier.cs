using System;
using System.Collections.Generic;
using UnityEngine;

public static class PotionEffectApplier
{
    private static readonly Dictionary<PotionElement, Action<IDamageable, float>> HealthEffectByElement = new()
    {
        { PotionElement.Heal, (target, amount) => target.RestoreHealth(amount) },
        { PotionElement.Fire, (target, amount) => target.DecreaseHealth(amount) },
        { PotionElement.Poison, (target, amount) => target.DecreaseHealth(amount) },
        { PotionElement.Ice, (target, amount) => target.DecreaseHealth(amount) },
        { PotionElement.Lightning, (target, amount) => target.DecreaseHealth(amount) }
    };

    public static void ApplyAreaEffect(PotionData data, IPotionEffectTarget target, int sourceId)
    {
        if (data == null || target == null) return;

        ApplyHealthEffect(data.Element, data.AreaEffectAmount, target);
        ApplyStatusEffect(data, target, sourceId);
    }

    public static void ApplyDrinkEffect(PotionData data, IPotionEffectTarget target) //포션 섭취 시 호출
    {
        if (data == null || target == null) return;

        PotionEffectProfile profile = data.ThrownProfile;
        if (profile != null)
        {
            PotionStatDispatcher.ChangeThirst(target.Thirstable, profile.ThirstDelta);
            var source = new PotionEffectSource(data.GetInstanceID(), data,
                owner: target.Damageable);
            foreach (PotionEffectDefinition effect in profile.GetEffects(PotionEffectPhase.Drink))
            {
                if (!IsTargetAlive(target)) break;
                // 섭취는 투척용 TargetType과 관계없이 마신 대상 자신에게 적용한다.
                target.ApplyEffect(effect, profile, source);
            }
            return;
        }

        ApplyThirstEffect(data.DrinkThirstRecoveryAmount, target);

        if (data.Element == PotionElement.Vaporization)
        {
            target.ClearStatuses();
            return;
        }

        ApplyHealthEffect(data.Element, data.DrinkEffectAmount, target);
        ApplyStatusEffect(data, target, 0);
    }

    public static IPotionEffectTarget FindTarget(Component component)
    {
        Transform current = component == null ? null : component.transform;
        while (current != null)
        {
            if (current.TryGetComponent<IPotionEffectTarget>(out var target))
            {
                return target;
            }

            current = current.parent;
        }

        return null;
    }

    public static bool IsTargetAlive(IPotionEffectTarget target)
    {
        return target != null && !(target is UnityEngine.Object unityObject && unityObject == null);
    }

    public static bool CanAffect(PotionTargetType targetType, IPotionEffectTarget target)
    {
        if (!IsTargetAlive(target)) return false;
        return targetType == PotionTargetType.Both ||
               targetType == PotionTargetType.Player && target.TargetKind == PotionTargetKind.Player ||
               targetType == PotionTargetType.Enemy && target.TargetKind == PotionTargetKind.Enemy;
    }

    public static void ApplyProfilePhase(PotionEffectProfile profile, PotionEffectPhase phase,
        IPotionEffectTarget target, PotionEffectSource source)
    {
        if (profile == null || !CanAffect(profile.TargetType, target)) return;
        foreach (PotionEffectDefinition effect in profile.GetEffects(phase))
        {
            if (!IsTargetAlive(target)) break;
            target.ApplyEffect(effect, profile, source);
        }
    }

    public static void ApplyCombinationEffect(PotionCombinationEffectCalculator.Result effect,
        IPotionEffectTarget target, IDamageable owner, int sourceId, float remainingDuration)
    {
        if (!IsTargetAlive(target) || remainingDuration <= 0f) return;

        IDamageable health = target.Damageable;
        bool hasHealth = health != null && !(health is UnityEngine.Object healthObject && healthObject == null);
        if (hasHealth && health.IsDead) return;

        if (hasHealth && effect.DamagePerTick > 0f)
        {
            float previousHealth = health.CurrentHealth;
            health.DecreaseHealth(effect.DamagePerTick);
            float actualDamage = Mathf.Max(0f, previousHealth - health.CurrentHealth);
            RestoreCombinationOwner(owner, effect.CalculateLifeSteal(actualDamage));
        }

        if (effect.Status.HasValue && IsTargetAlive(target) && (!hasHealth || !health.IsDead))
        {
            target.ApplyStatus(effect.Status.Value,
                Mathf.Min(effect.StatusDuration, remainingDuration), sourceId);
        }
    }

    public static void RestoreCombinationOwner(IDamageable owner, float amount)
    {
        if (amount <= 0f || owner == null ||
            (owner is UnityEngine.Object ownerObject && ownerObject == null) || owner.IsDead)
        {
            return;
        }

        owner.RestoreHealth(amount);
    }
    private static void ApplyHealthEffect(PotionElement element, float amount, IPotionEffectTarget target)
    {
        if (amount <= 0f || !HealthEffectByElement.TryGetValue(element, out var applyEffect)) return;

        IDamageable damageable = target.Damageable;
        if (damageable == null || (damageable is UnityEngine.Object healthObject && healthObject == null)) return;
        if (damageable.IsDead) return;

        applyEffect(damageable, amount);
    }

    private static void ApplyThirstEffect(float amount, IPotionEffectTarget target)
    {
        if (amount == 0f || float.IsNaN(amount) || float.IsInfinity(amount)) return;

        IThirstable thirstable = target.Thirstable;
        if (thirstable == null || (thirstable is UnityEngine.Object thirstObject && thirstObject == null)) return;

        if (amount > 0f)
        {
            thirstable.RestoreThirst(amount);
        }
        else
        {
            thirstable.DecreaseThirst(-amount);
        }
    }

    private static void ApplyStatusEffect(PotionData data, IPotionEffectTarget target, int sourceId)
    {
        if (data.EffectDuration > 0f &&
            PotionStatusEffectState.TryGetStatus(data.Element, out PotionStatusEffect status))
        {
            target.ApplyStatus(status, data.EffectDuration, sourceId);
        }
    }
}
