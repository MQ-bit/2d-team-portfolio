using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class RoomWaveSet //이건 대체 왜..?
{
    [SerializeField] private WaveDefinition[] _waves = Array.Empty<WaveDefinition>();
    public IReadOnlyList<WaveDefinition> Waves => _waves;
}
