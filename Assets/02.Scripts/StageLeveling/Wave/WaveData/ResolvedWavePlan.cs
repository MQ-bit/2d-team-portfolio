using System.Collections.Generic;
using UnityEngine;

public class ResolvedWavePlan // TODO : 구조체 고려
{
    public IReadOnlyList<ResolvedWave> Waves { get; }
    public float InterWaveDelay { get; }

    public ResolvedWavePlan(IReadOnlyList<ResolvedWave> waves, float interWaveDelay)
    {
        Waves = waves;
        InterWaveDelay = Mathf.Max(0f, interWaveDelay);
    }
}