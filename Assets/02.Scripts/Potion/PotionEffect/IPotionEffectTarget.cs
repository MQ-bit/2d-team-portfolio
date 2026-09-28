public enum PotionStatusEffect
{
    Burning,
    Poisoned,
    Frozen,
    Paralyzed
}

public interface IPotionEffectTarget
{
    void ModifyHealth(float amount);
    void ApplyStatus(PotionStatusEffect status, float duration);
}
