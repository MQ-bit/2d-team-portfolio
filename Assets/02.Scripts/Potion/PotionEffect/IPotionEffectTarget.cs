public enum PotionTargetKind
{
    Player,
    Enemy
}

public interface IPotionEffectTarget
{
    PotionTargetKind TargetKind { get; }

    // Applies status effects only. Health and thirst are handled by the player.

    void ApplyStatus(PotionStatusEffect status, float duration);
    void ApplyStatus(PotionStatusEffect status, float duration, int sourceId);
    void ClearStatuses();
}
