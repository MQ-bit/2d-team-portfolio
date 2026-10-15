using System.Collections.Generic;

// 원본 SO를 수정하지 않고 실행 중인 장판의 효과 대상만 바꾼다.
internal sealed class PotionTargetProfile : IPotionEffectData
{
    private readonly IPotionEffectData _source;
    public PotionTargetProfile(IPotionEffectData source, PotionTargetType targetType)
    {
        _source = source;
        TargetType = targetType;
    }
    public PotionTargetType TargetType { get; }
    public float EffectRadius => _source.EffectRadius;
    public bool CreatesFloor => _source.CreatesFloor;
    public float FloorDuration => _source.FloorDuration;
    public float TickInterval => _source.TickInterval;
    public bool ApplyOnEnter => _source.ApplyOnEnter;
    public float LifeStealRatio => _source.LifeStealRatio;
    public IReadOnlyList<PotionEffectDefinition> Effects => _source.Effects;
    public PotionFrostSettings Frost => _source.Frost;
}
