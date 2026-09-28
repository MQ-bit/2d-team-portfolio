public enum PotionTargetKind
{
    Player,
    Enemy
}

public interface IPotionEffectTarget
{
    PotionTargetKind TargetKind { get; }

    // 수신자가 초기화 시 찾아 둔 체력/갈증 처리 대상. 없으면 null.
    IDamageable Damageable { get; }
    IThirstable Thirstable { get; }

    void ApplyStatus(PotionStatusEffect status, float duration, int sourceId);
    void ClearStatuses();
}
