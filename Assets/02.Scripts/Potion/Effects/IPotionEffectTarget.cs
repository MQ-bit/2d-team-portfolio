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

    void EnterFloor(IPotionEffectData profile, PotionEffectSource source);
    void PulseFloor(int sourceId);
    void ExitFloor(int sourceId, bool applyResidual);
    void ApplyEffect(PotionEffectDefinition effect, IPotionEffectData profile, PotionEffectSource source);
    void ClearStatuses();
    void RemoveStatusesFromSource(int sourceId);
}
