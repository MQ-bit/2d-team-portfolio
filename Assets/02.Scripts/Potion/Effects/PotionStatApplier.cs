using UnityEngine;

// 수치만 전달한다. 실제 저장/Clamp/사망 이벤트는 기존 인터페이스 구현체의 책임이다.
public static class PotionStatApplier
{
    public static bool IsAlive(IDamageable target) =>
        target != null && !(target is Object unityObject && unityObject == null) && !target.IsDead;

    public static PotionDamageResult Damage(IDamageable target, float amount,
        PotionEffectSource source, PotionElement element = PotionElement.None)
    {
        amount = PotionDataValidation.NonNegative(amount);
        if (amount <= 0f || !IsAlive(target)) return new PotionDamageResult(source, element, 0f);
        float before = target.CurrentHealth;
        target.DecreaseHealth(amount);
        // 파괴되어 실제 HP를 읽을 수 없으면 추정 피해로 흡혈하지 않는다.
        float after = target is Object unityObject && unityObject == null ? before : target.CurrentHealth;
        float actual = Mathf.Min(PotionDataValidation.NonNegative(before),
            PotionDataValidation.NonNegative(before - after));
        return new PotionDamageResult(source, element, actual);
    }

    public static void Heal(IDamageable target, float amount)
    {
        amount = PotionDataValidation.NonNegative(amount);
        if (amount > 0f && IsAlive(target)) target.RestoreHealth(amount);
    }

    public static void ChangeThirst(IThirstable target, float delta)
    {
        if (target == null || (target is Object unityObject && unityObject == null)) return;
        delta = PotionDataValidation.Finite(delta);
        if (delta > 0f) target.RestoreThirst(delta);
        else if (delta < 0f) target.DecreaseThirst(-delta);
    }
}
