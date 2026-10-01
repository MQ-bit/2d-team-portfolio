using System.Collections.Generic;

// 실행기가 단일/조합 에셋을 공통으로 읽는 계약. 데이터는 각 에셋에 직접 저장한다.
public interface IPotionEffectData
{
    PotionTargetType TargetType { get; }
    float EffectRadius { get; }
    bool CreatesFloor { get; }
    float FloorDuration { get; }
    float TickInterval { get; }
    bool ApplyOnEnter { get; }
    float LifeStealRatio { get; }
    IReadOnlyList<PotionEffectDefinition> Effects { get; }
    PotionFrostSettings Frost { get; }
}

public static class PotionEffectDataExtensions
{
    public static IEnumerable<PotionEffectDefinition> GetEffects(this IPotionEffectData data, PotionEffectPhase phase)
    {
        if (data?.Effects == null) yield break;
        foreach (PotionEffectDefinition effect in data.Effects)
        {
            if (effect != null && effect.Phase == phase) yield return effect;
        }
    }
}