using UnityEngine;

public static class PotionEffectResolver
{
    public static void ApplyAreaEffect(PotionData data, IPotionEffectTarget target)
    {
        if (data == null || target == null) return;

        ApplyHealthEffect(data.Element, data.AreaEffectAmount, target);
        ApplyStatusEffect(data, target);
    }

    public static void ApplyDrinkEffect(PotionData data, IPotionEffectTarget target)
    {
        if (data == null || target == null) return;

        ApplyHealthEffect(data.Element, data.DrinkEffectAmount, target);
        target.RestoreThirst(data.DrinkThirstRecoveryAmount); //TODO : 포션을 마시면 Thirsty는 감소해야한다! 근데 여긴 증가되므로 수정하기
        ApplyStatusEffect(data, target);
    }

    public static IPotionEffectTarget FindTarget(Component component)
    {
        Transform current = component == null ? null : component.transform;
        while (current != null)
        {
            MonoBehaviour[] behaviours = current.GetComponents<MonoBehaviour>();
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IPotionEffectTarget target) return target;
            }

            current = current.parent;
        }

        return null;
    }

    private static void ApplyHealthEffect(PotionElement element, float amount, IPotionEffectTarget target)
    {
        if (amount <= 0f) return;
        target.ModifyHealth(element == PotionElement.Heal ? amount : -amount);
    }

    private static void ApplyStatusEffect(PotionData data, IPotionEffectTarget target)
    {
        PotionStatusEffect? status = data.Element switch
        {
            PotionElement.Fire => PotionStatusEffect.Burning,
            PotionElement.Poison => PotionStatusEffect.Poisoned,
            PotionElement.Ice => PotionStatusEffect.Frozen,
            PotionElement.Lightning => PotionStatusEffect.Paralyzed,
            PotionElement.Flash when target.TargetKind == PotionTargetKind.Player => PotionStatusEffect.Revealed,
            PotionElement.Flash => PotionStatusEffect.Blinded,
            _ => null
        };

        if (status.HasValue && data.EffectDuration > 0f)
        {
            target.ApplyStatus(status.Value, data.EffectDuration);
        }
    }
}
