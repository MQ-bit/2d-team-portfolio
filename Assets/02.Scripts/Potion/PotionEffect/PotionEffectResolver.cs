using UnityEngine;

public static class PotionEffectResolver
{
    public static void ApplyAreaEffect(PotionData data, IPotionEffectTarget target) //장판에 닿았을 때 호출
    {
        if (data == null || target == null) return;

        ApplyStatusEffect(data, target);
    }

    public static void ApplyDrinkEffect(PotionData data, IPotionEffectTarget target) //포션 섭취 시 호출
    {
        if (data == null || target == null) return;

        if (data.Element == PotionElement.Vaporization)
        {
            target.ClearStatuses();
            return;
        }

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
    private static void ApplyStatusEffect(PotionData data, IPotionEffectTarget target)
    {
        if (data.EffectDuration > 0f &&
            PotionStatusEffectState.TryGetStatus(data.Element, out PotionStatusEffect status))
        {
            target.ApplyStatus(status, data.EffectDuration);
        }
    }
}
