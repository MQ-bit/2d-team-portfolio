using UnityEngine;

public static class PotionEffectApplier
{
    public static void ApplyDrinkEffect(PotionData data, IPotionEffectTarget target)
    {
        if (data == null || !IsTargetAvailable(target)) return;
        PotionStatDispatcher.ChangeThirst(target.Thirstable, data.ThirstDelta);
        var source = new PotionEffectSource(data.GetInstanceID(), data, owner: target.Damageable);
        // 섭취는 투척용 TargetType과 관계없이 마신 대상 자신에게 적용한다.
        ApplyEffects(data, PotionEffectPhase.Drink, target, source);
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

    private static bool IsTargetAvailable(IPotionEffectTarget target)
    {
        return target != null && !(target is UnityEngine.Object unityObject && unityObject == null);
    }

    public static bool CanAffect(PotionTargetType targetType, IPotionEffectTarget target)
    {
        if (!IsTargetAvailable(target)) return false;
        return targetType == PotionTargetType.Both ||
               targetType == PotionTargetType.Player && target.TargetKind == PotionTargetKind.Player ||
               targetType == PotionTargetType.Enemy && target.TargetKind == PotionTargetKind.Enemy;
    }

    public static void ApplyProfilePhase(IPotionEffectData profile, PotionEffectPhase phase,
        IPotionEffectTarget target, PotionEffectSource source)
    {
        if (profile == null || !CanAffect(profile.TargetType, target)) return;
        ApplyEffects(profile, phase, target, source);
    }

    private static void ApplyEffects(IPotionEffectData profile, PotionEffectPhase phase,
        IPotionEffectTarget target, PotionEffectSource source)
    {
        foreach (PotionEffectDefinition effect in profile.GetEffects(phase))
        {
            if (!IsTargetAvailable(target)) break;
            target.ApplyEffect(effect, profile, source);
        }
    }
}
