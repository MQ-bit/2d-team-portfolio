using UnityEngine;

public static class PotionEffectResolver
{
    public static void ApplyAreaEffect(PotionData data, IPotionEffectTarget target)
    {
        if (data == null || target == null)
        {
            return;
        }

        ApplyHealthEffect(data.Element, data.AreaEffectAmount, target);
        ApplyStatusEffect(data, target);
    }
    

    public static bool TryFindTarget(Component component, out IPotionEffectTarget target)
    {
        Transform current = component == null ? null : component.transform;
        while (current != null)
        {
            target = current.GetComponent<IPotionEffectTarget>();
            if (target != null)
            {
                return true;
            }

            current = current.parent;
        }

        target = null;
        return false;
    }

    private static void ApplyHealthEffect(PotionElement element, float amount, IPotionEffectTarget target)
    {
        if (amount <= 0f)
        {
            return;
        }

        float signedAmount = element == PotionElement.Heal ? amount : -amount;
        target.ModifyHealth(signedAmount);
    }

    private static void ApplyStatusEffect(PotionData data, IPotionEffectTarget target)
    {
        PotionStatusEffect? status = data.Element switch
        {
            PotionElement.Fire => PotionStatusEffect.Burning,
            PotionElement.Poison => PotionStatusEffect.Poisoned,
            PotionElement.Ice => PotionStatusEffect.Frozen,
            PotionElement.Lightning => PotionStatusEffect.Paralyzed,
            _ => null
        };

        if (status.HasValue && data.EffectDuration > 0f)
        {
            target.ApplyStatus(status.Value, data.EffectDuration);
        }
    }
}
