using UnityEngine;

public static class PotionEffectResolver// 포션 데이터를 읽고 체력 변화·상태이상 전달
{
    public static void ApplyAreaEffect(PotionData data, PotionEffectReceiver target)
    {
        if (data == null || target == null)
        {
            return;
        }

        ApplyHealthEffect(data.Element, data.AreaEffectAmount, target);
        ApplyStatusEffect(data, target);
    }

    public static bool TryFindTarget(Component component, out PotionEffectReceiver target)
    {
        Transform current = component == null ? null : component.transform;
        while (current != null)
        {
            // Avoid Editor allocations when this object has no matching component.
            if (current.TryGetComponent(out target))
            {
                return true;
            }

            current = current.parent;
        }

        target = null;
        return false;
    }

    private static void ApplyHealthEffect(PotionElement element, float amount, PotionEffectReceiver target)
    {
        if (amount <= 0f)
        {
            return;
        }

        float signedAmount = element == PotionElement.Heal ? amount : -amount;
        target.ModifyHealth(signedAmount);
    }

    private static void ApplyStatusEffect(PotionData data, PotionEffectReceiver target)
    {
        if (data.EffectDuration > 0f &&
            PotionStatusEffectState.TryGetStatus(data.Element, out PotionStatusEffect status))
        {
            target.ApplyStatus(status, data.EffectDuration);
        }
    }
}
