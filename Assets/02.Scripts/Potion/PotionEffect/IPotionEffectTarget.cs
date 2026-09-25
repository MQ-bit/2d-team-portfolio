public enum PotionTargetKind
{
    Player,
    Enemy
}

public enum PotionStatusEffect
{
    Burning,
    Poisoned,
    Frozen,
    Paralyzed,
    Blinded,
    Revealed
}

public interface IPotionEffectTarget
{
    PotionTargetKind TargetKind { get; }
    void ModifyHealth(float amount);
    void RestoreThirst(float amount);
    void ApplyStatus(PotionStatusEffect status, float duration);
}
