using System;
using System.Collections.Generic;
using UnityEngine;

public static class PotionEffectResolver
{
    private static readonly Dictionary<PotionElement, Action<IDamageable, float>> HealthEffectByElement = new()
    {
        { PotionElement.Heal, (target, amount) => target.RestoreHealth(amount) },
        { PotionElement.Fire, (target, amount) => target.DecreaseHealth(amount) },
        { PotionElement.Poison, (target, amount) => target.DecreaseHealth(amount) },
        { PotionElement.Ice, (target, amount) => target.DecreaseHealth(amount) },
        { PotionElement.Lightning, (target, amount) => target.DecreaseHealth(amount) }
    };

    public static void ApplyAreaEffect(PotionData data, IPotionEffectTarget target) //장판에 닿았을 때 호출
    {
        ApplyAreaEffect(data, target, 0);
    }

    public static void ApplyAreaEffect(PotionData data, IPotionEffectTarget target, int sourceId)
    {
        if (data == null || target == null) return;

        ApplyHealthEffect(data.Element, data.AreaEffectAmount, target);
        ApplyStatusEffect(data, target, sourceId);
    }

    public static void ApplyDrinkEffect(PotionData data, IPotionEffectTarget target) //포션 섭취 시 호출
    {
        if (data == null || target == null) return;

        ApplyThirstEffect(data.DrinkThirstRecoveryAmount, target);

        if (data.Element == PotionElement.Vaporization)
        {
            target.ClearStatuses();
            return;
        }

        ApplyHealthEffect(data.Element, data.DrinkEffectAmount, target);
        ApplyStatusEffect(data, target);
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
    private static void ApplyHealthEffect(PotionElement element, float amount, IPotionEffectTarget target)
    {
        if (amount <= 0f || !HealthEffectByElement.TryGetValue(element, out var applyEffect)) return;

        IDamageable damageable = target as IDamageable;
        if (damageable == null && target is Component component && component != null)
        {
            damageable = component.GetComponentInParent<IDamageable>();
        }

        if (damageable == null || damageable.IsDead) return;

        applyEffect(damageable, amount);
    }

    private static void ApplyThirstEffect(float amount, IPotionEffectTarget target)
    {
        if (amount == 0f || float.IsNaN(amount) || float.IsInfinity(amount)) return;

        IThirstable thirstable = target as IThirstable;
        if (thirstable == null && target is Component component && component != null)
        {
            thirstable = component.GetComponentInParent<IThirstable>();
        }

        if (thirstable == null) return;

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
