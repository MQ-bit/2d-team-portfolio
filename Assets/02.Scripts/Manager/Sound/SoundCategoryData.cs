using System;
using UnityEngine;

[CreateAssetMenu(fileName = "SoundCategoryData", menuName = "Sound/Sound Category Data")]
public sealed class SoundCategoryData : ScriptableObject
{
    [SerializeField] private GameSoundData.Entry[] _entries = Array.Empty<GameSoundData.Entry>();

    public GameSoundData.Entry Get(GameSoundId id)
    {
        if (_entries == null) return null;
        foreach (GameSoundData.Entry entry in _entries)
            if (entry != null && entry.Id == id) return entry;
        return null;
    }
}
